using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Musoq.Evaluator.IR.Expressions;

/// <summary>One named expression in a structural record literal.</summary>
public sealed record StructuralFieldExpression(string Name, IrExpression Value)
{
    public string Name { get; } = string.IsNullOrWhiteSpace(Name)
        ? throw new ArgumentException("Structural field name cannot be empty.", nameof(Name))
        : Name;

    public IrExpression Value { get; } = Value ?? throw new ArgumentNullException(nameof(Value));
}

/// <summary>Named structural record expression with an optional receiving CLR type.</summary>
public sealed record StructuralRecordLiteral : IrExpression
{
    public StructuralRecordLiteral(
        Type returnType,
        IEnumerable<StructuralFieldExpression> fields,
        Type? targetType = null)
        : base(returnType ?? throw new ArgumentNullException(nameof(returnType)))
    {
        Fields = new ReadOnlyCollection<StructuralFieldExpression>((fields ?? throw new ArgumentNullException(nameof(fields))).ToArray());
        if (Fields.Any(static field => field is null))
            throw new ArgumentException("Structural record fields cannot be null.", nameof(fields));
        if (Fields.Select(static field => field.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Fields.Count)
            throw new ArgumentException("Structural record field names must be unique.", nameof(fields));
        TargetType = targetType;
    }

    public IReadOnlyList<StructuralFieldExpression> Fields { get; }

    /// <summary>Gets the concrete receiver type when this record is a source argument.</summary>
    public Type? TargetType { get; }
}

/// <summary>Ordered structural array expression with an expected element type.</summary>
public sealed record StructuralArrayLiteral : IrExpression
{
    public StructuralArrayLiteral(
        Type returnType,
        Type elementType,
        IEnumerable<IrExpression> elements)
        : base(returnType ?? throw new ArgumentNullException(nameof(returnType)))
    {
        ElementType = elementType ?? throw new ArgumentNullException(nameof(elementType));
        Elements = new ReadOnlyCollection<IrExpression>((elements ?? throw new ArgumentNullException(nameof(elements))).ToArray());
        if (Elements.Any(static element => element is null))
            throw new ArgumentException("Structural array elements cannot be null.", nameof(elements));
    }

    public Type ElementType { get; }

    public IReadOnlyList<IrExpression> Elements { get; }
}

/// <summary>Adapts a compile-time structural value to a typed source receiver.</summary>
public sealed record StructuralConversion(IrExpression Value, Type TargetType) : IrExpression(
    TargetType ?? throw new ArgumentNullException(nameof(TargetType)))
{
    public IrExpression Value { get; } = Value ?? throw new ArgumentNullException(nameof(Value));

    public Type TargetType { get; } = TargetType;
}