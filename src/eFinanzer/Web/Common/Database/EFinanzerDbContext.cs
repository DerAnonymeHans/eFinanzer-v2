using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Database.Models.Attachments;
using Microsoft.EntityFrameworkCore;

namespace eFinanzer.Common.Database;

public class EFinanzerDbContext(DbContextOptions<EFinanzerDbContext> options) : DbContext(options)
{
    public DbSet<AccountingUnit> AccountingUnits => Set<AccountingUnit>();
    public DbSet<FiscalYear> FiscalYears => Set<FiscalYear>();

    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<BudgetGroup> BudgetGroups => Set<BudgetGroup>();
    public DbSet<BudgetPosition> BudgetPositions => Set<BudgetPosition>();

    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<ReceiptPosition> ReceiptPositions => Set<ReceiptPosition>();

    public DbSet<BudgetAttachment> BudgetAttachments => Set<BudgetAttachment>();
    public DbSet<ReceiptAttachment> ReceiptAttachments => Set<ReceiptAttachment>();
    public DbSet<ReceiptPositionAttachment> ReceiptPositionAttachments => Set<ReceiptPositionAttachment>();
    
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

        modelBuilder.Entity<Budget>(entity =>
        {
            entity.HasMany(b => b.Groups)
                .WithOne(g => g.Budget)
                .HasForeignKey(g => g.BudgetId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(b => b.Attachments)
                .WithOne(a => a.Budget)
                .HasForeignKey(a => a.BudgetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BudgetGroup>(entity =>
        {
            entity.HasMany(g => g.Positions)
                .WithOne(p => p.BudgetGroup)
                .HasForeignKey(p => p.BudgetGroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BudgetPosition>(entity =>
        {
            entity.Property(p => p.PlannedExpenses).HasPrecision(18, 2);
            entity.Property(p => p.PlannedIncome).HasPrecision(18, 2);

            // Self reference to the "Deckelungstopf". Deleting the pot only detaches the positions covered by it.
            entity.HasOne(p => p.DeficitBudgetPosition)
                .WithMany()
                .HasForeignKey(p => p.DeficitBudgetPositionId)
                .OnDelete(DeleteBehavior.SetNull);

            // Receipts must not be lost or silently detached by deleting the budget they were booked on
            entity.HasMany(p => p.Receipts)
                .WithOne(r => r.BudgetPosition)
                .HasForeignKey(r => r.BudgetPositionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Receipt>(entity =>
        {
            entity.HasIndex(r => new { r.AccountingUnitKey, r.FiscalYearKey, r.Key }).IsUnique();

            entity.ComplexProperty(r => r.Receiver, receiver => receiver.ToJson());

            entity.HasMany(r => r.Positions)
                .WithOne(p => p.Receipt)
                .HasForeignKey(p => p.ReceiptId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(r => r.Attachments)
                .WithOne(a => a.Receipt)
                .HasForeignKey(a => a.ReceiptId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReceiptPosition>(entity =>
        {
            entity.Property(p => p.Amount).HasPrecision(18, 2);
            entity.Property(p => p.DepositAmount).HasPrecision(18, 2);

            entity.HasMany(p => p.Attachments)
                .WithOne(a => a.ReceiptPosition)
                .HasForeignKey(a => a.ReceiptPositionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoleChangeRequest>(entity =>
        {
            entity.ComplexCollection(r => r.RoleChanges, roleChanges => roleChanges.ToJson());

            entity.HasIndex(r => r.RequestedBy);
        });
    }
}
