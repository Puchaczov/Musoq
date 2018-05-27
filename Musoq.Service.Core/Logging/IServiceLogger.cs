using System;

namespace Musoq.Service.Core.Logging
{
    public interface IServiceLogger
    {
        void Log(string message);
        void Log(Exception exc);
    }
}