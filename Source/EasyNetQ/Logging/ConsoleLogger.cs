using System;

namespace EasyNetQ.Logging;

/// <inheritdoc />
public sealed class ConsoleLogger<TCategoryName> : ILogger<TCategoryName>
{
    /// <inheritdoc />
    public bool Log(
        LogLevel logLevel,
        Func<string>? messageFunc,
        Exception? exception = null,
        params object?[] formatParameters
    )
    {
        if (messageFunc == null) return true;

        var consoleColor = logLevel switch
        {
            LogLevel.Trace => ConsoleColor.DarkGray,
            LogLevel.Debug => ConsoleColor.Gray,
            LogLevel.Info => ConsoleColor.White,
            LogLevel.Warn => ConsoleColor.Magenta,
            LogLevel.Error => ConsoleColor.Yellow,
            LogLevel.Fatal => ConsoleColor.Red,
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null)
        };

        var message = MessageFormatter.FormatStructuredMessage(messageFunc(), formatParameters, out _);
        if (exception != null) message += " -> " + exception;

        ConcurrentColoredConsole.WriteLine(consoleColor, $"[{DateTime.UtcNow:HH:mm:ss} {logLevel}] {message}");

        return true;
    }
}
