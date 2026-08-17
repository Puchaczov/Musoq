using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests.Schema.ReadModifiers;

public sealed class ReadModifiersSchemaProvider(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
    ReadModifiersValidationMode validationMode = ReadModifiersValidationMode.None,
    IReadOnlyDictionary<string, Type>? sourceColumnKinds = null)
    : ISchemaProvider
{
    public ReadModifiersRecorder Recorder { get; } = new();

    public ISchema GetSchema(string schema)
    {
        if (!string.Equals(schema, "readmods", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(schema, "#readmods", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(schema);
        }

        return new ReadModifiersSchema(
            rows,
            validationMode,
            sourceColumnKinds ?? new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase),
            Recorder);
    }
}

[Flags]
public enum ReadModifiersValidationMode
{
    None = 0,
    LenientUnsupportedModifiers = 1,
    StrictUtf8Encoding = 2,
    ReportInfo = 4,
    ValidateSourceKinds = 8
}

internal sealed class ReadModifiersSchema(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
    ReadModifiersValidationMode validationMode,
    IReadOnlyDictionary<string, Type> sourceColumnKinds,
    ReadModifiersRecorder recorder)
    : SchemaBase("readmods", new MethodsAggregator(new MethodsManager()))
{
    public override ISchemaTable GetTableByName(
        string name,
        SourceMetadataContext metadataContext,
        params object?[] parameters)
    {
        EnsureRecords(name);
        var columns = metadataContext.AllColumns.ToArray();
        recorder.GetTableColumns.Add(columns);
        return new ReadModifiersTable(columns);
    }

    public override SourceDescriptor DescribeSource(
        string name,
        SourceDescribeContext context,
        params object?[] parameters)
    {
        EnsureRecords(name);
        var columns = context.MetadataContext.AllColumns.ToArray();
        recorder.DescriptorColumns.Add(columns);
        return new SourceDescriptor
        {
            Identity = context.Identity,
            RowType = typeof(IReadOnlyDictionary<string, object?>),
            Columns = columns,
            ContractDiagnostics = CreateDescriptorDiagnostics(columns)
        };
    }

    public override SourcePlanResult TryPlanSource(
        string name,
        SourcePlanRequest request,
        params object?[] parameters)
    {
        EnsureRecords(name);
        recorder.PlanRequests.Add(request);
        var diagnostics = CreatePlanDiagnostics(request.RequiredColumns);
        return SourcePlanResult.RejectAll(request) with { ContractDiagnostics = diagnostics };
    }

    public override RowSource<T> GetRowSource<T>(
        string name,
        SourceExecutionContext executionContext,
        params object?[] parameters)
    {
        EnsureRecords(name);
        recorder.ExecutionColumns.Add(executionContext.AllColumns.ToArray());
        var materializedRows = rows
            .Select(row => MaterializeRow(row, executionContext.AllColumns, validationMode))
            .ToArray();
        return EnsureSourceType<T, IReadOnlyDictionary<string, object?>>(
            name,
            new ReadModifiersRowSource(materializedRows));
    }

    private static IReadOnlyDictionary<string, object?> MaterializeRow(
        IReadOnlyDictionary<string, object?> row,
        IReadOnlyCollection<ISchemaColumn> columns,
        ReadModifiersValidationMode validationMode)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var column in columns)
        {
            row.TryGetValue(column.ColumnName, out var rawValue);
            result[column.ColumnName] = ReadModifiersValueConverter.Convert(rawValue, column, validationMode);
        }

        return result;
    }

    private SourceContractDiagnostic[] CreateDescriptorDiagnostics(IReadOnlyList<ISchemaColumn> columns)
    {
        var diagnostics = CreateColumnDiagnostics(columns).ToList();

        if (validationMode.HasFlag(ReadModifiersValidationMode.ValidateSourceKinds))
            diagnostics.AddRange(CreateSourceKindDiagnostics(columns));

        if (validationMode.HasFlag(ReadModifiersValidationMode.ReportInfo))
            diagnostics.Add(SourceContractDiagnostic.Info(
                "#readmods.records() inspected descriptor metadata.",
                "ReadModifiersInfo"));

        return diagnostics.ToArray();
    }

    private SourceContractDiagnostic[] CreatePlanDiagnostics(IReadOnlyList<SourceColumnRef> columns)
    {
        var diagnostics = CreateColumnDiagnostics(columns).ToList();

        if (validationMode.HasFlag(ReadModifiersValidationMode.ReportInfo))
            diagnostics.Add(SourceContractDiagnostic.Info(
                "#readmods.records() inspected plan metadata.",
                "ReadModifiersInfo"));

        return diagnostics.ToArray();
    }

    private IEnumerable<SourceContractDiagnostic> CreateColumnDiagnostics(IEnumerable<ISchemaColumn> columns)
    {
        foreach (var column in columns)
        {
            if (!column.ReadModifiers.TryGetValue(ColumnReadModifiers.Encoding, out var encoding))
                continue;

            var diagnostic = CreateEncodingDiagnostic(column.ColumnName, encoding);
            if (diagnostic != null)
                yield return diagnostic;
        }
    }

    private IEnumerable<SourceContractDiagnostic> CreateColumnDiagnostics(IEnumerable<SourceColumnRef> columns)
    {
        foreach (var column in columns)
        {
            if (!column.ReadModifiers.TryGetValue(ColumnReadModifiers.Encoding, out var encoding))
                continue;

            var diagnostic = CreateEncodingDiagnostic(column.Name, encoding);
            if (diagnostic != null)
                yield return diagnostic;
        }
    }

    private IEnumerable<SourceContractDiagnostic> CreateSourceKindDiagnostics(IEnumerable<ISchemaColumn> columns)
    {
        foreach (var column in columns)
        {
            if (!sourceColumnKinds.TryGetValue(column.ColumnName, out var sourceKind))
                continue;

            var declaredType = Nullable.GetUnderlyingType(column.ColumnType) ?? column.ColumnType;
            if (declaredType == sourceKind)
                continue;

            yield return SourceContractDiagnostic.Error(
                $"Source column '{column.ColumnName}' is {sourceKind.Name}, but the table contract declares {declaredType.Name}.",
                "ColumnKindMismatch") with
            {
                ColumnName = column.ColumnName
            };
        }
    }

    private SourceContractDiagnostic? CreateEncodingDiagnostic(string columnName, string encoding)
    {
        if (validationMode.HasFlag(ReadModifiersValidationMode.StrictUtf8Encoding) &&
            !string.Equals(encoding, "utf-8", StringComparison.OrdinalIgnoreCase))
        {
            return SourceContractDiagnostic.Error(
                $"Only utf-8 encoding is supported, but column '{columnName}' requested '{encoding}'.",
                "UnsupportedEncoding") with
            {
                ColumnName = columnName,
                ModifierKey = ColumnReadModifiers.Encoding
            };
        }

        if (validationMode.HasFlag(ReadModifiersValidationMode.LenientUnsupportedModifiers) &&
            !ReadModifiersValueConverter.IsSupportedEncoding(encoding))
        {
            return SourceContractDiagnostic.Warning(
                $"Encoding modifier '{encoding}' is ignored by #readmods.records().",
                "UnsupportedEncoding") with
            {
                ColumnName = columnName,
                ModifierKey = ColumnReadModifiers.Encoding
            };
        }

        return null;
    }

    private static void EnsureRecords(string name)
    {
        if (!string.Equals(name, "records", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException(name);
    }
}

public sealed class ReadModifiersRecorder
{
    public List<IReadOnlyCollection<ISchemaColumn>> GetTableColumns { get; } = [];

    public List<IReadOnlyCollection<ISchemaColumn>> DescriptorColumns { get; } = [];

    public List<SourcePlanRequest> PlanRequests { get; } = [];

    public List<IReadOnlyCollection<ISchemaColumn>> ExecutionColumns { get; } = [];
}

internal sealed class ReadModifiersTable(ISchemaColumn[] columns) : ISchemaTable
{
    public ISchemaColumn[] Columns => columns;

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(column =>
            string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase));
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns
            .Where(column => string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public SchemaTableMetadata Metadata { get; } = new(typeof(IReadOnlyDictionary<string, object?>));
}

internal sealed class ReadModifiersRowSource(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    : RowSourceBase<IReadOnlyDictionary<string, object?>>
{
    protected override void CollectChunks(
        IChunkWriter<IReadOnlyDictionary<string, object?>> writer)
    {
        writer.Write(rows);
    }
}

internal static class ReadModifiersValueConverter
{
    public static object? Convert(
        object? rawValue,
        ISchemaColumn column,
        ReadModifiersValidationMode validationMode)
    {
        if (rawValue == null)
            return null;

        var modifiers = column.ReadModifiers;
        var value = ApplySourceCodec(rawValue, modifiers);
        if (value is byte[] bytes)
            value = ResolveEncoding(modifiers, validationMode).GetString(bytes);

        if (value is string text)
        {
            if (modifiers.ContainsKey(ColumnReadModifiers.Trim))
                text = text.Trim();

            return ConvertText(text, column.ColumnType, modifiers);
        }

        var targetType = Nullable.GetUnderlyingType(column.ColumnType) ?? column.ColumnType;
        if (targetType.IsInstanceOfType(value))
            return value;

        return System.Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }

    private static object ApplySourceCodec(
        object rawValue,
        IReadOnlyDictionary<string, string> modifiers)
    {
        if (!modifiers.TryGetValue($"{ColumnReadModifiers.SourcePrefix}codec", out var codec))
            return rawValue;

        if (!string.Equals(codec, "base64", StringComparison.OrdinalIgnoreCase))
            return rawValue;

        var base64 = rawValue switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            string text => text,
            _ => rawValue.ToString() ?? string.Empty
        };

        return System.Convert.FromBase64String(base64);
    }

    private static object ConvertText(
        string text,
        Type columnType,
        IReadOnlyDictionary<string, string> modifiers)
    {
        var targetType = Nullable.GetUnderlyingType(columnType) ?? columnType;
        var culture = ResolveCulture(modifiers);

        if (targetType == typeof(string) || targetType == typeof(object))
            return text;

        if (targetType == typeof(decimal))
            return decimal.Parse(text, NumberStyles.Number, culture);

        if (targetType == typeof(DateTime))
            return ParseDateTime(text, modifiers, culture);

        if (targetType == typeof(DateTimeOffset))
            return ParseDateTimeOffset(text, modifiers, culture);

        return System.Convert.ChangeType(text, targetType, culture);
    }

    private static DateTime ParseDateTime(
        string text,
        IReadOnlyDictionary<string, string> modifiers,
        CultureInfo culture)
    {
        return modifiers.TryGetValue(ColumnReadModifiers.Format, out var format)
            ? DateTime.ParseExact(text, format, culture)
            : DateTime.Parse(text, culture);
    }

    private static DateTimeOffset ParseDateTimeOffset(
        string text,
        IReadOnlyDictionary<string, string> modifiers,
        CultureInfo culture)
    {
        return modifiers.TryGetValue(ColumnReadModifiers.Format, out var format)
            ? DateTimeOffset.ParseExact(text, format, culture)
            : DateTimeOffset.Parse(text, culture);
    }

    private static CultureInfo ResolveCulture(IReadOnlyDictionary<string, string> modifiers)
    {
        return modifiers.TryGetValue(ColumnReadModifiers.Culture, out var culture)
            ? CultureInfo.GetCultureInfo(culture)
            : CultureInfo.InvariantCulture;
    }

    public static bool IsSupportedEncoding(string encoding)
    {
        return string.Equals(encoding, "utf-8", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(encoding, "utf-16le", StringComparison.OrdinalIgnoreCase);
    }

    private static Encoding ResolveEncoding(
        IReadOnlyDictionary<string, string> modifiers,
        ReadModifiersValidationMode validationMode)
    {
        var encoding = modifiers.TryGetValue(ColumnReadModifiers.Encoding, out var requested)
            ? requested
            : "utf-8";

        if (string.Equals(encoding, "utf-8", StringComparison.OrdinalIgnoreCase))
            return Encoding.UTF8;

        if (string.Equals(encoding, "utf-16le", StringComparison.OrdinalIgnoreCase))
            return Encoding.Unicode;

        if (validationMode.HasFlag(ReadModifiersValidationMode.LenientUnsupportedModifiers))
            return Encoding.UTF8;

        throw new NotSupportedException($"Encoding '{encoding}' is not supported by #readmods.records().");
    }
}
