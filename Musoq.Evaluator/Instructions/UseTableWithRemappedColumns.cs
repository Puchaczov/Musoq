using System.Collections.Generic;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Instructions
{
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