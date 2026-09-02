using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Musoq.Evaluator.IR.Planning;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

/// <summary>
/// Target-neutral transfer metadata for a query-scoped source row. It carries
/// only portable type descriptors and immutable logical shape facts; generated
/// CLR carrier names are deliberately assigned by the renderer.
/// </summary>
public sealed record ExecutionQueryRowSourceTransfer
{
    public ExecutionQueryRowSourceTransfer(
        ExecutionQueryRowCarrier carrier,
        string shapeFingerprint,
        IReadOnlyList<ExecutionQueryRowField> fields)
        : this(
            carrier,
            carrier == ExecutionQueryRowCarrier.ReadonlyStruct
                ? ExecutionQueryRowLifetime.ScanLocal
                : ExecutionQueryRowLifetime.EscapesScan,
            shapeFingerprint,
            fields)
    {
    }

    public ExecutionQueryRowSourceTransfer(
        ExecutionQueryRowCarrier carrier,
        ExecutionQueryRowLifetime lifetime,
        string shapeFingerprint,
        IReadOnlyList<ExecutionQueryRowField> fields)
    {
        if (carrier is not (ExecutionQueryRowCarrier.ReadonlyStruct or ExecutionQueryRowCarrier.SealedClass))
            throw new ArgumentOutOfRangeException(nameof(carrier), carrier, "Unknown query-row carrier.");
        if (lifetime is not (ExecutionQueryRowLifetime.ScanLocal or ExecutionQueryRowLifetime.EscapesScan))
            throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, "Unknown query-row lifetime.");
        if (carrier == ExecutionQueryRowCarrier.ReadonlyStruct && lifetime != ExecutionQueryRowLifetime.ScanLocal)
        {
            throw new ArgumentException(
                "A readonly-struct query-row carrier must remain scan-local.",
                nameof(lifetime));
        }

        if (string.IsNullOrWhiteSpace(shapeFingerprint) || shapeFingerprint.Length != 64 ||
            shapeFingerprint.Any(static character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException(
                "A query-row shape fingerprint must be a 64-character hexadecimal digest.",
                nameof(shapeFingerprint));
        }

        ArgumentNullException.ThrowIfNull(fields);
        var values = fields.OrderBy(static field => field.Slot).ToArray();
        for (var index = 0; index < values.Length; index++)
        {
            if (values[index].Slot != index)
                throw new ArgumentException("Query-row field slots must be dense and zero-based.", nameof(fields));
        }

        if (values.Select(static field => field.SourceColumnIndex).Distinct().Count() != values.Length)
            throw new ArgumentException("Query-row source ordinals must be unique.", nameof(fields));

        Carrier = carrier;
        Lifetime = lifetime;
        ShapeFingerprint = shapeFingerprint.ToUpperInvariant();
        Fields = new ReadOnlyCollection<ExecutionQueryRowField>(values);
    }

    public ExecutionQueryRowCarrier Carrier { get; }

    public ExecutionQueryRowLifetime Lifetime { get; }

    public string ShapeFingerprint { get; }

    public IReadOnlyList<ExecutionQueryRowField> Fields { get; }

    internal static ExecutionQueryRowSourceTransfer FromPlanner(SourceTransferStrategyPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (plan.Mode != SourceTransferMode.QueryScopedRows ||
            plan.Shape == null ||
            plan.Carrier == null ||
            plan.Lifetime == null)
        {
            throw new InvalidOperationException(
                $"Source transfer plan '{plan.SourceContextId}' is not a complete query-scoped row selection.");
        }

        return new ExecutionQueryRowSourceTransfer(
            plan.Carrier.Value switch
            {
                SourceQueryRowCarrier.ReadonlyStruct => ExecutionQueryRowCarrier.ReadonlyStruct,
                SourceQueryRowCarrier.SealedClass => ExecutionQueryRowCarrier.SealedClass,
                _ => throw new InvalidOperationException($"Unknown source query-row carrier '{plan.Carrier}'.")
            },
            plan.Lifetime.Value switch
            {
                SourceQueryRowLifetime.ScanLocal => ExecutionQueryRowLifetime.ScanLocal,
                SourceQueryRowLifetime.EscapesScan => ExecutionQueryRowLifetime.EscapesScan,
                _ => throw new InvalidOperationException($"Unknown source query-row lifetime '{plan.Lifetime}'.")
            },
            plan.Shape.Fingerprint,
            plan.Shape.Fields.Select(static field => new ExecutionQueryRowField(
                field.Slot,
                field.SourceColumnIndex,
                field.Name,
                ExecutionClrBindingFactory.FromClr(field.FieldType),
                ExecutionClrBindingFactory.FromClr(field.SourceReadType),
                field.EnumType,
                field.IsNullable,
                field.ReadModifiers)).ToArray());
    }
}

public enum ExecutionQueryRowCarrier
{
    ReadonlyStruct = 0,
    SealedClass = 1
}

public enum ExecutionQueryRowLifetime
{
    ScanLocal = 0,
    EscapesScan = 1
}

public sealed record ExecutionQueryRowField
{
    public ExecutionQueryRowField(
        int slot,
        int sourceColumnIndex,
        string name,
        ExecutionTypeRef fieldType,
        bool isNullable,
        IReadOnlyDictionary<string, string>? readModifiers = null)
        : this(slot, sourceColumnIndex, name, fieldType, fieldType, null, isNullable, readModifiers)
    {
    }

    public ExecutionQueryRowField(
        int slot,
        int sourceColumnIndex,
        string name,
        ExecutionTypeRef fieldType,
        ExecutionTypeRef sourceReadType,
        EnumTypeDescriptor? enumType,
        bool isNullable,
        IReadOnlyDictionary<string, string>? readModifiers = null)
    {
        if (slot < 0)
            throw new ArgumentOutOfRangeException(nameof(slot));
        if (sourceColumnIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(sourceColumnIndex));

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(fieldType);
        ArgumentNullException.ThrowIfNull(sourceReadType);
        Slot = slot;
        SourceColumnIndex = sourceColumnIndex;
        Name = name;
        FieldType = fieldType;
        SourceReadType = sourceReadType;
        EnumType = enumType;
        IsNullable = isNullable;
        ReadModifiers = new ReadOnlyDictionary<string, string>(
            readModifiers is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(readModifiers, StringComparer.Ordinal));
    }

    public int Slot { get; }

    public int SourceColumnIndex { get; }

    public string Name { get; }

    public ExecutionTypeRef FieldType { get; }

    public ExecutionTypeRef SourceReadType { get; }

    public EnumTypeDescriptor? EnumType { get; }

    public bool IsNullable { get; }

    public IReadOnlyDictionary<string, string> ReadModifiers { get; }
}
