using System;
using System.Collections.Generic;
using System.Diagnostics;
using Musoq.Evaluator.Instructions;
using Musoq.Evaluator.Tables;
using Musoq.Plugins;

namespace Musoq.Evaluator
{
    public class VirtualMachine : IVirtualMachine
    {
        private readonly ByteCodeInstruction[] _instructions;

        public VirtualMachine(ByteCodeInstruction[] byteCodeInstruction)
        {
            _instructions = byteCodeInstruction;
            var frames = new Stack<StackFrame>();
            frames.Push(new StackFrame());
            Current = frames.Peek();
        }

        public Table Execute()
        {
            // ReSharper disable once TooWideLocalVariableScope
            long ip;

            try
            {
                while (this[Register.Sop] != (int) SpecialOperationRegister.Exit)
                {
                    ip = this[Register.Ip];
                    var instruction = _instructions[ip];
                    instruction.Execute(this);
                }

                var table = Current.Tables[Current.StringsStack.Pop()];

                Clear();

                return table;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                if(Debugger.IsAttached)
                    Debugger.Break();
            }

            Clear();

            return null;
        }

        public StackFrame Current { get; }

        public long this[Register register]
        {
            get => Current.Registers[(int) register];
            set => Current.Registers[(int) register] = value;
        }

        private void Clear()
        {
            Current.SourceStack.Clear();
            Current.Groups.Clear();

            Current.CurrentGroup = new Group(null, new string[0], new object[0]);
            Current.Groups.Add("root", Current.CurrentGroup);

            Current.LongsStack.Clear();
            Current.NumericsStack.Clear();
            Current.ObjectsStack.Clear();
            Current.Stats = new AmendableQueryStats();
            Current.StringsStack.Clear();
            Current.Tables.Clear();

            this[Register.Ip] = 0;
            this[Register.Sop] = 0;
        }
    }
}