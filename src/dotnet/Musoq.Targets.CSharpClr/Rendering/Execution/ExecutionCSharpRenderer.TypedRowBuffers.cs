using System.Collections.Generic;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static IReadOnlyDictionary<string, GeneratedRowShape> CreateTypedRowBufferVariables(
        ExecutionBlock block,
        string? finalShapeTableName = null)
    {
        return ExecutionTypedRowBufferResolver.Resolve(block, finalShapeTableName);
    }
}
