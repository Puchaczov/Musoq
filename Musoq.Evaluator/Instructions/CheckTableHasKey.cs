using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Instructions
{
    public class CheckTableHasKey : Instruction
    {
        private readonly int[] _indexedColumns;
        private readonly string _sourceName;
        private readonly bool _state;

        public CheckTableHasKey(string tableName, int[] columns, bool state)
        {
            _sourceName = tableName;
            _indexedColumns = columns;
            _state = state;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            var table = virtualMachine.Current.Tables[_sourceName];
            var values = (object[]) virtualMachine.Current.ObjectsStack.Peek();
            var filteredValues = new object[_indexedColumns.Length];

            for (var i = 0; i < filteredValues.Length; i++)
                filteredValues[i] = values[_indexedColumns[i]];

            var key = new Key(filteredValues, _indexedColumns);
            var hasKey = table.ContainsKey(key);

            virtualMachine.Current.BooleanStack.Push(hasKey == _state);

            virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            var stateText = _state ? string.Empty : "NOT";
            return $"CHK_TABLE {_sourceName} HAS {stateText} KEY";
        }
    }
}