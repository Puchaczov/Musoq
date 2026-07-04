using Microsoft.CodeAnalysis;

namespace Musoq.Evaluator.Runtime;

public static class RuntimeLibraries
{
    internal static IRuntimeReferenceProvider Default { get; } =
        new RuntimeReferenceProvider(MetadataReferenceCache.Default);

    public static MetadataReference[] References => Default.References;

    public static void CreateReferences()
    {
        Default.CreateReferences();
    }
}
