using System.Linq;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Instructions
{
    public class LoadTable : Instruction
    {
        private readonly Column[] _columns;
        private readonly string[] _keys;
        private readonly string _name;

        public LoadTable(string name, string[] keys, Column[] columns)
        {
            _name = name;
            _keys = keys;
            _columns = columns;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            var table = new Table(_name, _columns);
            virtualMachine.Current.Tables.Add(_name, table);
            table.AddIndex(_keys.Select(key => new TableIndex(key)).ToArray());

            virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return $"LD TABLE {_name}";
        }
    }
}