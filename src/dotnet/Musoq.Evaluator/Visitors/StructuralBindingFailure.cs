namespace Musoq.Evaluator.Visitors;

internal static class StructuralBindingFailure
{
    public static Exception Create(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new NotSupportedException(message);
    }
}