using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using NWrath.Logging;
using System.Runtime.CompilerServices;
using NWrath.Synergy.Common.Extensions;
using Microsoft.Extensions.Hosting;

namespace NWrath.Logging.AspNetCore
{
    public static class HostBuilderExtensions
    {
        public static THostBuilder UseNWrathRollingFileLogging<THostBuilder>(
            this THostBuilder hostBuilder,
            string folderPath = "Logs",
            LogLevel minLevel = LogLevel.Error,
            LogLevel consoleMinLevel = LogLevel.Info
            )
            #if NETCOREAPP3
                where THostBuilder : IHostBuilder
            #else
                where THostBuilder : Microsoft.AspNetCore.Hosting.IWebHostBuilder
            #endif
        {
            ILogger baseLogger = LoggingWizard.Spell.RollingFileLogger(folderPath, minLevel);
            
            var emergencyLogger = default(ILogger);

            if (Environment.UserInteractive)
            {
                var mainLogger = baseLogger;

                var console = LoggingWizard.Spell.SysConsoleLogger(consoleMinLevel);

                emergencyLogger = LoggingWizard.Spell.LambdaLogger(
                                      r => console.Log(r),
                                      b => { /*ignore emergency batch rewrite, write only error*/ },
                                      minLevel
                                      );

                baseLogger = new CompositeLogger(new[] { console, mainLogger });
            }
            
            baseLogger = LoggingWizard.Spell.BackgroundLogger(baseLogger, emergencyLogger: emergencyLogger);

            return hostBuilder.UseNWrathLogging(baseLogger);
        }

        public static THostBuilder UseNWrathSqlLogging<THostBuilder>(
            this THostBuilder hostBuilder,
            string connectionString,
            ILogger emergencyLogger,
            string tableName = "ServerLog",
            LogLevel minLevel = LogLevel.Error,
            LogLevel consoleMinLevel = LogLevel.Info
            )
            #if NETCOREAPP3
                where THostBuilder : IHostBuilder
            #else
                where THostBuilder : Microsoft.AspNetCore.Hosting.IWebHostBuilder
            #endif
        {
            var schemaApply = new Action<SqlLogSchemaConfig>(s =>
            {
                s.ConnectionString = connectionString;
                s.TableName = tableName;
            });

            return hostBuilder.UseNWrathSqlLogging(schemaApply, emergencyLogger, minLevel, consoleMinLevel);
        }

        public static THostBuilder UseNWrathSqlLogging<THostBuilder>(
            this THostBuilder hostBuilder,
            string connectionString,
            string tableName = "ServerLog",
            string emergencyLoggerFolderPath = "Logs",
            LogLevel minLevel = LogLevel.Error,
            LogLevel consoleMinLevel = LogLevel.Info
            )
            #if NETCOREAPP3
                where THostBuilder : IHostBuilder
            #else
                where THostBuilder : Microsoft.AspNetCore.Hosting.IWebHostBuilder
            #endif
        {
            var schemaApply = new Action<SqlLogSchemaConfig>(s =>
            {
                s.ConnectionString = connectionString;
                s.TableName = tableName;
            });

            return hostBuilder.UseNWrathSqlLogging(schemaApply, emergencyLoggerFolderPath, minLevel, consoleMinLevel);
        }

        public static THostBuilder UseNWrathSqlLogging<THostBuilder>(
            this THostBuilder hostBuilder,
            Action<SqlLogSchemaConfig> dbSchemaApply,
            string emergencyLoggerFolderPath = "Logs",
            LogLevel minLevel = LogLevel.Error,
            LogLevel consoleMinLevel = LogLevel.Info
            )
            #if NETCOREAPP3
                where THostBuilder : IHostBuilder
            #else
                where THostBuilder : Microsoft.AspNetCore.Hosting.IWebHostBuilder
            #endif
        {
            var emergencyLogger = LoggingWizard.Spell.RollingFileLogger(emergencyLoggerFolderPath, minLevel);

            return hostBuilder.UseNWrathSqlLogging(dbSchemaApply, emergencyLogger, minLevel, consoleMinLevel);
        }

        public static THostBuilder UseNWrathSqlLogging<THostBuilder>(
            this THostBuilder hostBuilder,
            Action<SqlLogSchemaConfig> dbSchemaApply,
            ILogger emergencyLogger,
            LogLevel minLevel = LogLevel.Error,
            LogLevel consoleMinLevel = LogLevel.Info
            )
            #if NETCOREAPP3
                where THostBuilder : IHostBuilder
            #else
                where THostBuilder : Microsoft.AspNetCore.Hosting.IWebHostBuilder
            #endif
        {
            ILogger baseLogger = LoggingWizard.Spell.SqlLogger(minLevel, dbSchemaApply);

            if (Environment.UserInteractive
                && emergencyLogger is ConsoleLogger == false
                && emergencyLogger.CastAs<BackgroundLogger>()?.EmergencyLogger is ConsoleLogger == false
                )
            {
                var mainLogger = baseLogger;

                var console = LoggingWizard.Spell.SysConsoleLogger(consoleMinLevel);

                baseLogger = new CompositeLogger(new[] { console, mainLogger });
            }

            baseLogger = LoggingWizard.Spell.BackgroundLogger(baseLogger, emergencyLogger: emergencyLogger);

            return hostBuilder.UseNWrathLogging(baseLogger);
        }

        public static THostBuilder UseNWrathLogging<THostBuilder>(
            this THostBuilder hostBuilder,
            Func<LoggingWizardCharms, ILogger[]> loggersFactory,
            Action<ILogger, IConfiguration, ILoggingBuilder> configure = null
            )
            #if NETCOREAPP3
                where THostBuilder : IHostBuilder
            #else
                where THostBuilder : Microsoft.AspNetCore.Hosting.IWebHostBuilder
            #endif
        {
            var baseLogger = LoggingWizard.Spell.BackgroundCompositeLogger(loggersFactory(LoggingWizard.Spell));

            return hostBuilder.UseNWrathLogging(baseLogger, configure);
        }

        public static THostBuilder UseNWrathLogging<THostBuilder>(
            this THostBuilder hostBuilder,
            Func<LoggingWizardCharms, ILogger> loggerFactory,
            Action<ILogger, IConfiguration, ILoggingBuilder> configure = null
            )
            #if NETCOREAPP3
                where THostBuilder : IHostBuilder
            #else
                where THostBuilder : Microsoft.AspNetCore.Hosting.IWebHostBuilder
            #endif
        {
            var baseLogger = loggerFactory(LoggingWizard.Spell);

            return hostBuilder.UseNWrathLogging(baseLogger, configure);
        }

        public static THostBuilder UseNWrathLogging<THostBuilder>(
            this THostBuilder hostBuilder,
            ILogger logger,
            Action<ILogger, IConfiguration, ILoggingBuilder> configure = null
            )
            #if NETCOREAPP3
                where THostBuilder : IHostBuilder
            #else
                where THostBuilder : Microsoft.AspNetCore.Hosting.IWebHostBuilder
            #endif
        {
            if (configure == null)
            {
                configure = (lg, cfg, ctx) =>
                {
                    ctx.ClearProviders();
                    ctx.AddNWrathProvider(lg);
                };
            }

            return (THostBuilder)hostBuilder.ConfigureServices((context, collection) =>
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
    }
}