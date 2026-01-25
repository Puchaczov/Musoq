using System.Threading;

namespace Musoq.Evaluator.Helpers;

public static class StringHelpers
{
    private static long _namespaceUniqueId;

    public static long GenerateNamespaceIdentifier()
    {
        return Interlocked.Increment(ref _namespaceUniqueId);
    }
}
