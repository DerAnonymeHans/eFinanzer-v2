namespace eFinanzer.Common.Database.Models;

public record BudgetPosition
{
    public int Id { get; init; }
    public decimal PlannedExpenses { get; init; }
    public decimal PlannedIncome { get; init; }
    
    public int? DeficitBudgetPositionId { get; init; }
    /// <summary>
    /// "Deckelungstopf" in case the expenses are higher than planned expenses
    /// </summary>
    public BudgetPosition? DeficitBudgetPosition { get; init; }
    
    public int BudgetGroupId { get; init; }
    public BudgetGroup? BudgetGroup { get; init; }
    
    public List<Receipt>? Receipts { get; init; }
}