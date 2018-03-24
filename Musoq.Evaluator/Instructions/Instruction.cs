using System.Diagnostics;

namespace Musoq.Evaluator.Instructions
{
    [DebuggerDisplay("{DebugInfo()}")]
    public abstract class Instruction
    {
        public abstract void Execute(IVirtualMachine vm);

        public abstract string DebugInfo();
    }
}