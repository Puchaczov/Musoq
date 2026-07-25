using Musoq.Evaluator.Helpers;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static ExecutionVariable CreateIndexedHashRowVariable(
        string name,
        ExecutionVariable row)
    {
        return new ExecutionVariable(
            name,
            CreateIndexedHashRowType(row),
            CreateIndexedHashRowTypeName(row));
    }

    private static Type CreateIndexedHashRowType(ExecutionVariable row)
    {
        return typeof(IndexedHashJoinRow<>).MakeGenericType(row.Type.ResolveClrType());
    }

    private static string? CreateIndexedHashRowTypeName(ExecutionVariable row)
    {
        return string.IsNullOrWhiteSpace(row.GeneratedRowTypeName)
            ? null
            : $"{nameof(IndexedHashJoinRow<int>)}<{row.GeneratedRowTypeName}>";
    }
}
