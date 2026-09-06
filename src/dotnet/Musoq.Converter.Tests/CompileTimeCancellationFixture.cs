using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Musoq.Evaluator;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;
using Musoq.Schema.Reflection;

namespace Musoq.Converter.Tests;

internal enum CooperativeCompileTimeStage
{
    None,
    RawConstructors,
    TableLookup,
    AliasedLookup,
    SourceDescription,
    RuntimeSettingsDescription,
    RuntimeSettingsResolution,
    SourcePlanning
}

internal sealed class CooperativeCompileTimeFixture : IDisposable
{
    private readonly TaskCompletionSource<CooperativeCompileTimeStage> _entered =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> _release =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly ConcurrentQueue<CancellationToken> _metadataTokens = [];
    private int _stageWaited;
    private int _disposed;

    public CooperativeCompileTimeFixture(CooperativeCompileTimeStage stage)
    {
        Stage = stage;
        Schema = new CooperativeSchema(this);
        Provider = new CooperativeSchemaProvider(Schema);
        Resolver = new CooperativeRuntimeSettingsResolver(this);
    }

    public CooperativeCompileTimeStage Stage { get; }

    public CooperativeSchemaProvider Provider { get; }

    public CooperativeSchema Schema { get; }

    public CooperativeRuntimeSettingsResolver Resolver { get; }

    public Task<CooperativeCompileTimeStage> Entered => _entered.Task;

    public IReadOnlyList<CancellationToken> MetadataTokens => _metadataTokens.ToArray();

    public CancellationToken? RuntimeSettingsToken { get; private set; }

    public CancellationToken? PlanningToken { get; private set; }

    public int GetSchemaCount => Provider.GetSchemaCount;

    public bool StageCompleted { get; private set; }

    public CompilationOptions CreateCompilationOptions()
    {
        return new CompilationOptions(sourceRuntimeSettingsResolver: Resolver);
    }

    public void CaptureMetadataToken(CancellationToken token)
    {
        _metadataTokens.Enqueue(token);
    }

    public void CaptureRuntimeSettingsToken(CancellationToken token)
    {
        RuntimeSettingsToken = token;
    }

    public void CapturePlanningToken(CancellationToken token)
    {
        PlanningToken = token;
    }

    public void WaitAt(CooperativeCompileTimeStage stage, CancellationToken token)
    {
        if (Stage != stage || Interlocked.Exchange(ref _stageWaited, 1) != 0)
            return;

        _entered.TrySetResult(stage);
        try
        {
            _release.Task.WaitAsync(token).GetAwaiter().GetResult();
            StageCompleted = true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
    }

    public void Release()
    {
        _release.TrySetResult(true);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        Release();
    }
}

internal sealed class CooperativeSchemaProvider(CooperativeSchema schemaDefinition) : ISchemaProvider
{
    public int GetSchemaCount { get; private set; }

    public ISchema GetSchema(string schema)
    {
        GetSchemaCount++;
        if (!string.Equals(schema, "cooperative", System.StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(schema, "#cooperative", System.StringComparison.OrdinalIgnoreCase))
        {
            throw new System.NotSupportedException(schema);
        }

        return schemaDefinition;
    }
}

internal sealed class CooperativeRuntimeSettingsResolver(CooperativeCompileTimeFixture fixture)
    : ISourceRuntimeSettingsResolver
{
    public IReadOnlyDictionary<string, string> Resolve(SourceRuntimeSettingsResolutionRequest request)
    {
        fixture.CaptureRuntimeSettingsToken(request.CancellationToken);
        fixture.WaitAt(CooperativeCompileTimeStage.RuntimeSettingsResolution, request.CancellationToken);
        return new Dictionary<string, string>(System.StringComparer.Ordinal)
        {
            ["TOKEN"] = "cooperative"
        };
    }
}

internal sealed class CooperativeSchema : SchemaBase
{
    public CooperativeSchema(CooperativeCompileTimeFixture fixture)
        : base("cooperative", CreateLibrary())
    {
        Fixture = fixture;
        AddTable<CooperativeTable>("items");
        AddSource<CooperativeRowSource>("items");
    }

    public CooperativeCompileTimeFixture Fixture { get; }

    public override SchemaMethodInfo[] GetRawConstructors(SourceMetadataContext metadataContext)
    {
        Fixture.CaptureMetadataToken(metadataContext.EndWorkToken);
        Fixture.WaitAt(CooperativeCompileTimeStage.RawConstructors, metadataContext.EndWorkToken);
        return base.GetRawConstructors(metadataContext);
    }

    public override SchemaMethodInfo[] GetRawConstructors(
        string methodName,
        SourceMetadataContext metadataContext)
    {
        Fixture.CaptureMetadataToken(metadataContext.EndWorkToken);
        var stage = Fixture.Stage == CooperativeCompileTimeStage.AliasedLookup
            ? CooperativeCompileTimeStage.AliasedLookup
            : CooperativeCompileTimeStage.RawConstructors;
        Fixture.WaitAt(stage, metadataContext.EndWorkToken);
        return base.GetRawConstructors(methodName, metadataContext);
    }

    public override ISchemaTable GetTableByName(
        string name,
        SourceMetadataContext metadataContext,
        params object?[] parameters)
    {
        Fixture.CaptureMetadataToken(metadataContext.EndWorkToken);
        Fixture.WaitAt(CooperativeCompileTimeStage.TableLookup, metadataContext.EndWorkToken);
        return base.GetTableByName(name, metadataContext, parameters);
    }

    public override SourceDescriptor DescribeSource(
        string name,
        SourceDescribeContext context,
        params object?[] parameters)
    {
        Fixture.CaptureMetadataToken(context.MetadataContext.EndWorkToken);
        Fixture.WaitAt(CooperativeCompileTimeStage.SourceDescription, context.MetadataContext.EndWorkToken);
        return base.DescribeSource(name, context, parameters);
    }

    public override IReadOnlyList<SourceRuntimeSettingRequirement> DescribeSourceRuntimeSettings(
        string name,
        SourceRuntimeSettingsDescribeContext context,
        params object?[] parameters)
    {
        Fixture.CaptureMetadataToken(context.MetadataContext.EndWorkToken);
        Fixture.WaitAt(
            CooperativeCompileTimeStage.RuntimeSettingsDescription,
            context.MetadataContext.EndWorkToken);
        return
        [
            new SourceRuntimeSettingRequirement(
                "TOKEN",
                Required: true,
                Secret: false,
                SourceRuntimeSettingPhase.All,
                "Cooperative test setting.")
        ];
    }

    public override SourcePlanResult TryPlanSource(
        string name,
        SourcePlanRequest request,
        params object?[] parameters)
    {
        Fixture.CapturePlanningToken(request.CancellationToken);
        Fixture.WaitAt(CooperativeCompileTimeStage.SourcePlanning, request.CancellationToken);
        return SourcePlanResult.RejectAll(request);
    }

    public override RowSource<T> GetRowSource<T>(
        string name,
        SourceExecutionContext executionContext,
        params object?[] parameters)
    {
        return EnsureSourceType<T, CooperativeRow>(name, new CooperativeRowSource());
    }

    private static MethodsAggregator CreateLibrary()
    {
        return new MethodsAggregator(new MethodsManager());
    }
}

internal sealed class CooperativeTable : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } =
    [
        new SchemaColumn("Value", 0, typeof(string))
    ];

    public SchemaTableMetadata Metadata { get; } = new(typeof(CooperativeRow));

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(column => column.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(column => column.ColumnName == name).ToArray();
    }
}

public sealed class CooperativeRow
{
    public string Value { get; init; } = "cooperative";
}

public sealed class CooperativeRowSource : RowSourceBase<CooperativeRow>
{
    protected override void CollectChunks(IChunkWriter<CooperativeRow> writer)
    {
        writer.Write([new CooperativeRow()]);
    }
}
