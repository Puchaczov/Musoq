using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionFieldRead(
    string? Alias,
    string FieldName,
    ExecutionTypeRef ReturnType,
    FieldAccessStrategy? AccessStrategy = null,
    string? GeneratedTypeName = null) : ExecutionExpression(ReturnType)
{
    public ColumnStability Stability { get; init; } = ColumnStability.Stable;

    public ExecutionTypeRef? SourceReadType { get; init; }

    public EnumTypeDescriptor? EnumType { get; init; }

    internal ExecutionFieldRead(
        string? alias,
        string fieldName,
        Type returnType,
        FieldAccessStrategy? accessStrategy = null)
        : this(alias, fieldName, ExecutionClrBindingFactory.FromClr(returnType), accessStrategy)
    {
    }
}
