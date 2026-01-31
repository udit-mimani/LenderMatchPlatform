namespace LenderMatch.API.Entities;

// --- ROOT AGGREGATE ---
public class LoanApplication
{
    public int Id { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    // Relationships (One-to-One for simplicity in this assignment)
    public Borrower Business { get; set; } = new();
    public PersonalGuarantor Guarantor { get; set; } = new();
    public BusinessCredit CreditProfile { get; set; } = new();
    public LoanRequest Request { get; set; } = new();
}

// --- REQUIREMENT 1: BORROWER/BUSINESS ---
// "industry, state, years in business, revenue"
public class Borrower
{
    public int Id { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty; // e.g., "Trucking", "Medical"
    public string State { get; set; } = string.Empty;    // Required for Apex/Citizens exclusions [cite: 41, 293]
    public decimal YearsInBusiness { get; set; }         // e.g., 2.5
    public decimal AnnualRevenue { get; set; }           // Required for Apex Corp-Only 
}

// --- REQUIREMENT 2: PERSONAL GUARANTOR ---
// "FICO, credit history flags"
public class PersonalGuarantor
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int FicoScore { get; set; }                   // e.g., 700

    // Credit History Flags
    public bool HasBankruptcy { get; set; }              // Stearns/Advantage+ check this 
    public bool HasTaxLiens { get; set; }                // Advantage+ check [cite: 5]
    public int BankruptcyDischargeYears { get; set; }    // "How long after discharge" [cite: 5]
}

// --- REQUIREMENT 3: BUSINESS CREDIT ---
// "PayNet score, trade lines"
public class BusinessCredit
{
    public int Id { get; set; }
    public int? PayNetScore { get; set; }                // Stearns/Falcon use this [cite: 115, 162]

    // Trade Lines
    public int TradeLineCount { get; set; }              // Stearns requires "3 or more contracts" [cite: 122]
    public bool HasComparableDebt { get; set; }          // "Comparable business borrowing" [cite: 28]
}

// --- REQUIREMENT 4: LOAN REQUEST ---
// "amount, term, equipment details"
public class LoanRequest
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public int TermMonths { get; set; }                  // Required for Citizens terms 

    // Equipment Details
    public string EquipmentType { get; set; } = string.Empty; // "Truck", "Medical Device"
    public int EquipmentYear { get; set; }               // Citizens/Falcon restrict age [cite: 194, 50]
    public int? EquipmentMileage { get; set; }           // Advantage+/Citizens check mileage [cite: 5, 230]
}

// --- REQUIREMENT 5: LENDER POLICIES ---
// "programs, criteria, restrictions"
public class Lender
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Navigation
    public List<LendingProgram> Programs { get; set; } = new();

    // Global Restrictions (Apply to all programs for this lender)
    public List<string> RestrictedIndustries { get; set; } = new(); // e.g., "Gambling" [cite: 126]
    public List<string> RestrictedStates { get; set; } = new();     // e.g., "CA", "NV" 
}

public class LendingProgram
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // "Tier 1", "Startup Program"

    // --- Criteria (Nullable means "No Rule") ---
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }          // Advantage+ <$75k [cite: 4]

    public int? MinFico { get; set; }
    public int? MinPayNet { get; set; }
    public decimal? MinTimeInBusinessYears { get; set; }
    public decimal? MinRevenue { get; set; }         // Apex Corp only $3MM 

    // Equipment Rules
    public int? MaxEquipmentAgeYears { get; set; }   // Falcon 15 years 
    public bool ExcludeTrucking { get; set; }        // Advantage+ Non-Trucking [cite: 4]
}

// --- REQUIREMENT 6: MATCH RESULTS ---
public class MatchResult
{
    public string LenderName { get; set; } = string.Empty;
    public bool IsEligible { get; set; }
    public List<string> QualifiedPrograms { get; set; } = new();
    public List<string> RejectionReasons { get; set; } = new();
    public int FitScore { get; set; }                // 0-100 as requested [cite: 336]
}