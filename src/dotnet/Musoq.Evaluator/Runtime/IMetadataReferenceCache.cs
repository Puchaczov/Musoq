using Microsoft.CodeAnalysis;

namespace Musoq.Evaluator.Runtime;

internal interface IMetadataReferenceCache
{
    int Count { get; }

    MetadataReference GetOrCreate(string assemblyPath);

    void Clear();
}
