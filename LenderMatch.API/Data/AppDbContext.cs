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
    public DbSet<Borrower> Borrowers { get; set; }
    public DbSet<PersonalGuarantor> Guarantors { get; set; }
    public DbSet<BusinessCredit> CreditProfiles { get; set; }
    public DbSet<LoanRequest> LoanRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Explicit: configure LendingProgram as dependent so EF uses the existing LenderId ---
        modelBuilder.Entity<LendingProgram>(eb =>
        {
            eb.HasKey(p => p.Id);

            eb.HasOne(p => p.Lender)
              .WithMany(l => l.Programs)
              .HasForeignKey(p => p.LenderId)
              .HasPrincipalKey(l => l.Id)
              .OnDelete(DeleteBehavior.Cascade);
        });

        // Keep LoanApplication mappings as before (one-to-one using shadow FKs),
        // or add explicit FK properties on LoanApplication if you prefer non-shadow keys.
        modelBuilder.Entity<LoanApplication>()
            .HasOne(a => a.Business)
            .WithOne()
            .HasForeignKey<LoanApplication>("BorrowerId")
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

        // PostgreSQL: ensure List<string> -> text[] mapping
        modelBuilder.Entity<Lender>()
            .Property(e => e.RestrictedIndustries)
            .HasColumnType("text[]");

        modelBuilder.Entity<Lender>()
            .Property(e => e.RestrictedStates)
            .HasColumnType("text[]");
    }
}