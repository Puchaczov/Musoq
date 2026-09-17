using System.Collections.Generic;
using System.Linq;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.IR.Execution;

/// <summary>Portable description of one structural field slot.</summary>
public sealed record ExecutionStructuralFieldPlan(
    string Name,
    ExecutionTypeRef Type,
    bool Required,
    bool HasDefault,
    string? DefaultText,
    ExecutionStructuralShape? Shape = null)
{
    public string Name { get; } = string.IsNullOrWhiteSpace(Name)
        ? throw new ArgumentException("Structural field name cannot be empty.", nameof(Name))
        : Name;

    public ExecutionTypeRef Type { get; } = Type ?? throw new ArgumentNullException(nameof(Type));

    public string? DefaultText { get; } = HasDefault ? DefaultText : null;

    /// <summary>Gets the recursive shape of this field when it is structured.</summary>
    public ExecutionStructuralShape? Shape { get; } = Shape;
}

/// <summary>Portable structural shape metadata used by preparation operations.</summary>
public sealed record ExecutionStructuralShape
{
    public ExecutionStructuralShape(
        StructuralTypeKind kind,
        ExecutionTypeRef? scalarType,
        IEnumerable<ExecutionStructuralFieldPlan>? fields,
        ExecutionTypeRef? elementType,
        bool nullable,
        ExecutionStructuralShape? elementShape = null)
    {
        if (kind == StructuralTypeKind.Scalar && scalarType == null)
            throw new ArgumentNullException(nameof(scalarType));
        if (kind == StructuralTypeKind.Record && fields == null)
            throw new ArgumentNullException(nameof(fields));
        if (kind == StructuralTypeKind.Collection && elementType == null)
            throw new ArgumentNullException(nameof(elementType));

        Kind = kind;
        ScalarType = scalarType;
        Fields = Array.AsReadOnly((fields ?? []).ToArray());
        ElementType = elementType;
        IsNullable = nullable;
        ElementShape = elementShape;
    }

    public StructuralTypeKind Kind { get; }

    public ExecutionTypeRef? ScalarType { get; }

    public IReadOnlyList<ExecutionStructuralFieldPlan> Fields { get; }

    public ExecutionTypeRef? ElementType { get; }

    public bool IsNullable { get; }

    /// <summary>Gets the recursive element shape for a collection.</summary>
    public ExecutionStructuralShape? ElementShape { get; }

    public string CanonicalType =>
        Kind switch
        {
            StructuralTypeKind.Scalar => ScalarType!.DisplayName + (IsNullable ? "?" : string.Empty),
            StructuralTypeKind.Record => $"({string.Join(", ", Fields.Select(static slot => $"{slot.Name}: {(slot.Shape?.CanonicalType ?? slot.Type.DisplayName)}{(slot.HasDefault ? $" = {slot.DefaultText ?? "null"}" : string.Empty)}"))})" + (IsNullable ? "?" : string.Empty),
            StructuralTypeKind.Collection => $"{(ElementShape?.CanonicalType ?? ElementType!.DisplayName)}[]{(IsNullable ? "?" : string.Empty)}",
            _ => throw new InvalidOperationException($"Unknown structural type '{Kind}'.")
        };
}

/// <summary>Identifies where a structural value originated.</summary>
public enum ExecutionStructuralInputOrigin
{
    Inline,
    Let,
    Parameter,
    Cte
}

/// <summary>Describes how a structural value's resource usage is measured.</summary>
public enum ExecutionStructuralMetricsStrategy
{
    None,
    CompileTime,
    Runtime,
    TypedPrepass
}

/// <summary>Describes ownership of storage passed to a receiving source.</summary>
public enum ExecutionStructuralOwnershipMode
{
    Borrowed,
    Transfer,
    Copy,
    ConstructFresh
}

/// <summary>Portable resource limits for one structural input root.</summary>
public readonly record struct ExecutionStructuralLimitPlan
{
    public ExecutionStructuralLimitPlan(int maxDepth, long maxNodes, long maxStringBytes)
    {
        if (maxDepth <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxDepth));
        if (maxNodes <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxNodes));
        if (maxStringBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxStringBytes));

        MaxDepth = maxDepth;
        MaxNodes = maxNodes;
        MaxStringBytes = maxStringBytes;
    }

    public static ExecutionStructuralLimitPlan Default { get; } = new(32, 100_000, 67_108_864);

    public int MaxDepth { get; }

    public long MaxNodes { get; }

    public long MaxStringBytes { get; }

    public override string ToString() =>
        $"depth={MaxDepth};nodes={MaxNodes};strings={MaxStringBytes}";
}

/// <summary>Limit metadata attached to one structural value at a source demand boundary.</summary>
public sealed record ExecutionStructuralLimitBinding(
    ExecutionStructuralLimitPlan Limits,
    ExecutionStructuralInputOrigin Origin,
    string Path,
    string? SourceContextId = null)
{
    public string Path { get; } = string.IsNullOrWhiteSpace(Path)
        ? throw new ArgumentException("A structural input path is required.", nameof(Path))
        : Path;
}

/// <summary>Controls where a prepared structural value is retained.</summary>
public enum ExecutionStructuralPreparationLifetime
{
    Inline,
    Statement,
    Execution
}

/// <summary>Portable indexed construction plan consumed by a target renderer.</summary>
public sealed record ExecutionStructuralConstructionPlan
{
    public ExecutionStructuralConstructionPlan(
        ExecutionTypeRef targetType,
        ExecutionCallableRef constructor,
        ExecutionStructuralShape inputShape,
        IEnumerable<int> sourceFieldIndexes,
        IEnumerable<ExecutionExpression?> defaults,
        ExecutionStructuralPreparationLifetime lifetime,
        ExecutionStructuralLimitPlan? limits = null,
        ExecutionStructuralInputOrigin origin = ExecutionStructuralInputOrigin.Inline,
        ExecutionStructuralMetricsStrategy metricsStrategy = ExecutionStructuralMetricsStrategy.Runtime,
        ExecutionStructuralOwnershipMode ownership = ExecutionStructuralOwnershipMode.ConstructFresh)
    {
        TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
        Constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));
        InputShape = inputShape ?? throw new ArgumentNullException(nameof(inputShape));
        SourceFieldIndexes = Array.AsReadOnly((sourceFieldIndexes ?? throw new ArgumentNullException(nameof(sourceFieldIndexes))).ToArray());
        Defaults = Array.AsReadOnly((defaults ?? throw new ArgumentNullException(nameof(defaults))).ToArray());
        Lifetime = lifetime;
        Limits = limits ?? ExecutionStructuralLimitPlan.Default;
        Origin = origin;
        MetricsStrategy = metricsStrategy;
        Ownership = ownership;

        if (SourceFieldIndexes.Count != Constructor.Descriptor.ParameterTypes.Count ||
            Defaults.Count != SourceFieldIndexes.Count)
            throw new ArgumentException("Construction slot metadata must match constructor arity.", nameof(sourceFieldIndexes));
    }

    public ExecutionTypeRef TargetType { get; }

    public ExecutionCallableRef Constructor { get; }

    public ExecutionStructuralShape InputShape { get; }

    public IReadOnlyList<int> SourceFieldIndexes { get; }

    public IReadOnlyList<ExecutionExpression?> Defaults { get; }

    public ExecutionStructuralPreparationLifetime Lifetime { get; }

    public ExecutionStructuralLimitPlan Limits { get; }

    public ExecutionStructuralInputOrigin Origin { get; }

    public ExecutionStructuralMetricsStrategy MetricsStrategy { get; }

    public ExecutionStructuralOwnershipMode Ownership { get; }

    /// <summary>Gets a stable metadata representation used by plan fingerprints.</summary>
    public string MetadataFingerprint =>
        $"target={TargetType.StableId};constructor={Constructor.StableId};shape={InputShape.CanonicalType};" +
        $"lifetime={Lifetime};origin={Origin};metrics={MetricsStrategy};ownership={Ownership};limits={Limits};" +
        $"indexes={string.Join(',', SourceFieldIndexes)};defaults={string.Join(',', Defaults.Select(static value => value?.ToString() ?? "-"))}";
}
