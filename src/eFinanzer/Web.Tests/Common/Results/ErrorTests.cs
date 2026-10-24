using System.Text.Json;
using System.Text.Json.Nodes;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Common.Tasks.GetRoleChanges.Errors;

namespace eFinanzer.Tests.Common.Results;

public class ErrorTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Serialize_Error_ContainsItsClassNameAndTheOneOfItsParent()
    {
        // Arrange
        var error = new ChangeUserRoleError("alice", "Treasurer", Added: true, new NotFoundError("User", "id-alice"));

        // Act
        var json = JsonNode.Parse(JsonSerializer.Serialize(error, JsonOptions))!;

        // Assert
        Assert.Equal(nameof(ChangeUserRoleError), json["type"]!.GetValue<string>());
        Assert.Equal(nameof(NotFoundError), json["parentError"]!["type"]!.GetValue<string>());
    }

    [Fact]
    public void Equals_SameValues_IsEqualDespiteTheComputedType()
    {
        Assert.Equal(new UnknownUserError("alice"), new UnknownUserError("alice"));
    }
}
