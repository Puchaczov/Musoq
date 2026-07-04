using Microsoft.CodeAnalysis;

namespace Musoq.Evaluator.Runtime;

internal interface IRuntimeReferenceProvider
{
    MetadataReference[] References { get; }

    void CreateReferences();
}
