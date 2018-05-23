using Musoq.Schema;

namespace Musoq.Evaluator.Utils.Symbols
{
    public class ColumnSymbol : Symbol
    {
        public ColumnSymbol(ISchemaColumn column)
        {
            Column = column;
        }

        public ISchemaColumn Column { get; }
    }
}