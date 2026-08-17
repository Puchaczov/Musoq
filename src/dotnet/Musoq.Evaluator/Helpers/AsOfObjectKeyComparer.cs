using System.Collections.Generic;

namespace Musoq.Evaluator.Helpers;

internal sealed class AsOfObjectKeyComparer : IComparer<object>
{
    public static readonly AsOfObjectKeyComparer Instance = new();

    public int Compare(object? left, object? right)
    {
        return EvaluationHelper.CompareAsOfValues(left!, right!);
    }
}
