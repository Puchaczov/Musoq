using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Helpers;

public sealed record RowOrderKey(
    Func<Row, object> Selector,
    bool Descending,
    int NullOrdering = 0);
