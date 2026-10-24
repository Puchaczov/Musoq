using System.Numerics;
using Musoq.Evaluator.Exceptions;

namespace Musoq.Evaluator.Helpers;

/// <summary>Validates typed paging values once, before query sources are opened.</summary>
public static class PagingCountValidator
{
    public static int Validate<T>(T value, string clause, string name) where T : struct, IBinaryInteger<T>
    {
        if (T.IsNegative(value) || value > T.CreateSaturating(int.MaxValue))
            throw new PagingCountException(clause, name);
        return int.CreateChecked(value);
    }

    public static int ValidateNullable<T>(T? value, string clause, string name) where T : struct, IBinaryInteger<T>
    {
        if (!value.HasValue)
            throw new PagingCountException(clause, name);
        return Validate(value.Value, clause, name);
    }
}
