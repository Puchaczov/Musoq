using System.Diagnostics;

namespace Musoq.Evaluator.Instructions
{
    [DebuggerDisplay("{DebugInfo()}")]
    public abstract class ByteCodeInstruction
    {
        public abstract void Execute(IVirtualMachine virtualMachine);

        public abstract string DebugInfo();
    }
}