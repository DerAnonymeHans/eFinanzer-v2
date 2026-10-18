using eFinanzer.Common.Helper;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace eFinanzer.Tests.Common.Helper;

public class ILoggerExtensionsTests
{
    private readonly ILogger _logger = Substitute.For<ILogger>();

    [Fact]
    public void AddContext_Chained_BeginsOneScopePerValue()
    {
        // Act
        using var logContext = _logger.AddContext("Username", "alice").AddContext("AccountingUnit", "fsr");

        // Assert
        _logger.Received(1).BeginScope(Arg.Is<Dictionary<string, object>>(x =>
            x.Count == 1 && (string)x["Username"] == "alice"));
        _logger.Received(1).BeginScope(Arg.Is<Dictionary<string, object>>(x =>
            x.Count == 1 && (string)x["AccountingUnit"] == "fsr"));
    }

    [Fact]
    public void Dispose_EndsScopesInReverseOrder()
    {
        // Arrange
        var disposed = new List<string>();
        var usernameScope = Substitute.For<IDisposable>();
        usernameScope.When(x => x.Dispose()).Do(_ => disposed.Add("Username"));
        var accountingUnitScope = Substitute.For<IDisposable>();
        accountingUnitScope.When(x => x.Dispose()).Do(_ => disposed.Add("AccountingUnit"));
        _logger.BeginScope(Arg.Is<Dictionary<string, object>>(x => x.ContainsKey("Username"))).Returns(usernameScope);
        _logger.BeginScope(Arg.Is<Dictionary<string, object>>(x => x.ContainsKey("AccountingUnit"))).Returns(accountingUnitScope);
        var logContext = _logger.AddContext("Username", "alice").AddContext("AccountingUnit", "fsr");

        // Act
        logContext.Dispose();

        // Assert
        Assert.Equal(["AccountingUnit", "Username"], disposed);
    }

    [Fact]
    public void Dispose_CalledTwice_EndsScopesOnce()
    {
        // Arrange
        var scope = Substitute.For<IDisposable>();
        _logger.BeginScope(Arg.Any<Dictionary<string, object>>()).Returns(scope);
        var logContext = _logger.AddContext("Username", "alice");

        // Act
        logContext.Dispose();
        logContext.Dispose();

        // Assert
        scope.Received(1).Dispose();
    }
}
