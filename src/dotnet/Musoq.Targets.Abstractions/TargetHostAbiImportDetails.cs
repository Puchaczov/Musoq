using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace Musoq.Targets.Abstractions;

internal abstract record TargetHostAbiImportDetails
{
    public abstract TargetHostAbiImportKind Kind { get; }

    public abstract IReadOnlyDictionary<string, string> Attributes { get; }

    protected static IReadOnlyDictionary<string, string> BuildAttributes(
        params (string Key, object? Value)[] values)
    {
        var attributes = new Dictionary<string, string>(values.Length, StringComparer.Ordinal);
        foreach (var (key, value) in values)
            attributes[RequireText(key, nameof(key))] = FormatAttributeValue(value);

        return new ReadOnlyDictionary<string, string>(attributes);
    }

    protected static string RequireText(string value, string parameterName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value cannot be null or whitespace.", parameterName)
            : value;
    }

    protected static int RequireNonNegative(int value, string parameterName)
    {
        return value < 0
            ? throw new ArgumentOutOfRangeException(parameterName, "Value cannot be negative.")
            : value;
    }

    protected static IReadOnlyList<T> Freeze<T>(IEnumerable<T>? values)
    {
        return Array.AsReadOnly(values?.ToArray() ?? []);
    }

    private static string FormatAttributeValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            bool boolean => boolean ? "true" : "false",
            Enum enumValue => enumValue.ToString(),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }
}

internal sealed record TargetCustomAbiImportDetails : TargetHostAbiImportDetails
{
    public TargetCustomAbiImportDetails(
        TargetHostAbiImportKind kind,
        IReadOnlyDictionary<string, string>? rawAttributes)
    {
        Kind = kind;
        Attributes = new ReadOnlyDictionary<string, string>(
            rawAttributes is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(rawAttributes, StringComparer.Ordinal));
    }

    public override TargetHostAbiImportKind Kind { get; }

    public override IReadOnlyDictionary<string, string> Attributes { get; }
}

internal enum TargetSourcePlanOperation
{
    Columns = 0,
    Predicate = 1,
    OrderBy = 2,
    Skip = 3,
    Take = 4
}

internal sealed record TargetSourceArgumentAbiContract(
    int Position,
    ExecutionPortableTypeDescriptor TypeSymbol);

internal sealed record TargetSourceFieldAbiContract
{
    public TargetSourceFieldAbiContract(
        int index,
        string name,
        ExecutionPortableTypeDescriptor type,
        ExecutionPortableTypeDescriptor publicType,
        string nullability,
        IReadOnlyDictionary<string, string>? readModifiers)
    {
        Index = RequireNonNegative(index, nameof(index));
        Name = RequireText(name, nameof(name));
        TypeSymbol = type ?? throw new ArgumentNullException(nameof(type));
        PublicTypeSymbol = publicType ?? throw new ArgumentNullException(nameof(publicType));
        Nullability = RequireText(nullability, nameof(nullability));
        ReadModifiers = new ReadOnlyDictionary<string, string>(
            readModifiers is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(readModifiers, StringComparer.Ordinal));
    }

    public int Index { get; }

    public string Name { get; }

    public ExecutionPortableTypeDescriptor TypeSymbol { get; }

    public ExecutionPortableTypeDescriptor PublicTypeSymbol { get; }

    public string Nullability { get; }

    public IReadOnlyDictionary<string, string> ReadModifiers { get; }

    private static string RequireText(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value cannot be null or whitespace.", parameterName)
            : value;

    private static int RequireNonNegative(int value, string parameterName) =>
        value < 0
            ? throw new ArgumentOutOfRangeException(parameterName, "Value cannot be negative.")
            : value;
}

internal sealed record TargetRuntimeSettingAbiContract
{
    public TargetRuntimeSettingAbiContract(
        string key,
        bool required,
        string phases,
        string status,
        string? nonSecretDescription)
    {
        Key = string.IsNullOrWhiteSpace(key)
            ? throw new ArgumentException("Setting key cannot be null or whitespace.", nameof(key))
            : key;
        Required = required;
        Phases = phases ?? string.Empty;
        Status = status ?? string.Empty;
        NonSecretDescription = nonSecretDescription ?? string.Empty;
    }

    public string Key { get; }

    public bool Required { get; }

    public string Phases { get; }

    public string Status { get; }

    public string NonSecretDescription { get; }
}

internal sealed record TargetSourceAccessAbiDetails : TargetHostAbiImportDetails
{
    public TargetSourceAccessAbiDetails(
        string sourceKind,
        string sourceContextId,
        string schemaName,
        string methodName,
        string rowsType,
        ExecutionPortableSymbolPortability rowsPortability,
        string sourceType,
        ExecutionPortableSymbolPortability? sourcePortability,
        IEnumerable<TargetSourceArgumentAbiContract>? arguments,
        IEnumerable<TargetSourceFieldAbiContract>? fields,
        IEnumerable<TargetSourcePlanOperation>? acceptedOperations,
        IEnumerable<TargetRuntimeSettingAbiContract>? runtimeSettings)
    {
        SourceKind = RequireText(sourceKind, nameof(sourceKind));
        SourceContextId = RequireText(sourceContextId, nameof(sourceContextId));
        SchemaName = RequireText(schemaName, nameof(schemaName));
        MethodName = RequireText(methodName, nameof(methodName));
        RowsType = RequireText(rowsType, nameof(rowsType));
        RowsPortability = rowsPortability;
        SourceType = sourceType ?? string.Empty;
        SourcePortability = sourcePortability;
        var argumentValues = Freeze(arguments).OrderBy(static argument => argument.Position).ToArray();
        if (argumentValues.Any(static argument => argument.Position < 0) ||
            argumentValues.Select(static argument => argument.Position).Distinct().Count() != argumentValues.Length)
        {
            throw new ArgumentException("Source argument positions must be unique and non-negative.", nameof(arguments));
        }

        var fieldValues = Freeze(fields).OrderBy(static field => field.Index).ThenBy(static field => field.Name, StringComparer.Ordinal).ToArray();
        if (fieldValues.Select(static field => field.Index).Distinct().Count() != fieldValues.Length)
            throw new ArgumentException("Source field indices must be unique.", nameof(fields));

        var runtimeSettingValues = Freeze(runtimeSettings).OrderBy(static setting => setting.Key, StringComparer.Ordinal).ToArray();
        if (runtimeSettingValues.Select(static setting => setting.Key).Distinct(StringComparer.Ordinal).Count() != runtimeSettingValues.Length)
            throw new ArgumentException("Runtime setting keys must be unique.", nameof(runtimeSettings));

        Arguments = Array.AsReadOnly(argumentValues);
        Fields = Array.AsReadOnly(fieldValues);
        AcceptedOperations = Array.AsReadOnly(Freeze(acceptedOperations).Distinct().OrderBy(static operation => operation).ToArray());
        RuntimeSettings = Array.AsReadOnly(runtimeSettingValues);
        Attributes = BuildAttributes(
            ("kind", SourceKind),
            ("sourceContextId", SourceContextId),
            ("schemaName", SchemaName),
            ("methodName", MethodName),
            ("rowsType", RowsType),
            ("rowsPortability", RowsPortability),
            ("sourceType", SourceType),
            ("sourcePortability", (object?)SourcePortability),
            ("argumentCount", Arguments.Count),
            ("fieldCount", Fields.Count),
            ("acceptedOperations", string.Join(",", AcceptedOperations)),
            ("runtimeSettingKeys", string.Join(",", RuntimeSettings.Select(static setting => setting.Key))));
    }

    public override TargetHostAbiImportKind Kind => TargetHostAbiImportKind.SourceAccess;

    public string SourceKind { get; }

    public string SourceContextId { get; }

    public string SchemaName { get; }

    public string MethodName { get; }

    public string RowsType { get; }

    public ExecutionPortableSymbolPortability RowsPortability { get; }

    public string SourceType { get; }

    public ExecutionPortableSymbolPortability? SourcePortability { get; }

    public IReadOnlyList<TargetSourceArgumentAbiContract> Arguments { get; }

    public IReadOnlyList<TargetSourceFieldAbiContract> Fields { get; }

    public IReadOnlyList<TargetSourcePlanOperation> AcceptedOperations { get; }

    public IReadOnlyList<TargetRuntimeSettingAbiContract> RuntimeSettings { get; }

    public int FieldCount => Fields.Count;

    public override IReadOnlyDictionary<string, string> Attributes { get; }
}

internal sealed record TargetPluginInvocationAbiDetails(
    string Detail,
    string Callable,
    ExecutionPortableSymbolPortability CallablePortability,
    string MethodName,
    string DeclaringType,
    int ParameterCount) : TargetHostAbiImportDetails
{
    public override TargetHostAbiImportKind Kind => TargetHostAbiImportKind.PluginInvocation;

    public override IReadOnlyDictionary<string, string> Attributes { get; } = BuildAttributes(
        ("detail", RequireText(Detail, nameof(Detail))),
        ("callable", RequireText(Callable, nameof(Callable))),
        ("callablePortability", CallablePortability),
        ("methodName", RequireText(MethodName, nameof(MethodName))),
        ("declaringType", DeclaringType ?? string.Empty),
        ("parameterCount", RequireNonNegative(ParameterCount, nameof(ParameterCount))));
}

internal sealed record TargetRowShapeTransferAbiDetails(
    string RowKind,
    string Name,
    string TypeName,
    ExecutionPortableSymbolPortability? TypePortability,
    int FieldCount) : TargetHostAbiImportDetails
{
    public override TargetHostAbiImportKind Kind => TargetHostAbiImportKind.RowShapeTransfer;

    public override IReadOnlyDictionary<string, string> Attributes { get; } = BuildAttributes(
        ("kind", RequireText(RowKind, nameof(RowKind))),
        ("name", RequireText(Name, nameof(Name))),
        ("type", TypeName ?? string.Empty),
        ("typePortability", (object?)TypePortability),
        ("fieldCount", RequireNonNegative(FieldCount, nameof(FieldCount))));
}

internal sealed record TargetNullTypeCoercionAbiDetails(
    string Semantics,
    bool UsesNullableValueTypes,
    bool UsesObjectNulls,
    bool UsesFieldNullabilityMetadata) : TargetHostAbiImportDetails
{
    public override TargetHostAbiImportKind Kind => TargetHostAbiImportKind.NullTypeCoercion;

    public override IReadOnlyDictionary<string, string> Attributes { get; } = BuildAttributes(
        ("semantics", RequireText(Semantics, nameof(Semantics))),
        ("usesNullableValueTypes", UsesNullableValueTypes),
        ("usesObjectNulls", UsesObjectNulls),
        ("usesFieldNullabilityMetadata", UsesFieldNullabilityMetadata));
}

internal sealed record TargetCancellationAbiDetails(
    bool RequiresCancellationToken,
    bool RequiresParallelCancellation) : TargetHostAbiImportDetails
{
    public override TargetHostAbiImportKind Kind => TargetHostAbiImportKind.Cancellation;

    public override IReadOnlyDictionary<string, string> Attributes { get; } = BuildAttributes(
        ("requiresCancellationToken", RequiresCancellationToken),
        ("requiresParallelCancellation", RequiresParallelCancellation));
}

internal sealed record TargetDiagnosticsAbiDetails(
    bool RequiresBuildDiagnostics,
    bool RequiresSourceDiagnostics,
    bool RequiresRuntimeExceptionDiagnostics) : TargetHostAbiImportDetails
{
    public override TargetHostAbiImportKind Kind => TargetHostAbiImportKind.Diagnostics;

    public override IReadOnlyDictionary<string, string> Attributes { get; } = BuildAttributes(
        ("requiresBuildDiagnostics", RequiresBuildDiagnostics),
        ("requiresSourceDiagnostics", RequiresSourceDiagnostics),
        ("requiresRuntimeExceptionDiagnostics", RequiresRuntimeExceptionDiagnostics));
}

internal sealed record TargetProfilingAbiDetails(
    bool SupportsSourceBoundaryProfiling,
    bool SupportsOperatorProfiling,
    int SourceBoundaryCount,
    int OperatorCount) : TargetHostAbiImportDetails
{
    public override TargetHostAbiImportKind Kind => TargetHostAbiImportKind.Profiling;

    public override IReadOnlyDictionary<string, string> Attributes { get; } = BuildAttributes(
        ("supportsSourceBoundaryProfiling", SupportsSourceBoundaryProfiling),
        ("supportsOperatorProfiling", SupportsOperatorProfiling),
        ("sourceBoundaryCount", RequireNonNegative(SourceBoundaryCount, nameof(SourceBoundaryCount))),
        ("operatorCount", RequireNonNegative(OperatorCount, nameof(OperatorCount))));
}
