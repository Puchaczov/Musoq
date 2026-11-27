using System.Numerics;

namespace Musoq.Evaluator;

public partial class Operators
{
    public static bool SqlTextEquals(char? character, string? text) => text is { Length: 1 } && character == text[0];

    public static bool SqlTextEquals(string? text, char? character) => SqlTextEquals(character, text);

    public static bool SqlDecimalEquals<TFloating>(decimal? number, TFloating? floating)
        where TFloating : struct, IFloatingPointIeee754<TFloating>
    {
        if (!number.HasValue || !floating.HasValue)
            return number.HasValue == floating.HasValue;

        return ToComparisonDecimal(floating) == number;
    }

    public static bool SqlDecimalEquals<TFloating>(TFloating? floating, decimal? number)
        where TFloating : struct, IFloatingPointIeee754<TFloating> => SqlDecimalEquals(number, floating);

    public static decimal? ToComparisonDecimal<TFloating>(TFloating? floating)
        where TFloating : struct, IFloatingPointIeee754<TFloating>
    {
        var value = floating.GetValueOrDefault();
        // Decimal.MaxValue rounds up to 2^96 in both supported floating formats.
        var limit = TFloating.CreateChecked(decimal.MaxValue);
        return floating.HasValue && value > -limit && value < limit ? decimal.CreateChecked(value) : null;
    }

    /// <summary>Applies SQL null admission to a planner-selected constant membership lookup.</summary>
    public static bool? SqlIn<T>(T value, Func<T, bool> contains, bool hasNull)
    {
        ArgumentNullException.ThrowIfNull(contains);
        if (value is null)
            return null;
        if (contains(value))
            return true;
        return hasNull ? null : false;
    }
}
