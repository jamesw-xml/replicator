// Copyright (c) Kurrent, Inc and/or licensed to Kurrent, Inc under one or more agreements.
// Kurrent, Inc licenses this file to you under the Kurrent License v1 (see LICENSE.md).

namespace Kurrent.Replicator.Tests.Logging;
    using Serilog;
    using Kurrent.Replicator.Shared.Logging;

public class SerilogLogProvider : ILogProvider
{
    ILogger _logger = new Serilog.LoggerConfiguration()
            .WriteTo.TestOutput()
            .CreateLogger();
    public Logger GetLogger(string name)
    {
        return new Logger(new SerilogLogger(_logger).Log);
    }

    public IDisposable OpenNestedContext(string message)
    {
        // Serilog does not natively support nested contexts, so return a no-op disposable.
        return new NoOpDisposable();
    }

    public IDisposable OpenMappedContext(string key, object value, bool destructure = false)
    {
        // Serilog supports mapped context via ForContext, but for compatibility, return a no-op disposable.
        Log.Logger = Log.Logger.ForContext(key, value, destructure);
        return new NoOpDisposable();
    }

    private class SerilogLogger : ILog
    {
        private readonly ILogger _logger;

        public SerilogLogger(ILogger logger)
        {
            _logger = logger;
        }

        public bool Log(LogLevel logLevel, Func<string> messageFunc, Exception exception, params object[] formatParameters)
        {
            if (messageFunc == null) return true;
            var message = messageFunc();
            switch (logLevel)
            {
                case LogLevel.Trace:
                    _logger.Verbose(exception, message, formatParameters);
                    break;
                case LogLevel.Debug:
                    _logger.Debug(exception, message, formatParameters);
                    break;
                case LogLevel.Info:
                    _logger.Information(exception, message, formatParameters);
                    break;
                case LogLevel.Warn:
                    _logger.Warning(exception, message, formatParameters);
                    break;
                case LogLevel.Error:
                    _logger.Error(exception, message, formatParameters);
                    break;
                case LogLevel.Fatal:
                    _logger.Fatal(exception, message, formatParameters);
                    break;
                default:
                    _logger.Information(exception, message, formatParameters);
                    break;
            }
            return true;
        }
    }

    private class NoOpDisposable : IDisposable
    {
        public void Dispose() { }
    }
}