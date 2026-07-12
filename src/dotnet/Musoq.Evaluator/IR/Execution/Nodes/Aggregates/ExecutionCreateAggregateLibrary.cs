using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCreateAggregateLibrary(
    ExecutionVariable Library,
    ExecutionTypeRef LibraryType) : ExecutionNode
{
    internal ExecutionCreateAggregateLibrary(ExecutionVariable library, Type libraryType)
        : this(library, ExecutionTypeRef.FromClr(libraryType))
    {
    }
}
