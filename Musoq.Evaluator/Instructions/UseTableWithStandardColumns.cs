using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Instructions
{
    public class UseTableWithStandardColumns : UseTableAsSource
    {
        public UseTableWithStandardColumns(string name) 
            : base(name)
        { }

        protected override TableRowSource CreateSource(Table table) => new TableRowSource(table);
    }
}