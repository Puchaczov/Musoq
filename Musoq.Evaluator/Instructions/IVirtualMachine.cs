namespace Musoq.Evaluator.Instructions
{
    public interface IVirtualMachine : IRunnable
    {
        long this[Register register] { get; set; }
        StackFrame Current { get; }
    }
}