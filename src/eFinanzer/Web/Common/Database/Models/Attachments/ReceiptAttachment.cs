namespace eFinanzer.Common.Database.Models.Attachments;

public record ReceiptAttachment : IAttachment
{
    public int Id { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    
    public int ReceiptId { get; init; }
    public Receipt? Receipt { get; init; }
}