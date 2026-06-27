namespace Musoq.Evaluator.Visitors;

internal sealed record ScriptValueConversionResult(bool Success, object? Value, string Error)
{
    public static ScriptValueConversionResult Converted(object? value)
    {
        return new ScriptValueConversionResult(true, value, string.Empty);
    }

    public static ScriptValueConversionResult Failed(string error)
    {
        return new ScriptValueConversionResult(false, null, error);
    }
}