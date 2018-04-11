using System;
using NLog;

namespace Musoq.Service.Logging
{
    public class ServiceLogger : IServiceLogger
    {
        private readonly ILogger _logger;

        private static ServiceLogger _instance;

        private ServiceLogger()
        {
            _logger = LogManager.GetCurrentClassLogger();
        }

        public void Log(string message)
        {
            _logger.Log(LogLevel.Info, message);
        }

        public void Log(Exception exc)
        {
            _logger.Log(LogLevel.Error, exc);
        }

        public static IServiceLogger Instance => _instance ?? (_instance = new ServiceLogger());
    }
}
