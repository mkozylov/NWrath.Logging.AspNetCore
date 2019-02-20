using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace NWrath.Logging.AspNetCore
{
    public class NWrathLoggerProvider : ILoggerProvider
    {
        private NWrathLogger _logger;

        public NWrathLoggerProvider(ILogger baseLogger)
        {
            _logger = new NWrathLogger(baseLogger);
        }

        public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName)
        {
            return _logger;
        }

        public void Dispose()
        {
            _logger.Dispose();
        }
    }
}