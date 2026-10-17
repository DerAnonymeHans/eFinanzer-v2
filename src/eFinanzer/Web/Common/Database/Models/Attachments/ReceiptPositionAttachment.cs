namespace eFinanzer.Common.Database.Models.Attachments;

public record ReceiptPositionAttachment : IAttachment
{
    public int Id { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    
    public int ReceiptPositionId { get; init; }
    public ReceiptPosition? ReceiptPosition { get; init; }
}