using FQL.Evaluator.Tables;

namespace FQL.Evaluator.Instructions
{
    public interface IVirtualMachine
    {
        long this[Register register] { get; set; }
        StackFrame Current { get; }
        Table Execute();
    }
}