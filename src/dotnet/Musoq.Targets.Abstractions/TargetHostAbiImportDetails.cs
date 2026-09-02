using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Musoq.Targets.Abstractions;

internal abstract record TargetHostAbiImportDetails
{
    public abstract TargetHostAbiImportKind Kind { get; }

    public abstract IReadOnlyDictionary<string, string> Attributes { get; }

    internal abstract string CanonicalDefinition { get; }

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

    protected static string CreateCanonicalDefinition(
        string kind,
        Action<StringBuilder> append)
    {
        ArgumentNullException.ThrowIfNull(append);
        var builder = new StringBuilder();
        AppendField(builder, "kind", kind);
        append(builder);
        return builder.ToString();
    }

    protected static void AppendField(StringBuilder builder, string name, object? value)
    {
        AppendScalar(builder, name, FormatCanonicalValue(value));
    }

    protected static void AppendDictionary(
        StringBuilder builder,
        string name,
        IReadOnlyDictionary<string, string> values)
    {
        AppendScalar(builder, name, "dictionary");
        var entries = values.OrderBy(static pair => pair.Key, StringComparer.Ordinal).ToArray();
        AppendScalar(builder, name + ".count", entries.Length);
        foreach (var (key, value) in entries)
        {
            AppendScalar(builder, name + ".key", key);
            AppendScalar(builder, name + ".value", value);
        }
    }

    protected static void AppendType(
        StringBuilder builder,
        string name,
        ExecutionPortableTypeDescriptor? descriptor)
    {
        AppendScalar(builder, name + ".present", descriptor is not null);
        if (descriptor is null)
            return;

        AppendScalar(builder, name + ".kind", descriptor.Kind);
        AppendScalar(builder, name + ".stableName", descriptor.StableName);
        AppendScalar(builder, name + ".displayName", descriptor.DisplayName);
        AppendScalar(builder, name + ".portability", descriptor.Portability);
        AppendScalar(builder, name + ".portabilityReason", descriptor.PortabilityReason);
        AppendScalar(builder, name + ".arrayRank", descriptor.ArrayRank);
        AppendScalar(builder, name + ".container.present", descriptor.Container is not null);
        if (descriptor.Container is { } container)
        {
            AppendScalar(builder, name + ".container.kind", container.Kind);
            AppendScalar(builder, name + ".container.ordered", container.IsOrdered);
            AppendScalar(builder, name + ".container.mutable", container.IsMutable);
            AppendScalar(builder, name + ".container.keyEquality", container.RequiresKeyEquality);
            AppendScalar(builder, name + ".container.keyHashing", container.RequiresKeyHashing);
            AppendScalar(builder, name + ".container.binding", container.BindingKind);
        }

        AppendScalar(builder, name + ".arguments.count", descriptor.Arguments.Count);
        for (var index = 0; index < descriptor.Arguments.Count; index++)
            AppendType(builder, name + ".arguments." + index.ToString(CultureInfo.InvariantCulture), descriptor.Arguments[index]);

        AppendScalar(builder, name + ".fields.count", descriptor.Fields.Count);
        for (var index = 0; index < descriptor.Fields.Count; index++)
        {
            var field = descriptor.Fields[index];
            var fieldName = name + ".fields." + index.ToString(CultureInfo.InvariantCulture);
            AppendScalar(builder, fieldName + ".name", field.Name);
            AppendType(builder, fieldName + ".type", field.Type);
            AppendScalar(builder, fieldName + ".nullability", field.Nullability);
        }
    }

    protected static void AppendEnumType(
        StringBuilder builder,
        string name,
        TargetEnumTypeAbiContract? descriptor)
    {
        AppendScalar(builder, name + ".present", descriptor is not null);
        if (descriptor is null)
            return;

        AppendScalar(builder, name + ".displayName", descriptor.DisplayName);
        AppendScalar(builder, name + ".origin", descriptor.Origin);
        AppendScalar(builder, name + ".underlyingKind", descriptor.UnderlyingKind);
        AppendScalar(builder, name + ".flags", descriptor.IsFlags);
        AppendScalar(builder, name + ".fingerprint", descriptor.Fingerprint);
        AppendScalar(builder, name + ".members.count", descriptor.Members.Count);
        for (var index = 0; index < descriptor.Members.Count; index++)
        {
            var member = descriptor.Members[index];
            var memberName = name + ".members." + index.ToString(CultureInfo.InvariantCulture);
            AppendScalar(builder, memberName + ".name", member.Name);
            AppendScalar(builder, memberName + ".rawValue", member.RawValue);
            AppendScalar(builder, memberName + ".canonicalName", member.CanonicalName);
        }
    }

    protected static void AppendScalar(StringBuilder builder, string name, object? value)
    {
        var formatted = FormatCanonicalValue(value);
        builder.Append(name.Length).Append(':').Append(name);
        builder.Append(formatted.Length).Append(':').Append(formatted);
    }

    private static string FormatCanonicalValue(object? value)
    {
        return value switch
        {
            null => "<null>",
            bool boolean => boolean ? "true" : "false",
            Enum enumValue => enumValue.ToString(),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
            _ => value.ToString() ?? string.Empty
        };
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

    internal override string CanonicalDefinition => CreateCanonicalDefinition(
        Kind.ToString(),
        builder => AppendDictionary(builder, "attributes", Attributes));

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

internal sealed record TargetEnumMemberAbiContract
{
    public TargetEnumMemberAbiContract(string name, ulong rawValue, string canonicalName)
    {
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Enum member name cannot be null or whitespace.", nameof(name))
            : name;
        RawValue = rawValue;
        CanonicalName = string.IsNullOrWhiteSpace(canonicalName)
            ? throw new ArgumentException("Canonical enum member name cannot be null or whitespace.", nameof(canonicalName))
            : canonicalName;
    }

    public string Name { get; }

    public ulong RawValue { get; }

    public string CanonicalName { get; }
}

internal sealed record TargetEnumTypeAbiContract
{
    public TargetEnumTypeAbiContract(
        string displayName,
        string origin,
        string underlyingKind,
        bool isFlags,
        string fingerprint,
        IEnumerable<TargetEnumMemberAbiContract>? members)
    {
        DisplayName = RequireText(displayName, nameof(displayName));
        Origin = RequireText(origin, nameof(origin));
        UnderlyingKind = RequireText(underlyingKind, nameof(underlyingKind));
        IsFlags = isFlags;
        if (string.IsNullOrWhiteSpace(fingerprint) || fingerprint.Length != 64 ||
            fingerprint.Any(static character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException(
                "An enum fingerprint must be a 64-character hexadecimal digest.",
                nameof(fingerprint));
        }

        Fingerprint = fingerprint.ToUpperInvariant();
        var values = Freeze(members).ToArray();
        if (values.Select(static member => member.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != values.Length)
            throw new ArgumentException("Enum member names must be unique ignoring case.", nameof(members));
        if (values.Any(member => !values.Any(candidate =>
                string.Equals(candidate.Name, member.CanonicalName, StringComparison.Ordinal) &&
                candidate.RawValue == member.RawValue)))
        {
            throw new ArgumentException(
                "Every enum member must reference a declared canonical member with the same value.",
                nameof(members));
        }

        Members = Array.AsReadOnly(values);
    }

    public string DisplayName { get; }

    public string Origin { get; }

    public string UnderlyingKind { get; }

    public bool IsFlags { get; }

    public string Fingerprint { get; }

    public IReadOnlyList<TargetEnumMemberAbiContract> Members { get; }

    private static string RequireText(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value cannot be null or whitespace.", parameterName)
            : value;

    private static IReadOnlyList<T> Freeze<T>(IEnumerable<T>? values) =>
        Array.AsReadOnly(values?.ToArray() ?? []);
}

internal sealed record TargetSourceFieldAbiContract
{
    public TargetSourceFieldAbiContract(
        int index,
        string name,
        ExecutionPortableTypeDescriptor type,
        ExecutionPortableTypeDescriptor publicType,
        string nullability,
        IReadOnlyDictionary<string, string>? readModifiers)
        : this(index, name, type, publicType, publicType, null, nullability, readModifiers)
    {
    }

    public TargetSourceFieldAbiContract(
        int index,
        string name,
        ExecutionPortableTypeDescriptor type,
        ExecutionPortableTypeDescriptor publicType,
        ExecutionPortableTypeDescriptor sourceReadType,
        TargetEnumTypeAbiContract? enumType,
        string nullability,
        IReadOnlyDictionary<string, string>? readModifiers)
    {
        Index = RequireNonNegative(index, nameof(index));
        Name = RequireText(name, nameof(name));
        TypeSymbol = type ?? throw new ArgumentNullException(nameof(type));
        PublicTypeSymbol = publicType ?? throw new ArgumentNullException(nameof(publicType));
        SourceReadTypeSymbol = sourceReadType ?? throw new ArgumentNullException(nameof(sourceReadType));
        EnumType = enumType;
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

    public ExecutionPortableTypeDescriptor SourceReadTypeSymbol { get; }

    public TargetEnumTypeAbiContract? EnumType { get; }

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

    internal override string CanonicalDefinition => CreateCanonicalDefinition(
        Kind.ToString(),
        builder =>
        {
            AppendScalar(builder, "sourceKind", SourceKind);
            AppendScalar(builder, "sourceContextId", SourceContextId);
            AppendScalar(builder, "schemaName", SchemaName);
            AppendScalar(builder, "methodName", MethodName);
            AppendScalar(builder, "rowsType", RowsType);
            AppendScalar(builder, "rowsPortability", RowsPortability);
            AppendScalar(builder, "sourceType", SourceType);
            AppendScalar(builder, "sourcePortability", SourcePortability);
            AppendScalar(builder, "arguments.count", Arguments.Count);
            foreach (var argument in Arguments)
            {
                AppendScalar(builder, "arguments.position", argument.Position);
                AppendType(builder, "arguments.type", argument.TypeSymbol);
            }

            AppendScalar(builder, "fields.count", Fields.Count);
            foreach (var sourceField in Fields)
            {
                AppendScalar(builder, "fields.index", sourceField.Index);
                AppendScalar(builder, "fields.name", sourceField.Name);
                AppendType(builder, "fields.type", sourceField.TypeSymbol);
                AppendType(builder, "fields.publicType", sourceField.PublicTypeSymbol);
                AppendType(builder, "fields.sourceReadType", sourceField.SourceReadTypeSymbol);
                AppendEnumType(builder, "fields.enumType", sourceField.EnumType);
                AppendScalar(builder, "fields.nullability", sourceField.Nullability);
                AppendDictionary(builder, "fields.readModifiers", sourceField.ReadModifiers);
            }

            AppendScalar(builder, "acceptedOperations.count", AcceptedOperations.Count);
            foreach (var operation in AcceptedOperations)
                AppendScalar(builder, "acceptedOperations.item", operation);

            AppendScalar(builder, "runtimeSettings.count", RuntimeSettings.Count);
            foreach (var setting in RuntimeSettings)
            {
                AppendScalar(builder, "runtimeSettings.key", setting.Key);
                AppendScalar(builder, "runtimeSettings.required", setting.Required);
                AppendScalar(builder, "runtimeSettings.phases", setting.Phases);
                AppendScalar(builder, "runtimeSettings.status", setting.Status);
                AppendScalar(builder, "runtimeSettings.description", setting.NonSecretDescription);
            }
        });

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

internal sealed record TargetQueryRowSourceAccessAbiDetails : TargetHostAbiImportDetails
{
    public TargetQueryRowSourceAccessAbiDetails(
        string sourceContextId,
        string schemaName,
        string methodName,
        string carrier,
        string lifetime,
        string shapeFingerprint,
        IEnumerable<TargetQueryRowFieldAbiContract>? fields)
    {
        SourceContextId = RequireText(sourceContextId, nameof(sourceContextId));
        SchemaName = RequireText(schemaName, nameof(schemaName));
        MethodName = RequireText(methodName, nameof(methodName));
        Carrier = RequireText(carrier, nameof(carrier));
        Lifetime = RequireText(lifetime, nameof(lifetime));
        if (Carrier is not ("ReadonlyStruct" or "SealedClass"))
            throw new ArgumentException($"Unknown query-row carrier '{Carrier}'.", nameof(carrier));
        if (Lifetime is not ("ScanLocal" or "EscapesScan"))
            throw new ArgumentException($"Unknown query-row lifetime '{Lifetime}'.", nameof(lifetime));
        if (Carrier == "ReadonlyStruct" && Lifetime != "ScanLocal")
            throw new ArgumentException("A readonly-struct query-row carrier must remain scan-local.", nameof(lifetime));
        if (string.IsNullOrWhiteSpace(shapeFingerprint) || shapeFingerprint.Length != 64 ||
            shapeFingerprint.Any(static character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException(
                "A query-row shape fingerprint must be a 64-character hexadecimal digest.",
                nameof(shapeFingerprint));
        }

        ShapeFingerprint = shapeFingerprint.ToUpperInvariant();
        var values = Freeze(fields).OrderBy(static field => field.Slot).ToArray();
        for (var slot = 0; slot < values.Length; slot++)
        {
            if (values[slot].Slot != slot)
                throw new ArgumentException("Query-row field slots must be dense and zero-based.", nameof(fields));
        }

        if (values.Select(static field => field.SourceColumnIndex).Distinct().Count() != values.Length)
            throw new ArgumentException("Query-row source ordinals must be unique.", nameof(fields));

        Fields = Array.AsReadOnly(values);
        Attributes = BuildAttributes(
            ("sourceContextId", SourceContextId),
            ("schemaName", SchemaName),
            ("methodName", MethodName),
            ("carrier", Carrier),
            ("lifetime", Lifetime),
            ("shapeFingerprint", ShapeFingerprint),
            ("fieldCount", Fields.Count));
    }

    public string SourceContextId { get; }

    public string SchemaName { get; }

    public string MethodName { get; }

    public string Carrier { get; }

    public string Lifetime { get; }

    public string ShapeFingerprint { get; }

    public IReadOnlyList<TargetQueryRowFieldAbiContract> Fields { get; }

    public override TargetHostAbiImportKind Kind => TargetHostAbiImportKind.QueryRowSourceAccess;

    public override IReadOnlyDictionary<string, string> Attributes { get; }

    internal override string CanonicalDefinition => CreateCanonicalDefinition(
        Kind.ToString(),
        builder =>
        {
            AppendField(builder, "sourceContextId", SourceContextId);
            AppendField(builder, "schemaName", SchemaName);
            AppendField(builder, "methodName", MethodName);
            AppendField(builder, "carrier", Carrier);
            AppendField(builder, "lifetime", Lifetime);
            AppendField(builder, "shapeFingerprint", ShapeFingerprint);
            AppendField(builder, "fields.count", Fields.Count);
            foreach (var rowField in Fields)
            {
                AppendField(builder, "fields.slot", rowField.Slot);
                AppendField(builder, "fields.sourceColumnIndex", rowField.SourceColumnIndex);
                AppendField(builder, "fields.name", rowField.Name);
                AppendType(builder, "fields.type", rowField.TypeSymbol);
                AppendType(builder, "fields.sourceReadType", rowField.SourceReadTypeSymbol);
                AppendEnumType(builder, "fields.enumType", rowField.EnumType);
                AppendField(builder, "fields.nullable", rowField.IsNullable);
                AppendDictionary(builder, "fields.readModifiers", rowField.ReadModifiers);
            }
        });
}

internal sealed record TargetQueryRowFieldAbiContract
{
    public TargetQueryRowFieldAbiContract(
        int slot,
        int sourceColumnIndex,
        string name,
        ExecutionPortableTypeDescriptor typeSymbol,
        bool isNullable,
        IReadOnlyDictionary<string, string>? readModifiers)
        : this(slot, sourceColumnIndex, name, typeSymbol, typeSymbol, null, isNullable, readModifiers)
    {
    }

    public TargetQueryRowFieldAbiContract(
        int slot,
        int sourceColumnIndex,
        string name,
        ExecutionPortableTypeDescriptor typeSymbol,
        ExecutionPortableTypeDescriptor sourceReadTypeSymbol,
        TargetEnumTypeAbiContract? enumType,
        bool isNullable,
        IReadOnlyDictionary<string, string>? readModifiers)
    {
        Slot = slot < 0
            ? throw new ArgumentOutOfRangeException(nameof(slot))
            : slot;
        SourceColumnIndex = sourceColumnIndex < 0
            ? throw new ArgumentOutOfRangeException(nameof(sourceColumnIndex))
            : sourceColumnIndex;
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(name))
            : name;
        TypeSymbol = typeSymbol ?? throw new ArgumentNullException(nameof(typeSymbol));
        SourceReadTypeSymbol = sourceReadTypeSymbol ?? throw new ArgumentNullException(nameof(sourceReadTypeSymbol));
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

    public ExecutionPortableTypeDescriptor TypeSymbol { get; }

    public ExecutionPortableTypeDescriptor SourceReadTypeSymbol { get; }

    public TargetEnumTypeAbiContract? EnumType { get; }

    public bool IsNullable { get; }

    public IReadOnlyDictionary<string, string> ReadModifiers { get; }
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

    internal override string CanonicalDefinition => CreateCanonicalDefinition(
        Kind.ToString(),
        builder =>
        {
            AppendScalar(builder, "detail", Detail);
            AppendScalar(builder, "callable", Callable);
            AppendScalar(builder, "callablePortability", CallablePortability);
            AppendScalar(builder, "methodName", MethodName);
            AppendScalar(builder, "declaringType", DeclaringType);
            AppendScalar(builder, "parameterCount", ParameterCount);
        });

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

    internal override string CanonicalDefinition => CreateCanonicalDefinition(
        Kind.ToString(),
        builder =>
        {
            AppendScalar(builder, "rowKind", RowKind);
            AppendScalar(builder, "name", Name);
            AppendScalar(builder, "typeName", TypeName);
            AppendScalar(builder, "typePortability", TypePortability);
            AppendScalar(builder, "fieldCount", FieldCount);
        });

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

    internal override string CanonicalDefinition => CreateCanonicalDefinition(
        Kind.ToString(),
        builder =>
        {
            AppendScalar(builder, "semantics", Semantics);
            AppendScalar(builder, "usesNullableValueTypes", UsesNullableValueTypes);
            AppendScalar(builder, "usesObjectNulls", UsesObjectNulls);
            AppendScalar(builder, "usesFieldNullabilityMetadata", UsesFieldNullabilityMetadata);
        });

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

    internal override string CanonicalDefinition => CreateCanonicalDefinition(
        Kind.ToString(),
        builder =>
        {
            AppendScalar(builder, "requiresCancellationToken", RequiresCancellationToken);
            AppendScalar(builder, "requiresParallelCancellation", RequiresParallelCancellation);
        });

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

    internal override string CanonicalDefinition => CreateCanonicalDefinition(
        Kind.ToString(),
        builder =>
        {
            AppendScalar(builder, "requiresBuildDiagnostics", RequiresBuildDiagnostics);
            AppendScalar(builder, "requiresSourceDiagnostics", RequiresSourceDiagnostics);
            AppendScalar(builder, "requiresRuntimeExceptionDiagnostics", RequiresRuntimeExceptionDiagnostics);
        });

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

    internal override string CanonicalDefinition => CreateCanonicalDefinition(
        Kind.ToString(),
        builder =>
        {
            AppendScalar(builder, "supportsSourceBoundaryProfiling", SupportsSourceBoundaryProfiling);
            AppendScalar(builder, "supportsOperatorProfiling", SupportsOperatorProfiling);
            AppendScalar(builder, "sourceBoundaryCount", SourceBoundaryCount);
            AppendScalar(builder, "operatorCount", OperatorCount);
        });

    public override IReadOnlyDictionary<string, string> Attributes { get; } = BuildAttributes(
        ("supportsSourceBoundaryProfiling", SupportsSourceBoundaryProfiling),
        ("supportsOperatorProfiling", SupportsOperatorProfiling),
        ("sourceBoundaryCount", RequireNonNegative(SourceBoundaryCount, nameof(SourceBoundaryCount))),
        ("operatorCount", RequireNonNegative(OperatorCount, nameof(OperatorCount))));
}
