using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Musoq.Evaluator.IR.Expressions;

/// <summary>
/// Represents a complete CTE relation supplied to a datasource collection
/// argument.  This is intentionally a distinct expression from
/// <see cref="CteTableRef"/> so relation binding cannot fall back to a scalar
/// or string conversion.
/// </summary>
public sealed record CteCollectionInput : IrExpression
{
    public CteCollectionInput(
        string cteName,
        Type returnType,
        Type elementType,
        IEnumerable<CteCollectionInputField> fields)
        : base(returnType ?? throw new ArgumentNullException(nameof(returnType)))
    {
        if (string.IsNullOrWhiteSpace(cteName))
            throw new ArgumentException("A CTE name is required.", nameof(cteName));

        CteName = cteName;
        ElementType = elementType ?? throw new ArgumentNullException(nameof(elementType));
        Fields = new ReadOnlyCollection<CteCollectionInputField>(
            (fields ?? throw new ArgumentNullException(nameof(fields))).ToArray());

        if (Fields.Count == 0)
            throw new ArgumentException("A CTE relation must expose at least one field.", nameof(fields));
        if (Fields.Any(static field => field is null))
            throw new ArgumentException("CTE relation fields cannot be null.", nameof(fields));
        if (Fields.Select(static field => field.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Fields.Count)
            throw new ArgumentException("CTE relation field names must be unique.", nameof(fields));
    }

    public string CteName { get; }

    public Type ElementType { get; }

    public IReadOnlyList<CteCollectionInputField> Fields { get; }
}

/// <summary>One output field available while adapting a CTE row.</summary>
public sealed record CteCollectionInputField(string Name, int Index, Type Type)
{
    public string Name { get; } = string.IsNullOrWhiteSpace(Name)
        ? throw new ArgumentException("CTE field name cannot be empty.", nameof(Name))
        : Name;

    public Type Type { get; } = Type ?? throw new ArgumentNullException(nameof(Type));

    public int Index { get; } = Index < 0
        ? throw new ArgumentOutOfRangeException(nameof(Index))
        : Index;
}
