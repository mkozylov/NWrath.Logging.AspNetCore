using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using NWrath.Logging;
using NWrath.Synergy.Common.Extensions;
using System.Runtime.CompilerServices;

namespace NWrath.Logging.AspNetCore
{
    public static class NWrathAspNetCoreLoggingExtensions
    {
        public static string DefaultRollingFolderPath { get; set; } = "Logs";

        public static IWebHostBuilder UseNWrathRollingFileLogging(
            this IWebHostBuilder hostBuilder,
            string folderPath = "Logs",
            LogLevel minLevel = LogLevel.Error
            )
        {
            var baseLogger = LoggingWizard.Spell.RollingFileLogger(folderPath, background: false);

            var emergencyLogger = default(ILogger);

            if (Environment.UserInteractive)
            {
                var mainLogger = baseLogger;

                var console = AppConsoleLogger();

                emergencyLogger = new LambdaLogger(
                    r => console.Log(r),
                    b => { /*ignore emergency batch rewrite, write only error*/ }
                    );
                
                baseLogger = new CompositeLogger(new[] { console, mainLogger });
            }
            
            baseLogger = LoggingWizard.Spell.BackgroundLogger(baseLogger, minLevel, emergencyLogger: emergencyLogger);

            return hostBuilder.UseNWrathLogging(baseLogger);
        }

        public static IWebHostBuilder UseNWrathDbLogging(
            this IWebHostBuilder hostBuilder,
            string connectionString,
            ILogger emergencyLogger,
            string tableName = "ServerLog",
            LogLevel minLevel = LogLevel.Error
            )
        {
            var schemaApply = new Action<DbLogSchemaConfig>(s =>
            {
                s.ConnectionString = connectionString;
                s.TableName = tableName;
            });

            return hostBuilder.UseNWrathDbLogging(schemaApply, emergencyLogger, minLevel);
        }

        public static IWebHostBuilder UseNWrathDbLogging(
            this IWebHostBuilder hostBuilder,
            string connectionString,
            string tableName = "ServerLog",
            string emergencyLoggerFolderPath = "Logs",
            LogLevel minLevel = LogLevel.Error
            )
        {
            var schemaApply = new Action<DbLogSchemaConfig>(s =>
            {
                s.ConnectionString = connectionString;
                s.TableName = tableName;
            });

            return hostBuilder.UseNWrathDbLogging(schemaApply, emergencyLoggerFolderPath, minLevel);
        }

        public static IWebHostBuilder UseNWrathDbLogging(
            this IWebHostBuilder hostBuilder,
            Action<DbLogSchemaConfig> dbSchemaApply,
            string emergencyLoggerFolderPath = "Logs",
            LogLevel minLevel = LogLevel.Error
            )
        {
            var emergencyLogger = LoggingWizard.Spell.RollingFileLogger(emergencyLoggerFolderPath, background: false);

            return hostBuilder.UseNWrathDbLogging(dbSchemaApply, emergencyLogger, minLevel);
        }

        public static IWebHostBuilder UseNWrathDbLogging(
            this IWebHostBuilder hostBuilder,
            Action<DbLogSchemaConfig> dbSchemaApply,
            ILogger emergencyLogger,
            LogLevel minLevel = LogLevel.Error
            )
        {
            var baseLogger = LoggingWizard.Spell.DbLogger(dbSchemaApply, background: false);

            if (Environment.UserInteractive 
                && emergencyLogger is ConsoleLogger == false 
                && emergencyLogger.CastAs<BackgroundLogger>()?.EmergencyLogger is ConsoleLogger == false
                )
            {
                var mainLogger = baseLogger;

                var console = AppConsoleLogger();

                baseLogger = new CompositeLogger(new[] { console, mainLogger });
            }

            baseLogger = LoggingWizard.Spell.BackgroundLogger(baseLogger, minLevel, emergencyLogger: emergencyLogger);

            return hostBuilder.UseNWrathLogging(baseLogger);
        }

        public static IWebHostBuilder UseNWrathLogging(this IWebHostBuilder hostBuilder, string sectionPath, string appsettingsPath = "appsettings.json")
        {
            var baseLogger = LoggingWizard.Spell.LoadFromJson(appsettingsPath, sectionPath, background: false);

            if (baseLogger is BackgroundLogger == false)
            {
                var emergencyLogger = AppRollingFileLogger();

                baseLogger = LoggingWizard.Spell.BackgroundLogger(baseLogger, baseLogger.RecordVerifier, emergencyLogger: emergencyLogger);
            }

            return hostBuilder.UseNWrathLogging(baseLogger);
        }

        public static IWebHostBuilder UseNWrathLogging(this IWebHostBuilder hostBuilder, Func<ILoggingWizardCharms, ILogger[]> loggersFactory, Action<ILogger, IConfiguration, ILoggingBuilder> configure = null, LogLevel minLevel = LogLevel.Error)
        {
            var baseLogger = LoggingWizard.Spell.CompositeLogger(minLevel, background: true, loggersFactory(LoggingWizard.Spell));

            return hostBuilder.UseNWrathLogging(baseLogger, configure);
        }

        public static IWebHostBuilder UseNWrathLogging(this IWebHostBuilder hostBuilder, Func<ILoggingWizardCharms, ILogger> loggerFactory, Action<ILogger, IConfiguration, ILoggingBuilder> configure = null)
        {
            var baseLogger = loggerFactory(LoggingWizard.Spell);

            return hostBuilder.UseNWrathLogging(baseLogger, configure);
        }

        public static IWebHostBuilder UseNWrathLogging(this IWebHostBuilder hostBuilder, ILogger logger, Action<ILogger, IConfiguration, ILoggingBuilder> configure = null)
        {
            if (configure == null)
            {
                configure = (lg, cfg, ctx) =>
                {
                    ctx.ClearProviders();
                    ctx.AddNWrathProvider(lg);
                };
            }

            return hostBuilder.ConfigureServices((context, collection) =>
            {
                Logger.Instance = logger;
                collection.AddSingleton(logger);
                collection.AddLogging(builder => configure(logger, context.Configuration, builder));
            });
        }

        public static ILoggingBuilder AddNWrathProvider(this ILoggingBuilder builder, ILogger baseLogger)
        {
            return builder.AddProvider(new NWrathLoggerProvider(baseLogger));
        }

        public static ILogger SysConsoleLogger(this ILoggingWizardCharms charms, LogLevel minLevel = LogLevel.Error, Action<ConsoleLogSerializer> serializerApply = null)
        {
            var logger = LoggingWizard.Spell.ConsoleLogger(s =>
            {
                s.OutputTemplate = "[{Level}] {Message}{ExNewLine}{Exception}";
                serializerApply?.Invoke(s);
            });

            logger.IsEnabled = Environment.UserInteractive;

            return logger;
        }

        public static ILogger SysConsoleLogger(this ILoggingWizardCharms charms, IStringLogSerializer serializer, LogLevel minLevel = LogLevel.Error)
        {
            var logger = LoggingWizard.Spell.ConsoleLogger(serializer);

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

        private static ILogger AppRollingFileLogger()
        {
            return LoggingWizard.Spell.RollingFileLogger(DefaultRollingFolderPath, background: false);
        }

        private static ILogger AppConsoleLogger()
        {
            return LoggingWizard.Spell.ConsoleLogger(s =>
            {
                s.OutputTemplate = "[{Level}] {Message}{ExNewLine}{Exception}";
            }, background: false);
        }
    }
}