using System.Collections.Generic;
using System.Linq;

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
            ExecutionClrBindingFactory.FromClr(elementType),
            values.Select(value => ExecutionConstantValue.FromClr(value, ExecutionClrBindingFactory.FromClr(elementType))).ToArray(),
            kind)
    {
    }
}
