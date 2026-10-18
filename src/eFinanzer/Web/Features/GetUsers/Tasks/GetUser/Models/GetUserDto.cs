namespace eFinanzer.Features.GetUsers.Tasks.GetUser.Models;

public record GetUserDto
{
    public required string Username { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
}
