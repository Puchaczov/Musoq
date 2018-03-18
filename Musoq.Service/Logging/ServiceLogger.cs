using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public static IServiceLogger Instance
        {
            get
            {
                if(_instance == null)
                    _instance = new ServiceLogger();

                return _instance;
            }
        }
    }
}
