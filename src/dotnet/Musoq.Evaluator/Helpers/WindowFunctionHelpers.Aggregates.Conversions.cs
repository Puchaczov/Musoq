namespace Musoq.Evaluator.Helpers;

public static partial class WindowFunctionHelpers
{
    private static decimal ToDecimalFast(object value)
    {
        return value switch
        {
            int i => i,
            long l => l,
            decimal d => d,
            double dbl => (decimal)dbl,
            float f => (decimal)f,
            short s => s,
            byte b => b,
            _ => Convert.ToDecimal(value, System.Globalization.CultureInfo.InvariantCulture)
        };
    }
}
