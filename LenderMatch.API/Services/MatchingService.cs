using LenderMatch.API.Data;
using LenderMatch.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace LenderMatch.API.Services;

public class MatchingService
{
    private readonly AppDbContext _context;

    public MatchingService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Main evaluation workflow - validates, derives features, evaluates matches, and ranks results
    /// </summary>
    public async Task<MatchingWorkflowResult> EvaluateAsync(LoanApplication app)
    {
        var workflowResult = new MatchingWorkflowResult
        {
            ApplicationId = app.Id,
            EvaluatedAt = DateTime.UtcNow
        };

        // STEP 1: Validate Application Completeness
        var validationErrors = ValidateApplicationCompleteness(app);
        if (validationErrors.Any())
        {
            workflowResult.IsValid = false;
            workflowResult.ValidationErrors = validationErrors;
            return workflowResult;
        }
        workflowResult.IsValid = true;

        // STEP 2: Derive Necessary Features
        var derivedFeatures = DeriveFeatures(app);
        workflowResult.DerivedFeatures = derivedFeatures;

        // STEP 3: Load Lenders and Evaluate Matches
        var lenders = await _context.Lenders
            .Include(l => l.Programs)
            .ToListAsync();

        var matchResults = new List<MatchResult>();

        foreach (var lender in lenders)
        {
            var result = EvaluateLender(app, lender, derivedFeatures);
            matchResults.Add(result);
        }

        // STEP 4: Rank Matches by Fit Score
        workflowResult.Matches = matchResults
            .OrderByDescending(r => r.FitScore)
            .ThenBy(r => r.LenderName)
            .ToList();

        workflowResult.EligibleCount = matchResults.Count(m => m.IsEligible);
        workflowResult.TotalEvaluated = matchResults.Count;

        return workflowResult;
    }

    /// <summary>
    /// STEP 1: Validates that all required application fields are present and valid
    /// </summary>
    private List<string> ValidateApplicationCompleteness(LoanApplication app)
    {
        var errors = new List<string>();

        // Business/Borrower Validation
        if (app.Business == null)
        {
            errors.Add("Business information is required.");
            return errors; // Can't continue without business
        }

        if (string.IsNullOrWhiteSpace(app.Business.BusinessName))
            errors.Add("Business name is required.");

        if (string.IsNullOrWhiteSpace(app.Business.Industry))
            errors.Add("Industry is required.");

        if (string.IsNullOrWhiteSpace(app.Business.State))
            errors.Add("Business state is required.");

        if (app.Business.YearsInBusiness < 0)
            errors.Add("Years in business cannot be negative.");

        if (app.Business.AnnualRevenue < 0)
            errors.Add("Annual revenue cannot be negative.");

        // Personal Guarantor Validation
        if (app.Guarantor == null)
        {
            errors.Add("Personal guarantor information is required.");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(app.Guarantor.Name))
            errors.Add("Guarantor name is required.");

        if (app.Guarantor.FicoScore < 300 || app.Guarantor.FicoScore > 850)
            errors.Add("FICO score must be between 300 and 850.");

        // Loan Request Validation
        if (app.Request == null)
        {
            errors.Add("Loan request information is required.");
            return errors;
        }

        if (app.Request.Amount <= 0)
            errors.Add("Loan amount must be greater than zero.");

        if (app.Request.TermMonths <= 0)
            errors.Add("Loan term must be greater than zero.");

        if (string.IsNullOrWhiteSpace(app.Request.EquipmentType))
            errors.Add("Equipment type is required.");

        if (app.Request.EquipmentYear < 1900 || app.Request.EquipmentYear > DateTime.Now.Year + 1)
            errors.Add($"Equipment year must be between 1900 and {DateTime.Now.Year + 1}.");

        // Business Credit Validation (warnings, not errors)
        if (app.CreditProfile == null)
        {
            errors.Add("Business credit profile is required.");
        }

        return errors;
    }

    /// <summary>
    /// STEP 2: Derives computed features from the application data
    /// </summary>
    private DerivedFeatures DeriveFeatures(LoanApplication app)
    {
        var features = new DerivedFeatures();

        // Equipment Age Calculation
        if (app.Request.EquipmentYear > 0)
        {
            features.EquipmentAgeYears = DateTime.Now.Year - app.Request.EquipmentYear;
        }

        // Business Classification
        features.BusinessType = ClassifyBusinessType(app.Business.Industry);
        features.IsTrucking = IsTruckingIndustry(app.Business.Industry);
        features.IsMedical = IsMedicalIndustry(app.Business.Industry);
        features.IsStartup = app.Business.YearsInBusiness < 2;

        // Credit Strength Assessment
        features.CreditTier = DetermineCreditTier(app);
        features.HasPayNetScore = app.CreditProfile?.PayNetScore.HasValue ?? false;

        // Bankruptcy/Credit Issues
        features.HasCreditIssues = app.Guarantor.HasBankruptcy ||
                                   app.Guarantor.HasTaxLiens;

        if (app.Guarantor.HasBankruptcy && app.Guarantor.BankruptcyDischargeYears > 0)
        {
            features.BankruptcyDischargeYears = app.Guarantor.BankruptcyDischargeYears;
        }

        // Comparable Debt Analysis
        features.HasComparableDebt = app.CreditProfile?.HasComparableDebt ?? false;
        features.TradeLineCount = app.CreditProfile?.TradeLineCount ?? 0;

        // Loan-to-Value and Size Classification
        features.LoanSizeCategory = ClassifyLoanSize(app.Request.Amount);

        // Equipment Classification
        features.EquipmentCategory = ClassifyEquipment(app.Request.EquipmentType);

        return features;
    }

    /// <summary>
    /// STEP 3: Evaluates a single lender against the application
    /// </summary>
    private MatchResult EvaluateLender(LoanApplication app, Lender lender, DerivedFeatures features)
    {
        var result = new MatchResult
        {
            LenderName = lender.Name,
            FitScore = 0,
            IsEligible = false,
            EvaluatedAt = DateTime.UtcNow
        };

        // --- GLOBAL LENDER RESTRICTIONS ---

        // 1. Industry Restrictions
        if (lender.RestrictedIndustries?.Any() == true)
        {
            var restrictedMatch = lender.RestrictedIndustries
                .FirstOrDefault(ri => app.Business.Industry.Contains(ri, StringComparison.OrdinalIgnoreCase) ||
                                     ri.Contains(app.Business.Industry, StringComparison.OrdinalIgnoreCase));

            if (restrictedMatch != null)
            {
                result.RejectionReasons.Add($"Industry '{app.Business.Industry}' is restricted by {lender.Name}.");
                result.FailurePoint = "Industry Restriction";
                return result;
            }
        }

        // 2. State Restrictions
        if (lender.RestrictedStates?.Contains(app.Business.State) == true)
        {
            result.RejectionReasons.Add($"{lender.Name} does not operate in {app.Business.State}.");
            result.FailurePoint = "State Restriction";
            return result;
        }

        // 3. Special Checks for Specific Lenders
        var lenderSpecificChecks = PerformLenderSpecificChecks(app, lender, features);
        if (lenderSpecificChecks.Any())
        {
            result.RejectionReasons.AddRange(lenderSpecificChecks);
            result.FailurePoint = "Lender-Specific Requirements";
            return result;
        }

        // --- PROGRAM LEVEL EVALUATION ---
        var programEvaluations = new List<ProgramEvaluation>();

        foreach (var program in lender.Programs)
        {
            var progEval = EvaluateProgram(app, program, features);
            programEvaluations.Add(progEval);

            if (progEval.IsQualified)
            {
                result.IsEligible = true;
                result.QualifiedPrograms.Add(program.Name);
            }
        }

        // Determine best matching program
        if (result.IsEligible)
        {
            var bestProgram = programEvaluations
                .Where(pe => pe.IsQualified)
                .OrderByDescending(pe => pe.Score)
                .FirstOrDefault();

            if (bestProgram != null)
            {
                result.BestMatchingProgram = bestProgram.ProgramName;
                result.ProgramMatchReasons = bestProgram.MatchReasons;
            }

            // Calculate Fit Score
            result.FitScore = CalculateFitScore(app, lender, programEvaluations, features);
        }
        else
        {
            // Compile rejection reasons from all programs if none qualified
            if (!result.RejectionReasons.Any())
            {
                var allRejectionReasons = programEvaluations
                    .SelectMany(pe => pe.RejectionReasons)
                    .Distinct()
                    .ToList();

                if (allRejectionReasons.Any())
                {
                    result.RejectionReasons = allRejectionReasons;
                }
                else
                {
                    result.RejectionReasons.Add("Does not meet minimum credit, tenure, or asset criteria for any program.");
                }

                result.FailurePoint = "Program Requirements";
            }
        }

        return result;
    }

    /// <summary>
    /// Evaluates a specific program against the application
    /// </summary>
    private ProgramEvaluation EvaluateProgram(LoanApplication app, LendingProgram program, DerivedFeatures features)
    {
        var evaluation = new ProgramEvaluation
        {
            ProgramName = program.Name,
            IsQualified = true,
            Score = 100 // Start with perfect score, deduct for issues
        };

        // 1. Amount Checks
        if (program.MinAmount.HasValue && app.Request.Amount < program.MinAmount)
        {
            evaluation.IsQualified = false;
            evaluation.RejectionReasons.Add($"Loan amount ${app.Request.Amount:N0} is below minimum ${program.MinAmount:N0} for {program.Name}.");
            evaluation.Score -= 30;
        }

        if (program.MaxAmount.HasValue && app.Request.Amount > program.MaxAmount)
        {
            evaluation.IsQualified = false;
            evaluation.RejectionReasons.Add($"Loan amount ${app.Request.Amount:N0} exceeds maximum ${program.MaxAmount:N0} for {program.Name}.");
            evaluation.Score -= 30;
        }

        // 2. FICO Score Check
        if (program.MinFico.HasValue)
        {
            if (app.Guarantor.FicoScore < program.MinFico)
            {
                evaluation.IsQualified = false;
                evaluation.RejectionReasons.Add($"FICO score {app.Guarantor.FicoScore} is below minimum {program.MinFico} for {program.Name}.");
                evaluation.Score -= 25;
            }
            else
            {
                // Add bonus for exceeding FICO requirements
                var ficoBuffer = app.Guarantor.FicoScore - program.MinFico.Value;
                evaluation.MatchReasons.Add($"FICO score {app.Guarantor.FicoScore} exceeds minimum by {ficoBuffer} points.");
                evaluation.Score += Math.Min(10, ficoBuffer / 10);
            }
        }

        // 3. Time in Business Check
        if (program.MinTimeInBusinessYears.HasValue)
        {
            if (app.Business.YearsInBusiness < program.MinTimeInBusinessYears)
            {
                evaluation.IsQualified = false;
                evaluation.RejectionReasons.Add($"Time in business {app.Business.YearsInBusiness:F1} years is below minimum {program.MinTimeInBusinessYears} years for {program.Name}.");
                evaluation.Score -= 25;
            }
            else
            {
                var tibBuffer = app.Business.YearsInBusiness - program.MinTimeInBusinessYears.Value;
                if (tibBuffer > 0)
                {
                    evaluation.MatchReasons.Add($"Time in business {app.Business.YearsInBusiness:F1} years exceeds minimum by {tibBuffer:F1} years.");
                    evaluation.Score += Math.Min(10, (int)(tibBuffer * 2));
                }
            }
        }

        // 4. PayNet Score Check
        if (program.MinPayNet.HasValue)
        {
            if (!app.CreditProfile.PayNetScore.HasValue)
            {
                evaluation.IsQualified = false;
                evaluation.RejectionReasons.Add($"PayNet score is required but not provided for {program.Name}.");
                evaluation.Score -= 20;
            }
            else if (app.CreditProfile.PayNetScore.Value < program.MinPayNet.Value)
            {
                evaluation.IsQualified = false;
                evaluation.RejectionReasons.Add($"PayNet score {app.CreditProfile.PayNetScore} is below minimum {program.MinPayNet} for {program.Name}.");
                evaluation.Score -= 20;
            }
            else
            {
                var paynetBuffer = app.CreditProfile.PayNetScore.Value - program.MinPayNet.Value;
                evaluation.MatchReasons.Add($"PayNet score {app.CreditProfile.PayNetScore} exceeds minimum by {paynetBuffer} points.");
                evaluation.Score += Math.Min(10, paynetBuffer / 10);
            }
        }

        // 5. Revenue Check (for Corp programs)
        if (program.MinRevenue.HasValue)
        {
            if (app.Business.AnnualRevenue < program.MinRevenue)
            {
                evaluation.IsQualified = false;
                evaluation.RejectionReasons.Add($"Annual revenue ${app.Business.AnnualRevenue:N0} is below minimum ${program.MinRevenue:N0} for {program.Name}.");
                evaluation.Score -= 20;
            }
            else
            {
                evaluation.MatchReasons.Add($"Annual revenue ${app.Business.AnnualRevenue:N0} meets minimum requirement.");
            }
        }

        // 6. Equipment Age Check
        if (program.MaxEquipmentAgeYears.HasValue && features.EquipmentAgeYears.HasValue)
        {
            if (features.EquipmentAgeYears.Value > program.MaxEquipmentAgeYears.Value)
            {
                evaluation.IsQualified = false;
                evaluation.RejectionReasons.Add($"Equipment age {features.EquipmentAgeYears} years exceeds maximum {program.MaxEquipmentAgeYears} years for {program.Name}.");
                evaluation.Score -= 15;
            }
            else
            {
                evaluation.MatchReasons.Add($"Equipment age {features.EquipmentAgeYears} years is within acceptable range.");
            }
        }

        // 7. Trucking Exclusion Check
        if (program.ExcludeTrucking && features.IsTrucking)
        {
            evaluation.IsQualified = false;
            evaluation.RejectionReasons.Add($"{program.Name} excludes trucking industry.");
            evaluation.Score -= 30;
        }

        // Ensure score stays in valid range
        evaluation.Score = Math.Max(0, Math.Min(100, evaluation.Score));

        return evaluation;
    }

    /// <summary>
    /// Performs lender-specific validation checks based on documented policies
    /// </summary>
    private List<string> PerformLenderSpecificChecks(LoanApplication app, Lender lender, DerivedFeatures features)
    {
        var issues = new List<string>();

        switch (lender.Name)
        {
            case "Advantage+ Financing":
                // No bankruptcies allowed
                if (app.Guarantor.HasBankruptcy)
                {
                    issues.Add("Advantage+ does not finance bankruptcies.");
                }
                // No tax liens
                if (app.Guarantor.HasTaxLiens)
                {
                    issues.Add("Advantage+ does not accept tax liens.");
                }
                // Requires comparable credit (80%)
                if (!app.CreditProfile.HasComparableDebt)
                {
                    issues.Add("Advantage+ prefers 80% comparable credit.");
                }
                break;

            case "Stearns Bank":
                // Requires 3+ trade lines
                if (app.CreditProfile.TradeLineCount < 3)
                {
                    issues.Add("Stearns Bank requires 3 or more trade lines with payment activity.");
                }
                // No bankruptcy in last 7 years
                if (app.Guarantor.HasBankruptcy && features.BankruptcyDischargeYears < 7)
                {
                    issues.Add("Stearns Bank requires 7+ years since bankruptcy discharge.");
                }
                // Comparable debt required
                if (!app.CreditProfile.HasComparableDebt)
                {
                    issues.Add("Stearns Bank requires comparable business borrowing history.");
                }
                break;

            case "Apex Commercial Capital":
                // Comparable business borrowing requirements
                if (app.Request.Amount >= 50000 && app.Request.Amount <= 100000)
                {
                    if (!app.CreditProfile.HasComparableDebt)
                    {
                        issues.Add("Apex requires comparable business borrowing of 50% for $50K-$100K requests.");
                    }
                }
                if (app.Request.Amount > 100000)
                {
                    if (!app.CreditProfile.HasComparableDebt)
                    {
                        issues.Add("Apex requires comparable business borrowing of 75% for requests over $100K.");
                    }
                }
                break;

            case "Falcon Equipment Finance":
                // Bankruptcies must be 15+ years discharged
                if (app.Guarantor.HasBankruptcy)
                {
                    if (features.BankruptcyDischargeYears < 15)
                    {
                        issues.Add("Falcon requires bankruptcies to be dismissed or discharged 15+ years ago.");
                    }
                }
                // Requires comparable commercial installment credit (70%)
                if (!app.CreditProfile.HasComparableDebt)
                {
                    issues.Add("Falcon requires comparable commercial installment credit repayment of at least 70%.");
                }
                // Special trucking requirements
                if (features.IsTrucking)
                {
                    if (app.Business.YearsInBusiness < 5)
                    {
                        issues.Add("Falcon requires 5+ years in business for trucking.");
                    }
                    if (app.Guarantor.FicoScore < 700)
                    {
                        issues.Add("Falcon requires 700+ FICO for trucking.");
                    }
                    if (app.CreditProfile.PayNetScore.HasValue && app.CreditProfile.PayNetScore < 680)
                    {
                        issues.Add("Falcon requires 680+ PayNet for trucking.");
                    }
                }
                break;

            case "Citizens Bank":
                // Bankruptcy must be 5+ years discharged
                if (app.Guarantor.HasBankruptcy && features.BankruptcyDischargeYears < 5)
                {
                    issues.Add("Citizens Bank requires bankruptcies to be over 5 years discharged.");
                }
                break;
        }

        return issues;
    }

    /// <summary>
    /// Calculates a comprehensive fit score (0-100) based on multiple factors
    /// </summary>
    private int CalculateFitScore(LoanApplication app, Lender lender, List<ProgramEvaluation> programEvaluations, DerivedFeatures features)
    {
        var score = 0;

        // Base score from best program match
        var bestProgram = programEvaluations
            .Where(pe => pe.IsQualified)
            .OrderByDescending(pe => pe.Score)
            .FirstOrDefault();

        if (bestProgram != null)
        {
            score = (int)(bestProgram.Score * 0.6); // 60% weight from program score
        }

        // Bonus for multiple qualifying programs (shows strong fit)
        var qualifiedCount = programEvaluations.Count(pe => pe.IsQualified);
        score += Math.Min(15, qualifiedCount * 5); // Up to 15 points for multiple programs

        // Credit strength bonus
        if (app.Guarantor.FicoScore >= 750)
            score += 10;
        else if (app.Guarantor.FicoScore >= 720)
            score += 5;

        // PayNet bonus
        if (app.CreditProfile.PayNetScore.HasValue)
        {
            if (app.CreditProfile.PayNetScore >= 700)
                score += 8;
            else if (app.CreditProfile.PayNetScore >= 680)
                score += 4;
        }

        // Established business bonus
        if (app.Business.YearsInBusiness >= 10)
            score += 7;
        else if (app.Business.YearsInBusiness >= 5)
            score += 4;

        // Loan size optimization (prefer lenders with good rate tiers for this amount)
        if (lender.Name == "Apex Commercial Capital" && app.Request.Amount >= 50000)
            score += 3;
        if (lender.Name == "Falcon Equipment Finance" && app.Request.Amount >= 50000)
            score += 2;

        // Deductions for risk factors
        if (app.Guarantor.HasBankruptcy)
            score -= 15;
        if (app.Guarantor.HasTaxLiens)
            score -= 10;
        if (!app.CreditProfile.HasComparableDebt)
            score -= 8;
        if (features.EquipmentAgeYears > 10)
            score -= 5;

        // Ensure score is in valid range
        return Math.Max(0, Math.Min(100, score));
    }

    #region Helper Methods

    private string ClassifyBusinessType(string industry)
    {
        var industryLower = industry.ToLower();

        if (industryLower.Contains("truck") || industryLower.Contains("transport") || industryLower.Contains("logistics"))
            return "Trucking/Transportation";
        if (industryLower.Contains("medical") || industryLower.Contains("dental") || industryLower.Contains("veterinar") ||
            industryLower.Contains("healthcare") || industryLower.Contains("doctor"))
            return "Medical/Healthcare";
        if (industryLower.Contains("construction") || industryLower.Contains("contractor"))
            return "Construction";
        if (industryLower.Contains("restaurant") || industryLower.Contains("food"))
            return "Restaurant/Food Service";
        if (industryLower.Contains("manufacturing"))
            return "Manufacturing";

        return "General";
    }

    private bool IsTruckingIndustry(string industry)
    {
        var industryLower = industry.ToLower();
        return industryLower.Contains("truck") ||
               industryLower.Contains("transport") ||
               industryLower.Contains("logistics") ||
               industryLower.Contains("hauling");
    }

    private bool IsMedicalIndustry(string industry)
    {
        var industryLower = industry.ToLower();
        return industryLower.Contains("medical") ||
               industryLower.Contains("dental") ||
               industryLower.Contains("veterinar") ||
               industryLower.Contains("healthcare") ||
               industryLower.Contains("doctor") ||
               industryLower.Contains("clinic");
    }

    private string DetermineCreditTier(LoanApplication app)
    {
        var fico = app.Guarantor.FicoScore;
        var paynet = app.CreditProfile?.PayNetScore;

        if (fico >= 750 && (!paynet.HasValue || paynet >= 700))
            return "A+";
        if (fico >= 720 && (!paynet.HasValue || paynet >= 680))
            return "A";
        if (fico >= 680 && (!paynet.HasValue || paynet >= 660))
            return "B";
        if (fico >= 640)
            return "C";
        if (fico >= 600)
            return "D";

        return "E";
    }

    private string ClassifyLoanSize(decimal amount)
    {
        if (amount < 25000) return "Small";
        if (amount < 75000) return "Medium";
        if (amount < 150000) return "Large";
        if (amount < 500000) return "Very Large";
        return "Enterprise";
    }

    private string ClassifyEquipment(string equipmentType)
    {
        var typeLower = equipmentType.ToLower();

        if (typeLower.Contains("truck") || typeLower.Contains("trailer"))
            return "Heavy Vehicle";
        if (typeLower.Contains("medical") || typeLower.Contains("dental"))
            return "Medical Equipment";
        if (typeLower.Contains("construction") || typeLower.Contains("excavator"))
            return "Construction Equipment";
        if (typeLower.Contains("machine") || typeLower.Contains("tool"))
            return "Industrial Machinery";

        return "General Equipment";
    }

    #endregion
}

#region Supporting Classes

/// <summary>
/// Complete workflow result including validation, derived features, and matches
/// </summary>
public class MatchingWorkflowResult
{
    public int ApplicationId { get; set; }
    public DateTime EvaluatedAt { get; set; }
    public bool IsValid { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public DerivedFeatures DerivedFeatures { get; set; } = new();
    public List<MatchResult> Matches { get; set; } = new();
    public int EligibleCount { get; set; }
    public int TotalEvaluated { get; set; }
}

/// <summary>
/// Features derived from the application for matching logic
/// </summary>
public class DerivedFeatures
{
    public int? EquipmentAgeYears { get; set; }
    public string BusinessType { get; set; } = string.Empty;
    public bool IsTrucking { get; set; }
    public bool IsMedical { get; set; }
    public bool IsStartup { get; set; }
    public string CreditTier { get; set; } = string.Empty;
    public bool HasPayNetScore { get; set; }
    public bool HasCreditIssues { get; set; }
    public int BankruptcyDischargeYears { get; set; }
    public bool HasComparableDebt { get; set; }
    public int TradeLineCount { get; set; }
    public string LoanSizeCategory { get; set; } = string.Empty;
    public string EquipmentCategory { get; set; } = string.Empty;
}

/// <summary>
/// Evaluation result for a specific program
/// </summary>
public class ProgramEvaluation
{
    public string ProgramName { get; set; } = string.Empty;
    public bool IsQualified { get; set; }
    public int Score { get; set; }
    public List<string> MatchReasons { get; set; } = new();
    public List<string> RejectionReasons { get; set; } = new();
}

#endregion