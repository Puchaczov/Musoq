using System.Collections.Generic;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator
{
    public class TableMetadata
    {
        public TableMetadata(TableMetadata parent)
        {
            Parent = parent;
        }

        public ICollection<Column> Columns { get; } = new List<Column>();

        public ICollection<string> Indexes { get; } = new List<string>();

        public TableMetadata Parent { get; }
    }
}