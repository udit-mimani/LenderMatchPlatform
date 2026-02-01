using LenderMatch.API.Entities;

namespace LenderMatch.API.TestData;

/// <summary>
/// Example loan applications for testing the matching engine
/// </summary>
public static class ExampleApplications
{
    /// <summary>
    /// Strong applicant - should match multiple lenders with high scores
    /// </summary>
    public static LoanApplication StrongApplicant => new()
    {
        Business = new Borrower
        {
            BusinessName = "ABC Construction Inc",
            Industry = "Construction",
            State = "TX",
            YearsInBusiness = 10,
            AnnualRevenue = 5000000
        },
        Guarantor = new PersonalGuarantor
        {
            Name = "John Smith",
            FicoScore = 780,
            HasBankruptcy = false,
            HasTaxLiens = false,
            BankruptcyDischargeYears = 0
        },
        CreditProfile = new BusinessCredit
        {
            PayNetScore = 720,
            TradeLineCount = 8,
            HasComparableDebt = true
        },
        Request = new LoanRequest
        {
            Amount = 250000,
            TermMonths = 60,
            EquipmentType = "Excavator",
            EquipmentYear = 2022,
            EquipmentMileage = null
        }
    };

    /// <summary>
    /// Startup applicant - should match limited lenders (Advantage+, Citizens Tier 2)
    /// </summary>
    public static LoanApplication StartupApplicant => new()
    {
        Business = new Borrower
        {
            BusinessName = "New Medical Practice LLC",
            Industry = "Medical",
            State = "FL",
            YearsInBusiness = 0.5m,
            AnnualRevenue = 150000
        },
        Guarantor = new PersonalGuarantor
        {
            Name = "Dr. Sarah Johnson",
            FicoScore = 720,
            HasBankruptcy = false,
            HasTaxLiens = false,
            BankruptcyDischargeYears = 0
        },
        CreditProfile = new BusinessCredit
        {
            PayNetScore = null, // No business credit yet
            TradeLineCount = 0,
            HasComparableDebt = false
        },
        Request = new LoanRequest
        {
            Amount = 45000,
            TermMonths = 48,
            EquipmentType = "Medical Equipment",
            EquipmentYear = 2024,
            EquipmentMileage = null
        }
    };

    /// <summary>
    /// Trucking company - should match specific trucking-friendly lenders (Falcon, Citizens)
    /// </summary>
    public static LoanApplication TruckingApplicant => new()
    {
        Business = new Borrower
        {
            BusinessName = "Swift Logistics LLC",
            Industry = "Trucking",
            State = "OH",
            YearsInBusiness = 8,
            AnnualRevenue = 2500000
        },
        Guarantor = new PersonalGuarantor
        {
            Name = "Michael Rodriguez",
            FicoScore = 710,
            HasBankruptcy = false,
            HasTaxLiens = false,
            BankruptcyDischargeYears = 0
        },
        CreditProfile = new BusinessCredit
        {
            PayNetScore = 685,
            TradeLineCount = 5,
            HasComparableDebt = true
        },
        Request = new LoanRequest
        {
            Amount = 180000,
            TermMonths = 60,
            EquipmentType = "Class 8 Truck",
            EquipmentYear = 2020,
            EquipmentMileage = 150000
        }
    };

    /// <summary>
    /// Marginal credit applicant - should match lower tiers (B/C programs)
    /// </summary>
    public static LoanApplication MarginalCreditApplicant => new()
    {
        Business = new Borrower
        {
            BusinessName = "Quick Auto Repair",
            Industry = "Automotive Repair",
            State = "MI",
            YearsInBusiness = 4,
            AnnualRevenue = 450000
        },
        Guarantor = new PersonalGuarantor
        {
            Name = "James Wilson",
            FicoScore = 660,
            HasBankruptcy = false,
            HasTaxLiens = false,
            BankruptcyDischargeYears = 0
        },
        CreditProfile = new BusinessCredit
        {
            PayNetScore = 650,
            TradeLineCount = 3,
            HasComparableDebt = true
        },
        Request = new LoanRequest
        {
            Amount = 35000,
            TermMonths = 48,
            EquipmentType = "Auto Repair Equipment",
            EquipmentYear = 2021,
            EquipmentMileage = null
        }
    };

    /// <summary>
    /// California applicant - should be rejected by Apex and Citizens
    /// </summary>
    public static LoanApplication CaliforniaApplicant => new()
    {
        Business = new Borrower
        {
            BusinessName = "Golden State Manufacturing",
            Industry = "Manufacturing",
            State = "CA", // Restricted by Apex and Citizens
            YearsInBusiness = 12,
            AnnualRevenue = 8000000
        },
        Guarantor = new PersonalGuarantor
        {
            Name = "Emily Chen",
            FicoScore = 750,
            HasBankruptcy = false,
            HasTaxLiens = false,
            BankruptcyDischargeYears = 0
        },
        CreditProfile = new BusinessCredit
        {
            PayNetScore = 710,
            TradeLineCount = 12,
            HasComparableDebt = true
        },
        Request = new LoanRequest
        {
            Amount = 500000,
            TermMonths = 60,
            EquipmentType = "Industrial Machinery",
            EquipmentYear = 2023,
            EquipmentMileage = null
        }
    };

    /// <summary>
    /// Past bankruptcy applicant - limited matches based on discharge period
    /// </summary>
    public static LoanApplication BankruptcyApplicant => new()
    {
        Business = new Borrower
        {
            BusinessName = "Recovery Services Inc",
            Industry = "Commercial Cleaning",
            State = "GA",
            YearsInBusiness = 6,
            AnnualRevenue = 750000
        },
        Guarantor = new PersonalGuarantor
        {
            Name = "Robert Martinez",
            FicoScore = 680,
            HasBankruptcy = true,
            HasTaxLiens = false,
            BankruptcyDischargeYears = 8 // 8 years since discharge
        },
        CreditProfile = new BusinessCredit
        {
            PayNetScore = 670,
            TradeLineCount = 4,
            HasComparableDebt = true
        },
        Request = new LoanRequest
        {
            Amount = 60000,
            TermMonths = 48,
            EquipmentType = "Commercial Cleaning Equipment",
            EquipmentYear = 2022,
            EquipmentMileage = null
        }
    };

    /// <summary>
    /// Large loan corporate applicant - should match Corp-only programs
    /// </summary>
    public static LoanApplication CorporateApplicant => new()
    {
        Business = new Borrower
        {
            BusinessName = "MegaCorp Industries",
            Industry = "Manufacturing",
            State = "TX",
            YearsInBusiness = 15,
            AnnualRevenue = 12000000 // Exceeds Apex Corp minimum
        },
        Guarantor = new PersonalGuarantor
        {
            Name = "David Thompson",
            FicoScore = 740,
            HasBankruptcy = false,
            HasTaxLiens = false,
            BankruptcyDischargeYears = 0
        },
        CreditProfile = new BusinessCredit
        {
            PayNetScore = 705,
            TradeLineCount = 20,
            HasComparableDebt = true
        },
        Request = new LoanRequest
        {
            Amount = 750000,
            TermMonths = 60,
            EquipmentType = "Industrial Machinery",
            EquipmentYear = 2024,
            EquipmentMileage = null
        }
    };

    /// <summary>
    /// Restricted industry applicant - should face rejections
    /// </summary>
    public static LoanApplication RestrictedIndustryApplicant => new()
    {
        Business = new Borrower
        {
            BusinessName = "Luxury Tanning Salon",
            Industry = "Tanning Salon", // Restricted by multiple lenders
            State = "NY",
            YearsInBusiness = 5,
            AnnualRevenue = 300000
        },
        Guarantor = new PersonalGuarantor
        {
            Name = "Amanda White",
            FicoScore = 720,
            HasBankruptcy = false,
            HasTaxLiens = false,
            BankruptcyDischargeYears = 0
        },
        CreditProfile = new BusinessCredit
        {
            PayNetScore = 680,
            TradeLineCount = 3,
            HasComparableDebt = true
        },
        Request = new LoanRequest
        {
            Amount = 50000,
            TermMonths = 48,
            EquipmentType = "Tanning Beds",
            EquipmentYear = 2023,
            EquipmentMileage = null
        }
    };

    /// <summary>
    /// Old equipment applicant - may face equipment age restrictions
    /// </summary>
    public static LoanApplication OldEquipmentApplicant => new()
    {
        Business = new Borrower
        {
            BusinessName = "Heritage Manufacturing",
            Industry = "Manufacturing",
            State = "PA",
            YearsInBusiness = 20,
            AnnualRevenue = 3000000
        },
        Guarantor = new PersonalGuarantor
        {
            Name = "Richard Brown",
            FicoScore = 730,
            HasBankruptcy = false,
            HasTaxLiens = false,
            BankruptcyDischargeYears = 0
        },
        CreditProfile = new BusinessCredit
        {
            PayNetScore = 695,
            TradeLineCount = 15,
            HasComparableDebt = true
        },
        Request = new LoanRequest
        {
            Amount = 100000,
            TermMonths = 36,
            EquipmentType = "Industrial Press",
            EquipmentYear = 2008, // 17 years old
            EquipmentMileage = null
        }
    };

    /// <summary>
    /// Get all example applications
    /// </summary>
    public static List<LoanApplication> GetAllExamples()
    {
        return new List<LoanApplication>
        {
            StrongApplicant,
            StartupApplicant,
            TruckingApplicant,
            MarginalCreditApplicant,
            CaliforniaApplicant,
            BankruptcyApplicant,
            CorporateApplicant,
            RestrictedIndustryApplicant,
            OldEquipmentApplicant
        };
    }
}