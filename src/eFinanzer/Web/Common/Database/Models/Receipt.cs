using System.ComponentModel.DataAnnotations;

namespace eFinanzer.Common.Database.Models;


public record Receipt
{
    public int Id { get; init; }
    
    public DateOnly ReceiptAt { get; init; }
    
    [MaxLength(10)]
    public required string AccountingUnitKey { get; init; }
    public AccountingUnit? AccountingUnit { get; init; }
    
    [MaxLength(10)]
    public required string FiscalYearKey { get; init; }
    public FiscalYear? FiscalYear { get; init; }
}