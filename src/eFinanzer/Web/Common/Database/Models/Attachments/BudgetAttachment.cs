namespace eFinanzer.Common.Database.Models.Attachments;

public record BudgetAttachment : IAttachment
{
    public int Id { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    
    public int BudgetId { get; init; }
    public Budget? Budget { get; init; }
}