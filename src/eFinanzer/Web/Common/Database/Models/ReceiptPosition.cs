using eFinanzer.Common.Database.Models.Attachments;
using eFinanzer.Common.Models;

namespace eFinanzer.Common.Database.Models;

public record ReceiptPosition
{
    public int Id { get; init; }
    public string? Title { get; init; }
    
    public decimal Amount { get; init; }
    /// <summary>
    /// Pfand
    /// </summary>
    public decimal DepositAmount { get; init; }
    
    public ReceiptPositionState State { get; init; }
    
    public string? PaidBy { get; init; }
    
    public int ReceiptId { get; init; }
    public Receipt? Receipt { get; init; }


    public List<ReceiptPositionAttachment>? Attachments { get; init; }
}