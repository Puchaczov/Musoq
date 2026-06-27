using System.Collections.Generic;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Runtime;

public interface ITableMaterializationSource
{
    bool TryMaterializeTable(string name, IReadOnlyList<Column> columns, out Table table);
}
