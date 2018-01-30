using System.Collections.Generic;
using Musoq.Evaluator.Instructions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator
{
    public class VirtualMachine : IVirtualMachine
    {
        private readonly Stack<StackFrame> _frames;
        private readonly ByteCodeInstruction[] _instructions;

        public VirtualMachine(ByteCodeInstruction[] byteCodeInstruction)
        {
            _instructions = byteCodeInstruction;
            _frames = new Stack<StackFrame>();
            _frames.Push(new StackFrame());
            Current = _frames.Peek();
        }

        public Table Execute()
        {
            while (this[Register.Sop] != (int) SpecialOperationRegister.Exit)
            {
                var ip = this[Register.Ip];
                var instruction = _instructions[ip];
                instruction.Execute(this);
            }

            return Current.Tables[Current.StringsStack.Pop()];
        }

        public StackFrame Current { get; private set; }

        public long this[Register register]
        {
            get => Current.Registers[(int) register];
            set => Current.Registers[(int) register] = value;
        }

        public void PushStackFrame()
        {
            _frames.Push(new StackFrame());
            Current = _frames.Peek();
        }

        public void PopStackFram()
        {
            _frames.Pop();
            Current = _frames.Peek();
        }
    }
}