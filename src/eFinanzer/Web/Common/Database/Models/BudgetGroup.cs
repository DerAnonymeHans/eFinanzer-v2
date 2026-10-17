namespace eFinanzer.Common.Database.Models;

public record BudgetGroup
{
    public int Id { get; init; }
    
    public string? Name { get; init; }
    
    public int BudgetId { get; init; }
    public Budget? Budget { get; init; }
    
    public List<BudgetPosition>? Positions { get; init; }
}