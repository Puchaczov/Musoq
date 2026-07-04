using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Musoq.Evaluator.Build;

internal interface IInterpreterReferenceProvider
{
    IReadOnlyList<MetadataReference> GetReferences();
}
