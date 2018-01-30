using System.Collections.Generic;
using FQL.Evaluator.Tables;

namespace FQL.Evaluator.Instructions
{
    public abstract class UseTableAsSource : ByteCodeInstruction
    {
        private readonly string _name;

        protected UseTableAsSource(string name)
        {
            _name = name;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            var table = virtualMachine.Current.Tables[_name];
            var tableSource = CreateSource(table);
            virtualMachine.Current.SourceStack.Push(tableSource.Rows.GetEnumerator());
            virtualMachine[Register.Ip] += 1;
        }

        protected abstract TableRowSource CreateSource(Table table);

        public override string DebugInfo()
        {
            return $"USE TABLE {_name} AS SOURCE";
        }
    }

    public class UseTableWithStandardColumns : UseTableAsSource
    {
        public UseTableWithStandardColumns(string name) 
            : base(name)
        { }

        protected override TableRowSource CreateSource(Table table) => new TableRowSource(table);
    }

    public class UseTableWithRemappedColumns : UseTableAsSource
    {
        private readonly IDictionary<string, int> _remappedColumns;
        public UseTableWithRemappedColumns(string name, IDictionary<string, int> remappedColumns) 
            : base(name)
        {
            _remappedColumns = remappedColumns;
        }

        protected override TableRowSource CreateSource(Table table) => new TableRowSource(table, _remappedColumns);
    }
}