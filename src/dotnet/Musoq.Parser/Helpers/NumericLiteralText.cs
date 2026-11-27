using System.Globalization;

namespace Musoq.Parser.Helpers;

public static class NumericLiteralText
{
    /// <summary>Returns SQL text that preserves a numeric literal's type when it is used in a larger expression.</summary>
    public static string? Format(object? value) =>
        value switch
        {
            int number => number.ToString(CultureInfo.InvariantCulture),
            sbyte number => number.ToString(CultureInfo.InvariantCulture) + "b",
            byte number => number.ToString(CultureInfo.InvariantCulture) + "ub",
            short number => number.ToString(CultureInfo.InvariantCulture) + "s",
            ushort number => number.ToString(CultureInfo.InvariantCulture) + "us",
            uint number => number.ToString(CultureInfo.InvariantCulture) + "ui",
            long number => number.ToString(CultureInfo.InvariantCulture) + "l",
            ulong number => number.ToString(CultureInfo.InvariantCulture) + "ul",
            decimal number => FormatDecimal(number),
            _ => null,
        };

    private static string FormatDecimal(decimal number)
    {
        var text = number.ToString(CultureInfo.InvariantCulture);
        return text.Contains('.') ? text : text + "d";
    }
}
