using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LenderMatch.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Borrowers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BusinessName = table.Column<string>(type: "text", nullable: false),
                    Industry = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false),
                    YearsInBusiness = table.Column<decimal>(type: "numeric", nullable: false),
                    AnnualRevenue = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Borrowers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CreditProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PayNetScore = table.Column<int>(type: "integer", nullable: true),
                    TradeLineCount = table.Column<int>(type: "integer", nullable: false),
                    HasComparableDebt = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Guarantors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    FicoScore = table.Column<int>(type: "integer", nullable: false),
                    HasBankruptcy = table.Column<bool>(type: "boolean", nullable: false),
                    HasTaxLiens = table.Column<bool>(type: "boolean", nullable: false),
                    BankruptcyDischargeYears = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guarantors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lenders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    RestrictedIndustries = table.Column<List<string>>(type: "text[]", nullable: false),
                    RestrictedStates = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lenders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoanRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    TermMonths = table.Column<int>(type: "integer", nullable: false),
                    EquipmentType = table.Column<string>(type: "text", nullable: false),
                    EquipmentYear = table.Column<int>(type: "integer", nullable: false),
                    EquipmentMileage = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Programs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LenderId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    MinAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    MaxAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    MinFico = table.Column<int>(type: "integer", nullable: true),
                    MinPayNet = table.Column<int>(type: "integer", nullable: true),
                    MinTimeInBusinessYears = table.Column<decimal>(type: "numeric", nullable: true),
                    MinRevenue = table.Column<decimal>(type: "numeric", nullable: true),
                    MaxEquipmentAgeYears = table.Column<int>(type: "integer", nullable: true),
                    ExcludeTrucking = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Programs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Programs_Lenders_LenderId",
                        column: x => x.LenderId,
                        principalTable: "Lenders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoanApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BorrowerId = table.Column<int>(type: "integer", nullable: false),
                    GuarantorId = table.Column<int>(type: "integer", nullable: false),
                    BusinessCreditId = table.Column<int>(type: "integer", nullable: false),
                    LoanRequestId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanApplications_Borrowers_BorrowerId",
                        column: x => x.BorrowerId,
                        principalTable: "Borrowers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LoanApplications_CreditProfiles_BusinessCreditId",
                        column: x => x.BusinessCreditId,
                        principalTable: "CreditProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LoanApplications_Guarantors_GuarantorId",
                        column: x => x.GuarantorId,
                        principalTable: "Guarantors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LoanApplications_LoanRequests_LoanRequestId",
                        column: x => x.LoanRequestId,
                        principalTable: "LoanRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoanApplications_BorrowerId",
                table: "LoanApplications",
                column: "BorrowerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoanApplications_BusinessCreditId",
                table: "LoanApplications",
                column: "BusinessCreditId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoanApplications_GuarantorId",
                table: "LoanApplications",
                column: "GuarantorId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoanApplications_LoanRequestId",
                table: "LoanApplications",
                column: "LoanRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Programs_LenderId",
                table: "Programs",
                column: "LenderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoanApplications");

            migrationBuilder.DropTable(
                name: "Programs");

            migrationBuilder.DropTable(
                name: "Borrowers");

            migrationBuilder.DropTable(
                name: "CreditProfiles");

            migrationBuilder.DropTable(
                name: "Guarantors");

            migrationBuilder.DropTable(
                name: "LoanRequests");

            migrationBuilder.DropTable(
                name: "Lenders");
        }
    }
}
