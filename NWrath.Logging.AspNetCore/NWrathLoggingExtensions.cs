using System;
using System.Collections.Generic;
using System.Text;

namespace NWrath.Logging.AspNetCore
{
    public static class NWrathLoggingExtensions
    {
        public static string DefaultConsoleOutputTemplate { get; set; } = "[{Level}] {Message}{exnl}{Exception}";

        public static ILogger SysConsoleLogger(
            this LoggingWizardCharms charms,
            LogLevel minLevel = LogLevel.Info,
            Action<ConsoleLogSerializerBuilder> serializerApply = null
            )
        {
            var logger = LoggingWizard.Spell.ConsoleLogger(minLevel, s =>
            {
                s.OutputTemplate = DefaultConsoleOutputTemplate;
                serializerApply?.Invoke(s);
            });

            logger.IsEnabled = Environment.UserInteractive;

            return logger;
        }

        public static ILogger SysConsoleLogger(
            this LoggingWizardCharms charms,
            IStringLogSerializer serializer,
            LogLevel minLevel = LogLevel.Info
            )
        {
            var logger = LoggingWizard.Spell.ConsoleLogger(minLevel, serializer);

            logger.IsEnabled = Environment.UserInteractive;

            return logger;
        }

        public static LogLevel ToNWrathLevel(this Microsoft.Extensions.Logging.LogLevel aspLogLevel)
        {
            switch (aspLogLevel)
            {
                case Microsoft.Extensions.Logging.LogLevel.Trace:
                case Microsoft.Extensions.Logging.LogLevel.Debug:
                    return LogLevel.Debug;

                case Microsoft.Extensions.Logging.LogLevel.Information:
                    return LogLevel.Info;

                case Microsoft.Extensions.Logging.LogLevel.Warning:
                    return LogLevel.Warning;

                case Microsoft.Extensions.Logging.LogLevel.Error:
                    return LogLevel.Error;

                case Microsoft.Extensions.Logging.LogLevel.Critical:
                case Microsoft.Extensions.Logging.LogLevel.None:
                default:
                    return LogLevel.Critical;
            }
        }
    }
}
