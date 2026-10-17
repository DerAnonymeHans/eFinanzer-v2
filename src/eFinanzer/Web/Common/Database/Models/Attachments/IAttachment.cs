namespace eFinanzer.Common.Database.Models.Attachments;

public interface IAttachment
{
    public string FileName { get; init; }
    public string ContentType { get; init; }
}