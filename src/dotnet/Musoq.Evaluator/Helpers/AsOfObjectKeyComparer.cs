using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.Helpers;

internal sealed class AsOfObjectKeyComparer : IComparer<object>
{
    public static readonly AsOfObjectKeyComparer Instance = new();

    public int Compare(object? left, object? right)
    {
        return EvaluationHelper.CompareAsOfValues(left!, right!);
    }
}
