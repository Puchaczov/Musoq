using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;
using Musoq.Schema.Reflection;

namespace Musoq.Examples.DataSources.Csv;

public sealed class CsvSchema : SchemaBase, IQueryScopedRowSourceSchema
{
    public const string SchemaName = "csv";
    public const string File = "file";

    private readonly CsvDataSourceApiRecorder? _recorder;
    private readonly bool _enableQueryScopedRows;
    private readonly HashSet<string> _dynamicMetadataQueryIds = new(StringComparer.Ordinal);

    public CsvSchema()
        : this(null, true)
    {
    }

    internal CsvSchema(CsvDataSourceApiRecorder? recorder, bool enableQueryScopedRows = true)
        : base(SchemaName, CreateLibrary())
    {
        _recorder = recorder;
        _enableQueryScopedRows = enableQueryScopedRows;

        AddTable<CsvFileTable>(File);
        AddSource<CsvFileDeclaration>(File);
    }

    public override ISchemaTable GetTableByName(
        string name,
        SourceMetadataContext metadataContext,
        params object?[] parameters)
    {
        EnsureFile(name);
        ArgumentNullException.ThrowIfNull(metadataContext);

        var dynamicColumns = DiscoverDynamicColumns(metadataContext.AllColumns, parameters);
        var columns = dynamicColumns.Length > 0
            ? dynamicColumns
            : metadataContext.AllColumns.ToArray();
        if (dynamicColumns.Length > 0)
            _dynamicMetadataQueryIds.Add(metadataContext.QueryId);
        _recorder?.GetTableCalls.Add(new CsvGetTableCall(
            name,
            CsvSourceMetadataSnapshot.From(metadataContext),
            parameters.ToArray(),
            columns));

        return dynamicColumns.Length > 0
            ? new CsvFileTable(columns, typeof(QueryScopedRowContract))
            : new CsvFileTable(columns);
    }

    public override SourceDescriptor DescribeSource(
        string name,
        SourceDescribeContext context,
        params object?[] parameters)
    {
        EnsureFile(name);
        ArgumentNullException.ThrowIfNull(context);

        var dynamicColumns = DiscoverDynamicColumns(context.MetadataContext.AllColumns, parameters);
        var dynamicMetadata = dynamicColumns.Length > 0 ||
            (_enableQueryScopedRows && _dynamicMetadataQueryIds.Contains(context.MetadataContext.QueryId));
        var columns = dynamicMetadata
            ? (dynamicColumns.Length > 0 ? dynamicColumns : CsvDynamicMetadata.Discover(parameters))
            : context.MetadataContext.AllColumns.ToArray();
        if (dynamicMetadata && columns.Length == 0)
            columns = CsvDynamicMetadata.Discover(parameters);
        if (dynamicMetadata && columns.Length > 0)
            _dynamicMetadataQueryIds.Add(context.MetadataContext.QueryId);
        _recorder?.DescribeSourceCalls.Add(new CsvDescribeSourceCall(
            name,
            context.Identity,
            CsvSourceMetadataSnapshot.From(context.MetadataContext),
            parameters.ToArray(),
            columns));

        var diagnostics = CsvContractDiagnostics.Describe(columns, parameters);
        if (!_enableQueryScopedRows)
            diagnostics = diagnostics.Append(SourceContractDiagnostic.Error(
                "CSV sources require a native typed positional accessor through query-scoped rows.", "CsvNativeTransferRequired")).ToArray();
        if (_recorder?.DescribeSourceContractDiagnostics.Count > 0)
            diagnostics = diagnostics.Concat(_recorder.DescribeSourceContractDiagnostics).ToArray();

        return new SourceDescriptor
        {
            Identity = context.Identity,
            RowType = typeof(QueryScopedRowContract),
            Columns = columns,
            ContractDiagnostics = diagnostics,
            TransferCapabilities = _enableQueryScopedRows
                ? SourceTransferCapabilities.QueryScopedRows
                : SourceTransferCapabilities.None
        };
    }

    public override IReadOnlyList<SourceRuntimeSettingRequirement> DescribeSourceRuntimeSettings(
        string name,
        SourceRuntimeSettingsDescribeContext context,
        params object?[] parameters)
    {
        EnsureFile(name);
        ArgumentNullException.ThrowIfNull(context);

        _recorder?.RuntimeSettingsCalls.Add(new CsvRuntimeSettingsCall(
            name,
            context.Identity,
            CsvSourceMetadataSnapshot.From(context.MetadataContext),
            parameters.ToArray()));

        return _recorder?.RuntimeSettingRequirements.ToArray() ?? [];
    }

    public override SourcePlanResult TryPlanSource(
        string name,
        SourcePlanRequest request,
        params object?[] parameters)
    {
        EnsureFile(name);
        ArgumentNullException.ThrowIfNull(request);

        _recorder?.PlanCalls.Add(new CsvPlanCall(name, request, parameters.ToArray()));
        var result = _recorder?.PlanResultFactory?.Invoke(request) ?? CsvSourcePlan.Create(request);
        if (_recorder?.PlanContractDiagnostics.Count > 0)
            result = result with { ContractDiagnostics = result.ContractDiagnostics.Concat(_recorder.PlanContractDiagnostics).ToArray() };
        if (result.ExecutionPlan is null)
            return result;
        try
        {
            var properties = result.ExecutionPlan.Properties.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
            properties[CsvNativeExecutionPlan.PropertyName] = CsvNativeExecutionPlan.Bind(result);
            return result with { ExecutionPlan = result.ExecutionPlan with { Properties = properties } };
        }
        catch (InvalidOperationException exception)
        {
            return result with
            {
                ContractDiagnostics = result.ContractDiagnostics.Append(SourceContractDiagnostic.Error(exception.Message, "CsvNativeBinding")).ToArray()
            };
        }
    }

    public override RowSource<T> GetRowSource<T, TArguments>(
        string name,
        SourceExecutionContext executionContext,
        TArguments parameters)
    {
        EnsureFile(name);
        ArgumentNullException.ThrowIfNull(executionContext);

        _recorder?.RowSourceCalls.Add(new CsvRowSourceCall(
            name,
            CsvSourceExecutionSnapshot.From(executionContext),
            CsvSourceOptions.FromNative(parameters),
            typeof(T)));

        throw new InvalidOperationException("CSV sources require native query-scoped rows.");
    }

    public RowSource<TRow> GetQueryScopedRowSource<TRow, TMaterializer, TArguments>(
        string name,
        QueryScopedRowSourceRequest request,
        TArguments parameters)
        where TMaterializer : struct, IQueryRowMaterializer<TRow>
        where TArguments : struct, global::Musoq.Schema.ISourceArguments
    {
        EnsureFile(name);
        ArgumentNullException.ThrowIfNull(request);
        if (!_enableQueryScopedRows)
        {
            throw new InvalidOperationException(
                "CSV query-scoped rows are disabled for this schema instance.");
        }

        _recorder?.QueryRowSourceCalls.Add(new CsvQueryRowSourceCall(
            name,
            CsvSourceExecutionSnapshot.From(request.ExecutionContext),
            CsvSourceOptions.FromNative(parameters),
            typeof(TRow),
            request.Shape.Fingerprint));

        return new CsvQueryRowSource<TRow, TMaterializer>(
            CsvSourceOptions.FromNative(parameters),
            request);
    }

    public override SchemaMethodInfo[] GetRawConstructors(SourceMetadataContext metadataContext)
    {
        ArgumentNullException.ThrowIfNull(metadataContext);
        _recorder?.RawConstructorCalls.Add(new CsvRawConstructorCall(null, CsvSourceMetadataSnapshot.From(metadataContext)));
        return base.GetRawConstructors(metadataContext);
    }

    public override SchemaMethodInfo[] GetRawConstructors(string methodName, SourceMetadataContext metadataContext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);
        ArgumentNullException.ThrowIfNull(metadataContext);
        _recorder?.RawConstructorCalls.Add(new CsvRawConstructorCall(methodName, CsvSourceMetadataSnapshot.From(metadataContext)));
        return base.GetRawConstructors(metadataContext)
            .Where(constructor => string.Equals(constructor.MethodName, methodName, StringComparison.Ordinal))
            .ToArray();
    }

    private static void EnsureFile(string name)
    {
        if (!string.Equals(name, File, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"CSV example schema does not expose source '{name}'.");
    }

    private ISchemaColumn[] DiscoverDynamicColumns(
        IReadOnlyCollection<ISchemaColumn> columns,
        IReadOnlyList<object?> parameters)
    {
        if (!_enableQueryScopedRows)
            return [];

        if (columns.Count == 0)
            return CsvDynamicMetadata.Discover(parameters);

        if (!columns.Any(static column => column.ColumnType == typeof(object)))
            return [];

        var discovered = CsvDynamicMetadata.Discover(parameters);
        if (discovered.Length == 0)
            return [];

        var discoveredNames = discovered
            .Select(static column => column.ColumnName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return columns.All(column => discoveredNames.Contains(column.ColumnName))
            ? discovered
            : [];
    }

    private static string RequireString(object? value, string parameterName)
    {
        if (value is string text)
            return text;

        throw new ArgumentException($"CSV file source parameter '{parameterName}' must be a string.");
    }

    private static bool RequireBoolean(object? value, string parameterName)
    {
        if (value is bool flag)
            return flag;

        throw new ArgumentException($"CSV file source parameter '{parameterName}' must be a boolean.");
    }

    private static int RequireInt32(object? value, string parameterName)
    {
        return value switch
        {
            int number => number,
            long number when number is >= int.MinValue and <= int.MaxValue => (int)number,
            _ => throw new ArgumentException($"CSV file source parameter '{parameterName}' must be a 32-bit integer.")
        };
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();
        methodsManager.RegisterLibraries(new LibraryBase());
        return new MethodsAggregator(methodsManager);
    }
}
