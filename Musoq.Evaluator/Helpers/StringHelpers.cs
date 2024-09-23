namespace Musoq.Evaluator.Helpers;

public static class StringHelpers
{
    private static readonly object NamespaceIdentifierGuard = new();
    private static long _namespaceUniqueId;

    public static long GenerateNamespaceIdentifier()
    {
        long value;

        lock (NamespaceIdentifierGuard)
        {
            value = _namespaceUniqueId++;
        }

        return value;
    }
}