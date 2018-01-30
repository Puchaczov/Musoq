using System.Collections.Generic;
using FQL.Evaluator.Tables;
using FQL.Parser.Nodes;

namespace FQL.Evaluator
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