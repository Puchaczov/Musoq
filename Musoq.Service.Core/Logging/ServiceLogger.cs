using System;
using NLog;

namespace Musoq.Service.Core.Logging
{
    public class ServiceLogger : IServiceLogger
    {
        private static ServiceLogger _instance;
        private readonly ILogger _logger;

        private ServiceLogger()
        {
            _logger = LogManager.GetCurrentClassLogger();
        }

        public static IServiceLogger Instance => _instance ?? (_instance = new ServiceLogger());

        public void Log(string message)
        {
            _logger.Log(LogLevel.Info, message);
        }

        public void Log(Exception exc)
        {
            _logger.Log(LogLevel.Error, exc);
        }
    }
}