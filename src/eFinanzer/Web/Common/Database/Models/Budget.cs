using System.ComponentModel.DataAnnotations;
using eFinanzer.Common.Database.Models.Attachments;

namespace eFinanzer.Common.Database.Models;

public record Budget
{
    public int Id { get; init; }
    
    public DateTimeOffset CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    
    public DateTimeOffset? ApprovedAt { get; init; }
    public string? ApprovedBy { get; init; }
    
    [MaxLength(10)]
    public required string AccountingUnitKey { get; init; }
    
    [MaxLength(10)]
    public required string FiscalYearKey { get; init; }
    public FiscalYear? FiscalYear { get; init; }
    
    public List<BudgetGroup>? Groups { get; init; }
    
    public List<BudgetAttachment>? Attachments { get; init; }
};