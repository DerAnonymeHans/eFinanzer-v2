using System.Security.Claims;
using System.Text.Encodings.Web;
using eFinanzer.Common.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace eFinanzer.Tests.Infrastructure;

/// <summary>
/// Replaces the Keycloak token validation. Requests with the <see cref="GroupsHeader"/> are authenticated as a user
/// in the listed groups (comma separated full group paths), requests without it stay anonymous.
/// </summary>
public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder
    ) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string GroupsHeader = "X-Test-Groups";
    public const string Username = "test-user";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(GroupsHeader, out var groupsHeader))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = groupsHeader.ToString()
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(group => new Claim(UserClaims.GroupClaimType, group))
            .Append(new Claim("sub", "test-user"))
            .Append(new Claim(UserClaims.UsernameClaimType, Username));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
