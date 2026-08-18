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
        AddSource<CsvFileSource>(File);
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
            ? new CsvFileTable(columns, typeof(IReadOnlyDictionary<string, object?>))
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
        if (_recorder?.DescribeSourceContractDiagnostics.Count > 0)
            diagnostics = diagnostics.Concat(_recorder.DescribeSourceContractDiagnostics).ToArray();

        return new SourceDescriptor
        {
            Identity = context.Identity,
            RowType = dynamicMetadata
                ? typeof(IReadOnlyDictionary<string, object?>)
                : typeof(CsvRow),
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
        return _recorder?.PlanContractDiagnostics.Count > 0
            ? result with { ContractDiagnostics = result.ContractDiagnostics.Concat(_recorder.PlanContractDiagnostics).ToArray() }
            : result;
    }

    public override RowSource<T> GetRowSource<T>(
        string name,
        SourceExecutionContext executionContext,
        params object?[] parameters)
    {
        EnsureFile(name);
        ArgumentNullException.ThrowIfNull(executionContext);

        _recorder?.RowSourceCalls.Add(new CsvRowSourceCall(
            name,
            CsvSourceExecutionSnapshot.From(executionContext),
            parameters.ToArray(),
            typeof(T)));

        return EnsureSourceType<T, CsvRow>(
            name,
            CreateFileSource(executionContext, parameters));
    }

    public RowSource<TRow> GetQueryScopedRowSource<TRow, TMaterializer>(
        string name,
        QueryScopedRowSourceRequest request,
        params object?[] parameters)
        where TMaterializer : struct, IQueryRowMaterializer<TRow>
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
            parameters.ToArray(),
            typeof(TRow),
            request.Shape.Fingerprint));

        return new CsvQueryRowSource<TRow, TMaterializer>(
            CsvSourceOptions.FromParameters(parameters),
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

    private static CsvFileSource CreateFileSource(
        SourceExecutionContext executionContext,
        object?[] parameters)
    {
        return parameters.Length switch
        {
            0 => new CsvFileSource(executionContext),
            1 => new CsvFileSource(RequireString(parameters[0], "path"), executionContext),
            2 => new CsvFileSource(
                RequireString(parameters[0], "path"),
                RequireBoolean(parameters[1], "hasHeader"),
                executionContext),
            3 => new CsvFileSource(
                RequireString(parameters[0], "path"),
                RequireBoolean(parameters[1], "hasHeader"),
                RequireInt32(parameters[2], "skipRows"),
                executionContext),
            4 => new CsvFileSource(
                RequireString(parameters[0], "path"),
                RequireBoolean(parameters[1], "hasHeader"),
                RequireInt32(parameters[2], "skipRows"),
                RequireString(parameters[3], "delimiter"),
                executionContext),
            _ => throw new ArgumentException("CSV file source accepts at most four parameters.")
        };
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
