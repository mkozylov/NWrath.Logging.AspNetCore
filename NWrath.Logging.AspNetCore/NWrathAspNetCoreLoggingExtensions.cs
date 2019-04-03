using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace NWrath.Logging.AspNetCore
{
    public static class NWrathAspNetCoreLoggingExtensions
    {
        public static IWebHostBuilder UseNWrathRollingFileLogging(
            this IWebHostBuilder hostBuilder,
            string folderPath = "Logs",
            LogLevel minLevel = LogLevel.Error
            )
        {
            var baseLogger = DefaultLoggerSet(
                LoggingWizard.Spell.RollingFileLogger(folderPath, minLevel: minLevel)
                );

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
            var emergencyLogger = LoggingWizard.Spell.RollingFileLogger(emergencyLoggerFolderPath);

            return hostBuilder.UseNWrathDbLogging(dbSchemaApply, emergencyLogger, minLevel);
        }

        public static IWebHostBuilder UseNWrathDbLogging(
            this IWebHostBuilder hostBuilder,
            Action<DbLogSchemaConfig> dbSchemaApply,
            ILogger emergencyLogger,
            LogLevel minLevel = LogLevel.Error
            )
        {
            var baseLogger = DefaultLoggerSet(
                LoggingWizard.Spell.DbLogger(minLevel, dbSchemaApply),
                emergencyLogger
                );

            return hostBuilder.UseNWrathLogging(baseLogger);
        }

        public static IWebHostBuilder UseNWrathLogging(this IWebHostBuilder hostBuilder, string sectionPath, string appsettingsPath = "appsettings.json")
        {
            var logger = LoggingWizard.Spell.LoadFromJson(appsettingsPath, sectionPath);

            return hostBuilder.UseNWrathLogging(logger);
        }

        public static IWebHostBuilder UseNWrathLogging(this IWebHostBuilder hostBuilder, Func<ILoggingWizardCharms, ILogger[]> loggersFactory, Action<ILogger, IConfiguration, ILoggingBuilder> configure = null, LogLevel minLevel = LogLevel.Error)
        {
            var baseLogger = LoggingWizard.Spell.CompositeLogger(minLevel, loggersFactory(LoggingWizard.Spell));

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

        public static ConsoleLogger SysConsoleLogger(this ILoggingWizardCharms charms, LogLevel minLevel = LogLevel.Error, Action<ConsoleLogSerializer> serializerApply = null)
        {
            return LoggingWizard.Spell.ConsoleLogger(s =>
            {
                s.OutputTemplate = "[{Level}] {Message}{ExNewLine}{Exception}";
                serializerApply?.Invoke(s);
            });
        }

        public static ConsoleLogger SysConsoleLogger(this ILoggingWizardCharms charms, IStringLogSerializer serializer, LogLevel minLevel = LogLevel.Error)
        {
            return LoggingWizard.Spell.ConsoleLogger(serializer);
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

        private static ILogger DefaultLoggerSet(ILogger baseLogger, ILogger emergencyLogger = null)
        {
            var targetLogger = baseLogger;

            if (Environment.UserInteractive)
            {
                var console = LoggingWizard.Spell.ConsoleLogger(s =>
                {
                    s.OutputTemplate = "[{Level}] {Message}{ExNewLine}{Exception}";
                });

                targetLogger = new LambdaLogger(batch => {
                    console.Log(batch);
                    baseLogger.Log(batch);
                });
            }

            return LoggingWizard.Spell.BackgroundLogger(targetLogger, emergencyLogger: emergencyLogger);
        }
    }
}