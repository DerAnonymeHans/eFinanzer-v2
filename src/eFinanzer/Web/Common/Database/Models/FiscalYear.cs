namespace eFinanzer.Common.Database.Models;

public record FiscalYear
{
    public int Id { get; init; }
    public string? Name { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    
    public int AccountingUnitId { get; init; }
    public AccountingUnit? AccountingUnit { get; init; }
    
    public List<Receipt>? Receipts { get; init; }
}