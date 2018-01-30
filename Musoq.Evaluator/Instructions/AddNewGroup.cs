using System.Collections.Generic;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Instructions
{
    public class AddNewGroup : ByteCodeInstruction
    {
        private readonly IDictionary<int, string> _columnToValue;
        private readonly string _name;

        public AddNewGroup(string name, IDictionary<int, string> columnToValue)
        {
            _name = name;
            _columnToValue = columnToValue;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            var table = virtualMachine.Current.Tables[_name];

            table.Add(new GroupRow(virtualMachine.Current.CurrentGroup, _columnToValue));
            virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return $"ADD GROUP ROW {_name}";
        }
    }
}