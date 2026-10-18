namespace eFinanzer.Common.Helper;

public static class ILoggerExtensions
{
    /// <summary>
    /// Adds the value to all log entries written until the returned context is disposed. Further values can be
    /// chained with <see cref="LogContext.AddContext"/>.
    /// </summary>
    public static LogContext AddContext(this ILogger logger, string key, object value)
        => new LogContext(logger).AddContext(key, value);

    public sealed class LogContext(ILogger logger) : IDisposable
    {
        // A scope captures its values when it is begun, so every added value gets its own scope
        private readonly Stack<IDisposable?> _scopes = new();

        public LogContext AddContext(string key, object value)
        {
            _scopes.Push(logger.BeginScope(new Dictionary<string, object> { [key] = value }));
            return this;
        }

        public void Dispose()
        {
            // Scopes have to be ended in reverse order
            while (_scopes.TryPop(out var scope))
                scope?.Dispose();
        }
    }
}
