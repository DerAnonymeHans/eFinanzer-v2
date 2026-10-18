using System.ComponentModel.DataAnnotations;

namespace eFinanzer.Common.Database.Models;

public record FiscalYear
{
    [MaxLength(10)]
    public required string Key { get; init; }
    [MaxLength(10)]
    public required string AccountingUnitKey { get; init; }
    public AccountingUnit? AccountingUnit { get; init; }
    
    public string? Name { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    
    
    public List<Budget>? Budgets { get; init; }
    public List<Receipt>? Receipts { get; init; }
}