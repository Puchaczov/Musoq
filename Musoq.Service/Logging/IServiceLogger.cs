using System;

namespace Musoq.Service.Logging
{
    public interface IServiceLogger
    {
        void Log(string message);
        void Log(Exception exc);
    }
}