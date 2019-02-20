using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

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
            return hostBuilder.UseNWrathLogging(
                LoggingWizard.Spell.RollingFileLogger(folderPath, minLevel: minLevel)
                );
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
            var baseLogger = LoggingWizard.Spell.BackgroundLogger(
                b => b.DbLogger(minLevel, dbSchemaApply),
                emergencyLogger: emergencyLogger,
                minLevel: minLevel
                );

            return hostBuilder.UseNWrathLogging(baseLogger);
        }

        public static IWebHostBuilder UseNWrathLogging(this IWebHostBuilder hostBuilder, string sectionPath, string appsettingsPath = "appsettings.json")
        {
            var logger = LoggingWizard.Spell.LoadFromJson(appsettingsPath, sectionPath);

            return hostBuilder.UseNWrathLogging(logger);
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
                    ctx.AddDebug();
                    ctx.AddConsole();
                    ctx.AddNWrathProvider(lg);
                };
            }

            return hostBuilder.ConfigureServices((context, collection) =>
            {
                collection.AddSingleton(logger);
                collection.AddLogging(builder => configure(logger, context.Configuration, builder));
            });
        }

        public static ILoggingBuilder AddNWrathProvider(this ILoggingBuilder builder, ILogger baseLogger)
        {
            return builder.AddProvider(new NWrathLoggerProvider(baseLogger));
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