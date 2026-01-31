using LenderMatch.API.Entities;

namespace LenderMatch.API.Data;

public static class DbSeeder
{
    public static void Seed(AppDbContext context)
    {
        if (context.Lenders.Any()) return;

        var lenders = new List<Lender>
        {
            // ============================================================
            // 1. ADVANTAGE+ FINANCING
            // ============================================================
            // Source: Advantage__Broker_2025.pdf
            // Non-Trucking applications up to $75,000
            new Lender
            {
                Name = "Advantage+ Financing",
                RestrictedIndustries = new List<string>
                {
                    "Trucking" // Explicitly Non-Trucking only
                },
                RestrictedStates = new List<string>(), // No state restrictions mentioned
                Programs = new List<LendingProgram>
                {
                    new LendingProgram
                    {
                        Name = "Standard Non-Trucking Program",
                        MinAmount = 10000, // $10,000 loan minimum
                        MaxAmount = 75000, // ≤$75,000
                        MinFico = 680, // 680 FICO v5 (Equifax)
                        MinPayNet = null, // Not mentioned
                        MinTimeInBusinessYears = 3, // 3 years minimum industry experience
                        MinRevenue = null, // Not specified
                        MaxEquipmentAgeYears = null, // No age restrictions
                        ExcludeTrucking = true
                    },
                    new LendingProgram
                    {
                        Name = "Start-Up Program",
                        MinAmount = 10000,
                        MaxAmount = 75000,
                        MinFico = 700, // 700+ for Start-Ups
                        MinPayNet = null,
                        MinTimeInBusinessYears = 0, // Accepts startups
                        MinRevenue = null,
                        MaxEquipmentAgeYears = null,
                        ExcludeTrucking = true
                    }
                }
            },

            // ============================================================
            // 2. APEX COMMERCIAL CAPITAL
            // ============================================================
            // Source: Apex_EF_Broker_Guidelines_082725.pdf
            new Lender
            {
                Name = "Apex Commercial Capital",
                RestrictedIndustries = new List<string>
                {
                    "Aircraft/Boats",
                    "ATMs",
                    "Audio/Visual",
                    "Cannabis",
                    "Casino/Gambling",
                    "Churches/Non-profits",
                    "Copiers",
                    "Electric Vehicles",
                    "Fad Medical",
                    "Furniture",
                    "Kiosks",
                    "Leasehold Improvements",
                    "Logging Equipment",
                    "Nail Salons",
                    "Petroleum Industry (Oil/Gas)",
                    "Sale-Leasebacks",
                    "Signage",
                    "Tanning Beds",
                    "Trucking (Local & Long Haul)"
                },
                RestrictedStates = new List<string> { "CA", "NV", "ND", "VT" },
                Programs = new List<LendingProgram>
                {
                    // STANDARD PRICING - A Rate
                    new LendingProgram
                    {
                        Name = "Standard A Rate",
                        MinAmount = 10000,
                        MaxAmount = 500000,
                        MinFico = 700, // 700+ FICO
                        MinPayNet = 660, // 660+ PayNet
                        MinTimeInBusinessYears = 5, // 5 years time in business
                        MinRevenue = null,
                        MaxEquipmentAgeYears = null, // Not specified for standard
                        ExcludeTrucking = true
                    },
                    // STANDARD PRICING - B Rate
                    new LendingProgram
                    {
                        Name = "Standard B Rate",
                        MinAmount = 10000,
                        MaxAmount = 250000,
                        MinFico = 670, // 670+ FICO
                        MinPayNet = 650, // 650+ PayNet
                        MinTimeInBusinessYears = 3, // 3 years time in business
                        MinRevenue = null,
                        MaxEquipmentAgeYears = null,
                        ExcludeTrucking = true
                    },
                    // STANDARD PRICING - C Rate
                    new LendingProgram
                    {
                        Name = "Standard C Rate",
                        MinAmount = 10000,
                        MaxAmount = 100000,
                        MinFico = 640, // 640+ FICO
                        MinPayNet = 640, // 640+ PayNet
                        MinTimeInBusinessYears = 2, // 2 years time in business
                        MinRevenue = null,
                        MaxEquipmentAgeYears = null,
                        ExcludeTrucking = true
                    },
                    // MEDICAL PRICING - A Rate
                    new LendingProgram
                    {
                        Name = "Medical A Rate",
                        MinAmount = 10000,
                        MaxAmount = 500000,
                        MinFico = 700, // 700+ FICO
                        MinPayNet = null, // Not mentioned for medical
                        MinTimeInBusinessYears = 5, // 5 years time licensed
                        MinRevenue = null,
                        MaxEquipmentAgeYears = null,
                        ExcludeTrucking = false
                    },
                    // MEDICAL PRICING - B Rate
                    new LendingProgram
                    {
                        Name = "Medical B Rate",
                        MinAmount = 10000,
                        MaxAmount = 250000,
                        MinFico = 670, // 670+ FICO
                        MinPayNet = null,
                        MinTimeInBusinessYears = 2, // 2 years time licensed
                        MinRevenue = null,
                        MaxEquipmentAgeYears = null,
                        ExcludeTrucking = false
                    },
                    // A+ PRICING (Premium)
                    new LendingProgram
                    {
                        Name = "A+ Rate",
                        MinAmount = 10000,
                        MaxAmount = 500000,
                        MinFico = 720, // 720+ FICO
                        MinPayNet = 670, // 670+ PayNet
                        MinTimeInBusinessYears = 5, // 5 years time in business
                        MinRevenue = null,
                        MaxEquipmentAgeYears = 5, // Max Age of Collateral = 5 years
                        ExcludeTrucking = false // Includes specific industries
                    },
                    // CORP ONLY GUIDELINES
                    new LendingProgram
                    {
                        Name = "Corp Only",
                        MinAmount = 10000,
                        MaxAmount = null, // Not specified
                        MinFico = null, // Not specified (relies on financials)
                        MinPayNet = null,
                        MinTimeInBusinessYears = 5, // Minimum 5 years in business
                        MinRevenue = 3000000, // Annual sales must be at least $3MM
                        MaxEquipmentAgeYears = null,
                        ExcludeTrucking = false
                    }
                }
            },

            // ============================================================
            // 3. STEARNS BANK (FALCON LEASING)
            // ============================================================
            // Source: EF_Credit_Box_4_14_2025.pdf
            new Lender
            {
                Name = "Stearns Bank",
                RestrictedIndustries = new List<string>
                {
                    "Gaming/Gambling",
                    "Hazmat",
                    "Oil & Gas",
                    "MSBs", // Money Service Businesses
                    "Adult Entertainment",
                    "Non-Essential Use",
                    "Weapons/Firearms",
                    "Beauty/Tanning Salons",
                    "Tattoo/Piercing",
                    "Aesthetic",
                    "Real Estate",
                    "OTR", // Over The Road
                    "Restaurants",
                    "Car Wash"
                },
                RestrictedStates = new List<string>(), // No specific state restrictions
                Programs = new List<LendingProgram>
                {
                    // WITH PayNet - Tier 1
                    new LendingProgram
                    {
                        Name = "Tier 1 (With PayNet)",
                        MinAmount = null,
                        MaxAmount = null,
                        MinFico = 725, // 725 FICO
                        MinPayNet = 685, // 685 PayNet
                        MinTimeInBusinessYears = 3, // 3 years TIB
                        MinRevenue = null,
                        MaxEquipmentAgeYears = null,
                        ExcludeTrucking = false
                    },
                    // WITH PayNet - Tier 2
                    new LendingProgram
                    {
                        Name = "Tier 2 (With PayNet)",
                        MinAmount = null,
                        MaxAmount = null,
                        MinFico = 710, // 710 FICO
                        MinPayNet = 675, // 675 PayNet
                        MinTimeInBusinessYears = 3, // 3 years TIB
                        MinRevenue = null,
                        MaxEquipmentAgeYears = null,
                        ExcludeTrucking = false
                    },
                    // WITH PayNet - Tier 3
                    new LendingProgram
                    {
                        Name = "Tier 3 (With PayNet)",
                        MinAmount = null,
                        MaxAmount = null,
                        MinFico = 700, // 700 FICO
                        MinPayNet = 665, // 665 PayNet
                        MinTimeInBusinessYears = 2, // 2 years TIB
                        MinRevenue = null,
                        MaxEquipmentAgeYears = null,
                        ExcludeTrucking = false
                    },
                    // Corp Only (No PayNet) - Tier 1
                    new LendingProgram
                    {
                        Name = "Corp Only Tier 1",
                        MinAmount = null,
                        MaxAmount = null,
                        MinFico = 735, // 735 FICO
                        MinPayNet = null, // No PayNet required
                        MinTimeInBusinessYears = 5, // 5 years TIB
                        MinRevenue = null,
                        MaxEquipmentAgeYears = null,
                        ExcludeTrucking = false
                    },
                    // Corp Only (No PayNet) - Tier 2
                    new LendingProgram
                    {
                        Name = "Corp Only Tier 2",
                        MinAmount = null,
                        MaxAmount = null,
                        MinFico = 720, // 720 FICO
                        MinPayNet = null,
                        MinTimeInBusinessYears = 3, // 3 years TIB
                        MinRevenue = null,
                        MaxEquipmentAgeYears = null,
                        ExcludeTrucking = false
                    },
                    // Corp Only (No PayNet) - Tier 3
                    new LendingProgram
                    {
                        Name = "Corp Only Tier 3",
                        MinAmount = null,
                        MaxAmount = null,
                        MinFico = 710, // 710 FICO
                        MinPayNet = null,
                        MinTimeInBusinessYears = 2, // 2 years TIB
                        MinRevenue = null,
                        MaxEquipmentAgeYears = null,
                        ExcludeTrucking = false
                    },
                    // If Corp Only (different PayNet requirements)
                    new LendingProgram
                    {
                        Name = "Corp PayNet Tier 1",
                        MinAmount = null,
                        MaxAmount = null,
                        MinFico = null,
                        MinPayNet = 700, // 700 PayNet
                        MinTimeInBusinessYears = 10, // 10 years TIB
                        MinRevenue = null,
                        MaxEquipmentAgeYears = null,
                        ExcludeTrucking = false
                    },
                    new LendingProgram
                    {
                        Name = "Corp PayNet Tier 2",
                        MinAmount = null,
                        MaxAmount = null,
                        MinFico = null,
                        MinPayNet = 690, // 690 PayNet
                        MinTimeInBusinessYears = 5, // 5 years TIB
                        MinRevenue = null,
                        MaxEquipmentAgeYears = null,
                        ExcludeTrucking = false
                    },
                    new LendingProgram
                    {
                        Name = "Corp PayNet Tier 3",
                        MinAmount = null,
                        MaxAmount = null,
                        MinFico = null,
                        MinPayNet = 680, // 680 PayNet
                        MinTimeInBusinessYears = 5, // 5 years TIB
                        MinRevenue = null,
                        MaxEquipmentAgeYears = null,
                        ExcludeTrucking = false
                    }
                }
            },

            // ============================================================
            // 4. FALCON EQUIPMENT FINANCE
            // ============================================================
            // Source: 112025_Rates_-_STANDARD.pdf
            new Lender
            {
                Name = "Falcon Equipment Finance",
                RestrictedIndustries = new List<string>(), // Not explicitly listed
                RestrictedStates = new List<string>(),
                Programs = new List<LendingProgram>
                {
                    // Standard Credit A
                    new LendingProgram
                    {
                        Name = "A Credit",
                        MinAmount = 15000, // Net Financed $15,000+
                        MaxAmount = null, // No explicit max
                        MinFico = 680, // Minimum FICO 680+
                        MinPayNet = 660, // Minimum PayNet 660+
                        MinTimeInBusinessYears = 3, // 3+ Years in business
                        MinRevenue = null,
                        MaxEquipmentAgeYears = 15, // Aged Equipment - for every 15 years
                        ExcludeTrucking = false
                    },
                    // Standard Credit B
                    new LendingProgram
                    {
                        Name = "B Credit",
                        MinAmount = 15000,
                        MaxAmount = null,
                        MinFico = 680, // Same baseline
                        MinPayNet = 660,
                        MinTimeInBusinessYears = 3,
                        MinRevenue = null,
                        MaxEquipmentAgeYears = 15,
                        ExcludeTrucking = false
                    },
                    // Standard Credit C
                    new LendingProgram
                    {
                        Name = "C Credit",
                        MinAmount = 15000,
                        MaxAmount = null,
                        MinFico = 680,
                        MinPayNet = 660,
                        MinTimeInBusinessYears = 3,
                        MinRevenue = null,
                        MaxEquipmentAgeYears = 15,
                        ExcludeTrucking = false
                    },
                    // Standard Credit D
                    new LendingProgram
                    {
                        Name = "D Credit",
                        MinAmount = 15000,
                        MaxAmount = null,
                        MinFico = 680,
                        MinPayNet = 660,
                        MinTimeInBusinessYears = 3,
                        MinRevenue = null,
                        MaxEquipmentAgeYears = 15,
                        ExcludeTrucking = false
                    },
                    // Standard Credit E
                    new LendingProgram
                    {
                        Name = "E Credit",
                        MinAmount = 15000,
                        MaxAmount = null,
                        MinFico = 680,
                        MinPayNet = 660,
                        MinTimeInBusinessYears = 3,
                        MinRevenue = null,
                        MaxEquipmentAgeYears = 15,
                        ExcludeTrucking = false
                    },
                    // Trucking Program (A/B Credits Only)
                    new LendingProgram
                    {
                        Name = "Trucking A/B",
                        MinAmount = 15000,
                        MaxAmount = null,
                        MinFico = 700, // 700 FICO score or better
                        MinPayNet = 680, // 680 or better Paynet Masterscore
                        MinTimeInBusinessYears = 5, // Must have 5+ years in business
                        MinRevenue = null,
                        MaxEquipmentAgeYears = 10, // Class 8 trucks 10 years and newer
                        ExcludeTrucking = false
                    }
                }
            },

            // ============================================================
            // 5. CITIZENS BANK
            // ============================================================
            // Source: 2025_Program_Guidelines_UPDATED.pdf
            new Lender
            {
                Name = "Citizens Bank",
                RestrictedIndustries = new List<string>
                {
                    "Cannabis" // Cannabis related equipment/businesses not desired
                },
                RestrictedStates = new List<string>
                {
                    "CA" // No longer provides financing in California
                },
                Programs = new List<LendingProgram>
                {
                    // Tier 1: General Program
                    new LendingProgram
                    {
                        Name = "Tier 1 General Program",
                        MinAmount = null, // Not specified
                        MaxAmount = 75000, // $75,000 ALL IN (Total Relationship)
                        MinFico = 700, // 700+ Transunion Credit Score
                        MinPayNet = null, // Not mentioned
                        MinTimeInBusinessYears = 2, // 2 years time in business
                        MinRevenue = null,
                        MaxEquipmentAgeYears = null, // Varies by equipment type
                        ExcludeTrucking = false
                    },
                    // Tier 2: Start-up Program / Non-Homeowner Program
                    new LendingProgram
                    {
                        Name = "Tier 2 Start-up/Non-Homeowner",
                        MinAmount = null,
                        MaxAmount = 50000, // $50,000 ALL IN (Total Relationship)
                        MinFico = 700, // 700+ Transunion
                        MinPayNet = null,
                        MinTimeInBusinessYears = 0, // Accepts startups
                        MinRevenue = null,
                        MaxEquipmentAgeYears = null,
                        ExcludeTrucking = false
                    },
                    // Tier 3: Full Financials
                    new LendingProgram
                    {
                        Name = "Tier 3 Full Financials",
                        MinAmount = 75000, // Transactions $75,000 - $1,000,000
                        MaxAmount = 1000000,
                        MinFico = null, // Not specified (relies on financials)
                        MinPayNet = null,
                        MinTimeInBusinessYears = null, // Not specified
                        MinRevenue = null,
                        MaxEquipmentAgeYears = null,
                        ExcludeTrucking = false
                    }
                }
            }
        };

        context.Lenders.AddRange(lenders);
        context.SaveChanges();
    }
}