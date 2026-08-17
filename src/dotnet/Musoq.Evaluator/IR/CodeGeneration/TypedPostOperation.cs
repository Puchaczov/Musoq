using System.Collections.Generic;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.IR.CodeGeneration;

internal abstract record TypedPostOperation
{
    public sealed record Distinct : TypedPostOperation
    {
        public static readonly Distinct Instance = new();
    }

    public sealed record Order(IReadOnlyList<ExecutionOrderField> Keys) : TypedPostOperation;

    public sealed record Skip(int Count) : TypedPostOperation;

    public sealed record Take(int Count) : TypedPostOperation;
}
