using System.Collections.Generic;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Runtime;

public interface ITableRowBatchSource<TRow> : IQueryRows<TRow>
    where TRow : Row
{
    void AddTo(Table table);
}
