using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Examples.DataSources.Csv.Tests;

public abstract class CsvExampleTestBase
{
    protected static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);
    protected static readonly ILoggerResolver LoggerResolver = new NullLoggerResolver();

    protected static CompiledQuery Compile(
        string query,
        CsvSchemaProvider? provider = null,
        CompilationOptions? options = null)
    {
        return InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            provider ?? new CsvSchemaProvider(),
            LoggerResolver,
            options ?? TestCompilationOptions);
    }

    protected static Table Run(
        string query,
        CsvSchemaProvider? provider = null,
        CompilationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return Compile(query, provider, options).Run(cancellationToken);
    }

    protected static BuildResult CompileWithDiagnostics(
        string query,
        CsvSchemaProvider? provider = null,
        CompilationOptions? options = null)
    {
        return InstanceCreator.CompileWithDiagnostics(
            query,
            Guid.NewGuid().ToString(),
            provider ?? new CsvSchemaProvider(),
            LoggerResolver,
            options ?? TestCompilationOptions);
    }

    protected static QueryInspectionResult Inspect(
        string query,
        CsvSchemaProvider? provider = null,
        CompilationOptions? options = null)
    {
        return InstanceCreator.CompileForInspection(
            query,
            Guid.NewGuid().ToString(),
            provider ?? new CsvSchemaProvider(),
            LoggerResolver,
            options ?? TestCompilationOptions);
    }

    protected static CompilationOptions CreateOptionsWithRuntimeSettings(
        IReadOnlyDictionary<string, string> settings,
        ISourceRuntimeSettingsResolver? resolver = null)
    {
        return new CompilationOptions(
            usePrimitiveTypeValidation: false,
            sourceRuntimeSettingsResolver: resolver ?? new StaticSettingsResolver(settings));
    }

    protected static string WriteTempCsv(string content, Encoding? encoding = null)
    {
        var directory = Path.Combine(Path.GetTempPath(), "MusoqCsvTests");
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, $"{Guid.NewGuid():N}.csv");
        File.WriteAllText(path, content, encoding ?? new UTF8Encoding(false));
        return path;
    }

    protected static string SqlString(string value)
    {
        return $"'{value.Replace('\\', '/').Replace("'", "''")}'";
    }

    protected static SourceMetadataContext CreateMetadataContext(
        IReadOnlyCollection<ISchemaColumn>? columns = null,
        string queryId = "query",
        IReadOnlyDictionary<string, string>? sourceRuntimeSettings = null,
        CancellationToken cancellationToken = default)
    {
        return new SourceMetadataContext(
            queryId,
            cancellationToken,
            columns ?? [],
            sourceRuntimeSettings ?? new Dictionary<string, string>(),
            NullLogger.Instance);
    }

    protected static SourceExecutionContext CreateExecutionContext(
        SourceExecutionPlan? plan = null,
        IReadOnlyCollection<ISchemaColumn>? columns = null,
        IReadOnlyDictionary<string, string>? sourceRuntimeSettings = null,
        CancellationToken cancellationToken = default)
    {
        return new SourceExecutionContext(
            "execution-query",
            plan ?? SourceExecutionPlan.Empty(SourceIdentity.Empty),
            cancellationToken,
            columns ?? [],
            sourceRuntimeSettings ?? new Dictionary<string, string>(),
            NullLogger.Instance);
    }

    protected static Musoq.Schema.DataSources.SchemaColumn Column(
        string name,
        int index,
        Type type,
        IReadOnlyDictionary<string, string>? readModifiers = null)
    {
        return new Musoq.Schema.DataSources.SchemaColumn(name, index, type, readModifiers);
    }

    private sealed class NullLoggerResolver : ILoggerResolver
    {
        public ILogger ResolveLogger()
        {
            return NullLogger.Instance;
        }

        public ILogger<T> ResolveLogger<T>()
        {
            return NullLogger<T>.Instance;
        }
    }

    protected sealed class StaticSettingsResolver(IReadOnlyDictionary<string, string> settings)
        : ISourceRuntimeSettingsResolver
    {
        public List<SourceRuntimeSettingsResolutionRequest> Requests { get; } = [];

        public IReadOnlyDictionary<string, string> Resolve(SourceRuntimeSettingsResolutionRequest request)
        {
            Requests.Add(request);
            return settings;
        }
    }
}
