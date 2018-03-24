using System;
using System.Collections.Generic;
using System.Text;

namespace Musoq.Evaluator.Instructions
{
    public class RenameTable : Instruction
    {
        private readonly string _tableSourceName;
        private readonly string _tableDestinationName;

        public RenameTable(string tableSourceName, string tableDestinationName)
        {
            _tableSourceName = tableSourceName;
            _tableDestinationName = tableDestinationName;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            virtualMachine.Current.Tables.Add(_tableDestinationName, virtualMachine.Current.Tables[_tableSourceName]);
            virtualMachine.Current.Tables.Remove(_tableSourceName);
            virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return $"RENAME {_tableSourceName} AS {_tableDestinationName}";
        }
    }
}
