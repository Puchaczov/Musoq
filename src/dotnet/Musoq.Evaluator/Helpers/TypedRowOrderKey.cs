using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Helpers;

public readonly record struct TypedRowOrderKey<TRow>(
    Func<TRow, object?> Selector,
    bool Descending,
    int NullOrdering)
    where TRow : Row;
