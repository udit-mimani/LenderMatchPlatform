namespace LenderMatch.API.Entities;

public class Lender
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public List<LendingProgram> Programs { get; set; } = new();
    // E.g., "Trucking", "Gambling" - Applies to all programs
    public List<string> IndustryExclusions { get; set; } = new();
}

public class LendingProgram
{
    public int Id { get; set; }
    public required string Name { get; set; } // e.g., "Tier 1", "Startup Program"
    public int LenderId { get; set; }
    public Lender Lender { get; set; } = null!;

    // Hard Constraints
    public decimal? MinLoanAmount { get; set; }
    public decimal? MaxLoanAmount { get; set; }
    public int? MinTimeInBusinessMonths { get; set; }
    public int? MinFicoScore { get; set; }
    public int? MinPayNetScore { get; set; }

    // Some lenders have specific equipment lists (Advantage+ says "Non-Trucking")
    public List<string> EquipmentExclusions { get; set; } = new();
}

public class LoanApplication
{
    public int Id { get; set; }
    public required string BusinessName { get; set; }
    public required string Industry { get; set; } // "Trucking", "Medical", "General"
    public decimal LoanAmount { get; set; }
    public int FicoScore { get; set; }
    public int YearsInBusiness { get; set; } // Converted to months in logic
    public int? PayNetScore { get; set; } // Optional as not all users might have it
    public string EquipmentType { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}