using LenderMatch.API.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace LenderMatch.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Lender> Lenders { get; set; }
    public DbSet<LendingProgram> Programs { get; set; }
    public DbSet<LoanApplication> Applications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Store simple lists as JSON/Arrays (Postgres feature) or simple splitting strings
        // For simplicity in this demo, we assume EF Core handles primitives or we stick to basic types.
        base.OnModelCreating(modelBuilder);
    }
}