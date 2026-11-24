using eFinanzer.Common.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace eFinanzer.Common.Database;

public class EFinanzerDbContext(DbContextOptions<EFinanzerDbContext> options) : DbContext(options)
{
    public DbSet<AccountingUnit> AccountingUnits => Set<AccountingUnit>();
    public DbSet<FiscalYear> FiscalYears => Set<FiscalYear>();

    public DbSet<Budget> Budgets => Set<Budget>();

    public DbSet<Receipt> Receipts => Set<Receipt>();
    
    public DbSet<RoleChangeRequest> RoleChangeRequests => Set<RoleChangeRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AccountingUnit>(entity =>
        {
            entity.HasKey(a => a.Key);

            entity.HasMany(a => a.FiscalYears)
                .WithOne(f => f.AccountingUnit)
                .HasForeignKey(f => f.AccountingUnitKey)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(a => a.Receipts)
                .WithOne(r => r.AccountingUnit)
                .HasForeignKey(r => r.AccountingUnitKey)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasMany(x => x.Budgets)
                .WithOne(x => x.AccountingUnit)
                .HasForeignKey(x => x.AccountingUnitKey)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FiscalYear>(entity =>
        {
            entity.HasKey(f => new { f.AccountingUnitKey, f.Key });

            entity.HasMany(f => f.Budgets)
                .WithOne(b => b.FiscalYear)
                .HasForeignKey(b => new { b.AccountingUnitKey, b.FiscalYearKey })
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(f => f.Receipts)
                .WithOne(r => r.FiscalYear)
                .HasForeignKey(r => new { r.AccountingUnitKey, r.FiscalYearKey })
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RoleChangeRequest>(entity =>
        {
            entity.ComplexCollection(r => r.RoleChanges, roleChanges => roleChanges.ToJson());

            entity.HasIndex(r => r.RequestedBy);
        });
    }
}
