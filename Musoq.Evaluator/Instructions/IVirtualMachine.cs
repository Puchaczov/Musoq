using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Instructions
{
    public interface IVirtualMachine
    {
        long this[Register register] { get; set; }
        StackFrame Current { get; }
        Table Execute();
    }
}