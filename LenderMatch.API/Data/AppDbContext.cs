using LenderMatch.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace LenderMatch.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // --- Core Tables ---
    public DbSet<Lender> Lenders { get; set; }
    public DbSet<LendingProgram> Programs { get; set; }
    public DbSet<LoanApplication> LoanApplications { get; set; }

    // --- New Tables for Application Components ---
    // Registering these ensures they get their own tables (e.g., "Borrowers", "Guarantors")
    public DbSet<Borrower> Borrowers { get; set; }
    public DbSet<PersonalGuarantor> Guarantors { get; set; }
    public DbSet<BusinessCredit> CreditProfiles { get; set; }
    public DbSet<LoanRequest> LoanRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Configure Lender -> Programs (One-to-Many)
        modelBuilder.Entity<Lender>()
            .HasMany(l => l.Programs)
            .WithOne()
            .HasForeignKey(p => p.LenderId)
            .OnDelete(DeleteBehavior.Cascade);

        // 2. Configure LoanApplication Relationships (One-to-One)
        // When an Application is deleted, its parts (Borrower, Guarantor, etc.) should also be deleted.

        modelBuilder.Entity<LoanApplication>()
            .HasOne(a => a.Business)
            .WithOne()
            .HasForeignKey<LoanApplication>("BorrowerId") // Shadow FK
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LoanApplication>()
            .HasOne(a => a.Guarantor)
            .WithOne()
            .HasForeignKey<LoanApplication>("GuarantorId")
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LoanApplication>()
            .HasOne(a => a.CreditProfile)
            .WithOne()
            .HasForeignKey<LoanApplication>("BusinessCreditId")
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LoanApplication>()
            .HasOne(a => a.Request)
            .WithOne()
            .HasForeignKey<LoanApplication>("LoanRequestId")
            .OnDelete(DeleteBehavior.Cascade);

        // 3. PostgreSQL Specific: Handle List<string> as Arrays
        // Npgsql automatically maps List<string> to text[] columns in Postgres.
        // No special configuration is strictly required, but this ensures explicit definition.
        modelBuilder.Entity<Lender>()
            .Property(e => e.RestrictedIndustries)
            .HasColumnType("text[]");

        modelBuilder.Entity<Lender>()
            .Property(e => e.RestrictedStates)
            .HasColumnType("text[]");

        // Note: MatchResult is not added as a DbSet because your model 
        // does not have a Primary Key (Id). It is treated as a DTO (Data Transfer Object).
    }
}