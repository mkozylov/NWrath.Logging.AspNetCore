using System;
using Microsoft.Extensions.Logging;

namespace NWrath.Logging.AspNetCore
{
    public class NWrathLogger : LoggerBase, Microsoft.Extensions.Logging.ILogger
    {
        private ILogger _baseLogger;

        public NWrathLogger(ILogger baseLogger)
        {
            _baseLogger = baseLogger;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public void Log<TState>(
            Microsoft.Extensions.Logging.LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter
            )
        {
            Log(message: formatter(state, null),
                level: logLevel.ToNWrathLevel(),
                exception: exception,
                extra: new { EventId = eventId }
                );
        }

        public override void Dispose()
        {
            base.Dispose();

            _baseLogger.Dispose();
        }

        bool Microsoft.Extensions.Logging.ILogger.IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            return IsEnabled;
        }

        protected override void WriteRecord(LogRecord record)
        {
            _baseLogger.Log(record);
        }
    }
}