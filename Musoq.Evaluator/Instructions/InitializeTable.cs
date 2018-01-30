using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Instructions
{
    internal class InitializeTable<T> : ByteCodeInstruction
    {
        private readonly IDictionary<string, Column> _columns;
        private readonly string _name;

        public InitializeTable(Dictionary<string, Column> columns, string name)
        {
            _columns = columns;
            _name = name;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            virtualMachine.Current.Tables.Add(_name, new Table(_name, _columns.Select(f => f.Value).ToArray()));
            virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return $"INITIALIZE {_name}";
        }
    }
}