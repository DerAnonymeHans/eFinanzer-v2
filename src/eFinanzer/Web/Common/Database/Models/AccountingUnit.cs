using System.ComponentModel.DataAnnotations;

namespace eFinanzer.Common.Database.Models;

public record AccountingUnit
{
    [MaxLength(10)]
    public required string Key { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    
    public List<FiscalYear>? FiscalYears { get; init; }
    public List<Receipt>? Receipts { get; init; }
}