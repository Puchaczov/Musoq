namespace Musoq.Service.Client.Core
{
    public enum ExecutionStatus
    {
        Unknown,
        WaitingToStart,
        Compiling,
        Compiled,
        Running,
        Success,
        Failure
    }
}