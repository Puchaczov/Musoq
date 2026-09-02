using System.Collections.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

public sealed record FieldBinding
{
    public FieldBinding(
        string name,
        string qualifiedName,
        int outputIndex,
        ExecutionTypeRef type,
        FieldNullability nullability,
        FieldAccessStrategy accessStrategy,
        ExecutionTypeRef? publicType = null,
        IReadOnlyDictionary<string, string>? readModifiers = null,
        ExecutionTypeRef? sourceReadType = null,
        EnumTypeDescriptor? enumType = null)
    {
        Name = name;
        QualifiedName = qualifiedName;
        OutputIndex = outputIndex;
        Type = type;
        Nullability = nullability;
        AccessStrategy = accessStrategy;
        PublicType = publicType;
        SourceReadType = sourceReadType ?? publicType ?? type;
        EnumType = enumType;
        ReadModifiers = ColumnReadModifiers.Create(readModifiers);
    }

    internal FieldBinding(
        string name,
        string qualifiedName,
        int outputIndex,
        Type type,
        FieldNullability nullability,
        FieldAccessStrategy accessStrategy,
        Type? publicType = null,
        IReadOnlyDictionary<string, string>? readModifiers = null,
        Type? sourceReadType = null,
        EnumTypeDescriptor? enumType = null)
        : this(
            name,
            qualifiedName,
            outputIndex,
            ExecutionClrBindingFactory.FromClr(type),
            nullability,
            accessStrategy,
            ExecutionClrBindingFactory.FromOptionalClr(publicType),
            readModifiers,
            ExecutionClrBindingFactory.FromOptionalClr(sourceReadType),
            enumType)
    {
    }

    public string Name { get; init; }

    public string QualifiedName { get; init; }

    public int OutputIndex { get; init; }

    public ExecutionTypeRef Type { get; init; }

    public FieldNullability Nullability { get; init; }

    public FieldAccessStrategy AccessStrategy { get; init; }

    public ExecutionTypeRef? PublicType { get; init; }

    public ExecutionTypeRef SourceReadType { get; init; }

    public EnumTypeDescriptor? EnumType { get; init; }

    public string? GeneratedTypeName { get; init; }

    public IReadOnlyDictionary<string, string> GeneratedMemberTypeNames { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string> ReadModifiers { get; init; }

    public ColumnStability Stability { get; init; } = ColumnStability.Stable;

    public ExecutionTypeRef ColumnType => PublicType ?? Type;
}
