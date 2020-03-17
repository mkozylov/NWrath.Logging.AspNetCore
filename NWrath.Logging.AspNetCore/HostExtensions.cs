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
    public static class HostExtensions
    {
        public static void TryRun<THost>(
            this THost host,
            string crashLog = "crash.log"
            )
            #if NETCOREAPP3
                where THost : IHost
            #else
                where THost : Microsoft.AspNetCore.Hosting.IWebHost
            #endif
        {
            try
            {
                #if NETCOREAPP3
                    host.Run();
                #else
                    Microsoft.AspNetCore.Hosting.WebHostExtensions.Run(host);
                #endif
            }
            catch (Exception ex)
            {
                Console.Clear();

                LoggingWizard.Spell.CompositeLogger(
                    f => f.ConsoleLogger(),
                    f => f.FileLogger(crashLog)
                    )
                    .Critical("Application startup exception", ex);

                throw;
            }
        }
    }
}