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
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        public void Log(string message)
        {
            Logger.Log(LogLevel.Info, message);
        }

        public void Log(Exception exc)
        {
            Logger.Log(LogLevel.Error, exc);
        }
    }
}
