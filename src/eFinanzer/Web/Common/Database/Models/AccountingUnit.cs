namespace eFinanzer.Common.Database.Models;

public record AccountingUnit
{
    public int Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    
    public List<FiscalYear>? FiscalYears { get; init; }
}