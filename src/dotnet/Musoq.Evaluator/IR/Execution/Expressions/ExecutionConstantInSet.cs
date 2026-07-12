using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionConstantInSet
{
    private readonly IReadOnlyList<ExecutionConstantValue> _values;

    public ExecutionConstantInSet(
        ExecutionTypeRef elementType,
        IEnumerable<ExecutionConstantValue> values,
        ExecutionConstantInSetKind kind)
    {
        ElementType = elementType ?? throw new ArgumentNullException(nameof(elementType));
        _values = Array.AsReadOnly(values?.ToArray() ?? throw new ArgumentNullException(nameof(values)));
        Kind = kind;
    }

    public ExecutionTypeRef ElementType { get; }

    public IReadOnlyList<ExecutionConstantValue> Values => _values;

    public ExecutionConstantInSetKind Kind { get; }

    internal ExecutionConstantInSet(Type elementType, IReadOnlyList<object?> values, ExecutionConstantInSetKind kind)
        : this(
            ExecutionTypeRef.FromClr(elementType),
            values.Select(value => ExecutionConstantValue.FromClr(value, ExecutionTypeRef.FromClr(elementType))).ToArray(),
            kind)
    {
    }
}
