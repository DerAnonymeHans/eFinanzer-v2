using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization;
using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Integrations.Keycloak.Constants;
using eFinanzer.Common.Models;
using eFinanzer.Features.ManageAccountingUnits.Tasks.GetAccountingUnit.Models;
using eFinanzer.Tests.Infrastructure;
using NSubstitute;

namespace eFinanzer.Tests.Features.ManageAccountingUnits.Endpoints;

/// <summary>
/// The tasks are covered by their own tests, these only cover what the endpoints add: authorization and the mapping
/// to HTTP
/// </summary>
public class ManageAccountingUnitsEndpointTests(DatabaseFixture database) : DatabaseTest(database)
{
    private const string BaseUrl = "/api/manage-accounting-units";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly EFinanzerApiFactory _factory = new(database);

    public override async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        await base.DisposeAsync();
    }

    public static TheoryData<string, string> AllEndpoints => new()
    {
        { "POST", $"{BaseUrl}/create-accounting-unit" },
        { "GET", $"{BaseUrl}/list-accounting-units" },
        { "GET", $"{BaseUrl}/get-accounting-unit?key=fsr" },
        { "PUT", $"{BaseUrl}/update-accounting-unit?key=fsr" },
        { "DELETE", $"{BaseUrl}/delete-accounting-unit?key=fsr" },
    };

    private static HttpRequestMessage CreateRequest(string method, string url) => new(new HttpMethod(method), url)
    {
        // Valid body for the create and update endpoints, so the request reaches the authorization check
        Content = method switch
        {
            "POST" => Form(fields: ("Key", "fsr")),
            "PUT" => Form(fields: ("Name", "FSR")),
            _ => null,
        },
    };

    /// <summary>
    /// multipart/form-data body as sent by the frontend
    /// </summary>
    private static MultipartFormDataContent Form(byte[]? logo = null, string logoFileName = "logo.png",
        params (string Name, string Value)[] fields)
    {
        var form = new MultipartFormDataContent();
        foreach (var (name, value) in fields)
            form.Add(new StringContent(value), name);

        if (logo is not null)
        {
            var file = new ByteArrayContent(logo);
            file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(file, "Logo", logoFileName);
        }

        return form;
    }

    [Theory]
    [MemberData(nameof(AllEndpoints))]
    public async Task Endpoint_AnonymousUser_ReturnsUnauthorized(string method, string url)
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.SendAsync(CreateRequest(method, url), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(AllEndpoints))]
    public async Task Endpoint_UserWithoutManagingRole_ReturnsForbidden(string method, string url)
    {
        // Arrange
        var client = _factory.CreateAuthorizedClient(
            KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceReviewer),
            KeycloakGroups.GetAccountingUnitRoleGroup("fsr", AccountingUnitRole.FinanceOfficer));

        // Act
        var response = await client.SendAsync(CreateRequest(method, url), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(AdministrationRole.HeadFinanceOfficer)]
    [InlineData(AdministrationRole.FinanceAdministrator)]
    public async Task Create_UserWithManagingRole_ReturnsCreated(AdministrationRole role)
    {
        // Arrange
        _factory.KeycloakUserClient.SetupGroupCreation();
        var client = _factory.CreateAuthorizedClient(KeycloakGroups.GetAdministrationRoleGroup(role));

        // Act
        var response = await client.PostAsync($"{BaseUrl}/create-accounting-unit", Form(fields: ("Key", "fsr")),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task Create_InvalidKey_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateAuthorizedClient(KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceAdministrator));

        // Act
        var response = await client.PostAsync($"{BaseUrl}/create-accounting-unit", Form(fields: ("Key", "fsr inf")),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_KeyAlreadyExists_ReturnsConflict()
    {
        // Arrange
        Context.AccountingUnits.Add(new AccountingUnit { Key = "fsr" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var client = _factory.CreateAuthorizedClient(KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceAdministrator));

        // Act
        var response = await client.PostAsync($"{BaseUrl}/create-accounting-unit", Form(fields: ("Key", "fsr")),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Get_ExistingUnit_ReturnsUnitWithRolesAsStrings()
    {
        // Arrange
        Context.AccountingUnits.Add(new AccountingUnit { Key = "fsr", Name = "FSR" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _factory.KeycloakUserClient.SetupRoleMembers("fsr", (AccountingUnitRole.Treasurer, ["alice"]));
        var client = _factory.CreateAuthorizedClient(KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.HeadFinanceOfficer));

        // Act
        var response = await client.GetAsync($"{BaseUrl}/get-accounting-unit?key=fsr", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("\"Treasurer\"", json);
        var dto = JsonSerializer.Deserialize<GetAccountingUnitManagementDto>(json, JsonOptions)!;
        Assert.Equal("FSR", dto.Name);
        var user = Assert.Single(dto.UsersWithRoles);
        Assert.Equal("alice", user.UserName);
        Assert.Equal([AccountingUnitRole.Treasurer], user.Roles);
    }

    [Fact]
    public async Task Delete_UnknownKey_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateAuthorizedClient(KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceAdministrator));

        // Act
        var response = await client.DeleteAsync($"{BaseUrl}/delete-accounting-unit?key=unknown", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("UsersWithRoles[0].UserName", "alice", "UsersWithRoles[0].Roles[0]", "99")]
    [InlineData("UsersWithRoles[0].UserName", "alice", "UsersWithRoles[0].Roles[0]", "Unknown")]
    [InlineData("UsersWithRoles[0].UserName", "", "UsersWithRoles[0].Roles[0]", "Treasurer")]
    public async Task Update_InvalidBody_ReturnsBadRequest(string field1, string value1, string field2, string value2)
    {
        // Arrange
        Context.AccountingUnits.Add(new AccountingUnit { Key = "fsr" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var client = _factory.CreateAuthorizedClient(KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceAdministrator));

        // Act
        var response = await client.PutAsync($"{BaseUrl}/update-accounting-unit?key=fsr",
            Form(fields: [(field1, value1), (field2, value2)]), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(_factory.KeycloakUserClient.ReceivedCalls());
    }

    [Fact]
    public async Task Update_UsersWithRolesAsFormFields_AppliesRoles()
    {
        // Arrange
        Context.AccountingUnits.Add(new AccountingUnit { Key = "fsr" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _factory.KeycloakUserClient.SetupRoleMembers("fsr");
        _factory.KeycloakUserClient.SetupUsers("alice");
        var client = _factory.CreateAuthorizedClient(KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceAdministrator));

        // Act
        var response = await client.PutAsync($"{BaseUrl}/update-accounting-unit?key=fsr",
            Form(fields: [
                ("Name", "FSR"),
                ("UsersWithRoles[0].UserName", "alice"),
                ("UsersWithRoles[0].Roles[0]", "Treasurer"),
                ("UsersWithRoles[0].Roles[1]", "FinanceOfficer"),
            ]),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await _factory.KeycloakUserClient.Received(2).AddUserToGroup("id-alice", Arg.Any<string>(), Arg.Any<CancellationToken>());
        Assert.Equal("FSR", Context.AccountingUnits.Single().Name);
    }

    [Fact]
    public async Task CreateAndLoadLogo_ValidLogo_ReturnsStoredLogo()
    {
        // Arrange
        _factory.KeycloakUserClient.SetupGroupCreation();
        var manager = _factory.CreateAuthorizedClient(KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceAdministrator));
        // Every logged-in user may load logos
        var user = _factory.CreateAuthorizedClient(KeycloakGroups.GetAccountingUnitRoleGroup("other", AccountingUnitRole.FinanceOfficer));

        // Act
        var createResponse = await manager.PostAsync($"{BaseUrl}/create-accounting-unit",
            Form(TestFiles.Png, fields: ("Key", "fsr")), TestContext.Current.CancellationToken);
        var loadResponse = await user.GetAsync($"{BaseUrl}/load-accounting-unit-logo?key=fsr", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, loadResponse.StatusCode);
        Assert.Equal(MediaTypeNames.Image.Png, loadResponse.Content.Headers.ContentType?.MediaType);
        Assert.Equal(TestFiles.Png, await loadResponse.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateAndLoadLogo_ValidLogo_ReturnsReplacedLogo()
    {
        // Arrange
        Context.AccountingUnits.Add(new AccountingUnit { Key = "fsr" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _factory.KeycloakUserClient.SetupRoleMembers("fsr");
        var client = _factory.CreateAuthorizedClient(KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceAdministrator));
        await client.PutAsync($"{BaseUrl}/update-accounting-unit?key=fsr", Form(TestFiles.Png),
            TestContext.Current.CancellationToken);

        // Act
        var updateResponse = await client.PutAsync($"{BaseUrl}/update-accounting-unit?key=fsr",
            Form(TestFiles.Jpeg, "logo.jpg"), TestContext.Current.CancellationToken);
        var loadResponse = await client.GetAsync($"{BaseUrl}/load-accounting-unit-logo?key=fsr", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);
        Assert.Equal(MediaTypeNames.Image.Jpeg, loadResponse.Content.Headers.ContentType?.MediaType);
        Assert.Equal(TestFiles.Jpeg, await loadResponse.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("POST", $"{BaseUrl}/create-accounting-unit")]
    [InlineData("PUT", $"{BaseUrl}/update-accounting-unit?key=fsr")]
    public async Task CreateAndUpdate_InvalidLogo_ReturnsBadRequest(string method, string url)
    {
        // Arrange
        Context.AccountingUnits.Add(new AccountingUnit { Key = "fsr" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var client = _factory.CreateAuthorizedClient(KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceAdministrator));
        var request = new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = Form(TestFiles.Gif, "logo.gif", ("Key", "new")),
        };

        // Act
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Logo", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task LoadLogo_Svg_IsServedWithPolicyBlockingScripts()
    {
        // Arrange
        _factory.KeycloakUserClient.SetupGroupCreation();
        var client = _factory.CreateAuthorizedClient(KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceAdministrator));
        await client.PostAsync($"{BaseUrl}/create-accounting-unit", Form(TestFiles.Svg, "logo.svg", ("Key", "fsr")),
            TestContext.Current.CancellationToken);

        // Act
        var response = await client.GetAsync($"{BaseUrl}/load-accounting-unit-logo?key=fsr", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(MediaTypeNames.Image.Svg, response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(TestFiles.Svg, await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
        var policy = Assert.Single(response.Headers.GetValues("Content-Security-Policy"));
        Assert.Contains("default-src 'none'", policy);
        Assert.Contains("sandbox", policy);
        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
    }

    [Fact]
    public async Task LoadLogo_AnonymousUser_ReturnsLogo()
    {
        // Arrange
        _factory.KeycloakUserClient.SetupGroupCreation();
        var manager = _factory.CreateAuthorizedClient(KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceAdministrator));
        await manager.PostAsync($"{BaseUrl}/create-accounting-unit", Form(TestFiles.Png, fields: ("Key", "fsr")),
            TestContext.Current.CancellationToken);
        var anonymous = _factory.CreateClient();

        // Act
        var response = await anonymous.GetAsync($"{BaseUrl}/load-accounting-unit-logo?key=fsr", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(TestFiles.Png, await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("fsr")]
    public async Task LoadLogo_UnknownUnitOrNoLogo_ReturnsNotFound(string key)
    {
        // Arrange
        Context.AccountingUnits.Add(new AccountingUnit { Key = "fsr" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var client = _factory.CreateAuthorizedClient();

        // Act
        var response = await client.GetAsync($"{BaseUrl}/load-accounting-unit-logo?key={key}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
