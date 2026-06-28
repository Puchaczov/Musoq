using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Evaluator;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Helpers;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;

namespace Musoq.Converter.Tests;

[TestClass]
public class CompiledQueryArtifactApiTests
{
    private readonly TestsLoggerResolver _loggerResolver = new();

    [TestMethod]
    public void CompileArtifactWithDiagnostics_WhenQueryIsValid_ProducesArtifact()
    {
        const string query = "select i.Value from #artifact.items() i";
        var result = InstanceCreator.CompileArtifactWithDiagnostics(
            query,
            "ArtifactBasic",
            new ArtifactSchemaProvider(new ArtifactSchema("single")),
            _loggerResolver);

        Assert.IsTrue(result.Succeeded);
        Assert.IsNotNull(result.Artifact);
        Assert.IsTrue(result.Artifact.AssemblyBytes.Length > 0);
        Assert.AreEqual("ArtifactBasic.CompiledQuery", result.Artifact.RunnableTypeName);
        Assert.AreEqual(CompiledQueryArtifact.CurrentArtifactFormatVersion, result.Artifact.ArtifactFormatVersion);
        Assert.IsTrue(result.Artifact.Metadata.ContainsKey("AssemblyName"));
        Assert.IsTrue(result.Artifact.Metadata.ContainsKey("ScriptSha256"));
        Assert.IsTrue(result.Artifact.Metadata.ContainsKey("GeneratedCodeSha256"));
    }

    [TestMethod]
    public void CreateExecutableFromArtifactWithDiagnostics_WhenArtifactIsValid_RunsQuery()
    {
        const string query = "select i.Value from #artifact.items() i";
        var artifact = CompileArtifact(query, new ArtifactSchemaProvider(new ArtifactSchema("single")));

        var result = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            artifact,
            new ArtifactSchemaProvider(new ArtifactSchema("single")),
            _loggerResolver);

        Assert.IsTrue(result.Succeeded);
        Assert.IsNotNull(result.CompiledQuery);
        var table = result.CompiledQuery.Run();
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("single", table[0][0]);
    }

    [TestMethod]
    public void CreateExecutableFromArtifactWithDiagnostics_WhenProviderChanges_RebindsCurrentProvider()
    {
        const string query = "select i.Value from #artifact.items() i";
        var artifact = CompileArtifact(query, new ArtifactSchemaProvider(new ArtifactSchema("first")));

        var result = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            artifact,
            new ArtifactSchemaProvider(new ArtifactSchema("second")),
            _loggerResolver);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("second", result.CompiledQuery.Run()[0][0]);
    }

    [TestMethod]
    public void CreateExecutableFromArtifactWithDiagnostics_WhenRuntimeSettingsChange_RebindsCurrentSettingsAndPlans()
    {
        const string query = "select i.Token from #settings.items() i";
        var compileProvider = new SettingsArtifactSchemaProvider();
        var compileOptions = new CompilationOptions(sourceRuntimeSettingsResolver: new TokenSettingsResolver("compile-token"));
        var artifact = CompileArtifact(query, compileProvider, compileOptions);

        var loadProvider = new SettingsArtifactSchemaProvider();
        var loadOptions = new CompilationOptions(sourceRuntimeSettingsResolver: new TokenSettingsResolver("load-token"));
        var result = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            artifact,
            loadProvider,
            _loggerResolver,
            loadOptions);

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("load-token", result.CompiledQuery.Run()[0][0]);
        Assert.IsGreaterThanOrEqualTo(loadProvider.Schema.PlanCount, 1);
        Assert.IsGreaterThanOrEqualTo(loadProvider.Schema.DescribeRuntimeSettingsCount, 1);
    }

    [TestMethod]
    public void CreateExecutableFromArtifactWithDiagnostics_WhenCustomLoaderIsProvided_UsesLoader()
    {
        const string query = "select i.Value from #artifact.items() i";
        var artifact = CompileArtifact(query, new ArtifactSchemaProvider(new ArtifactSchema("single")));
        var invoked = false;

        var result = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            artifact,
            new ArtifactSchemaProvider(new ArtifactSchema("single")),
            _loggerResolver,
            typeLoader: loadedArtifact =>
            {
                invoked = true;
                var context = new AssemblyLoadContext($"artifact-test-{Guid.NewGuid()}", isCollectible: true);
                using var assemblyStream = new MemoryStream(loadedArtifact.AssemblyBytes);
                if (loadedArtifact.SymbolsBytes is { Length: > 0 } symbols)
                {
                    using var symbolsStream = new MemoryStream(symbols);
                    return context.LoadFromStream(assemblyStream, symbolsStream)
                        .GetType(loadedArtifact.RunnableTypeName)!;
                }

                return context.LoadFromStream(assemblyStream)
                    .GetType(loadedArtifact.RunnableTypeName)!;
            });

        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(invoked);
        Assert.AreEqual("single", result.CompiledQuery.Run()[0][0]);
    }

    [TestMethod]
    public void CreateExecutableFromArtifactWithDiagnostics_WhenOptionsMismatch_ReturnsArtifactDiagnostic()
    {
        const string query = "select i.Value from #artifact.items() i where true";
        var artifact = CompileArtifact(query, new ArtifactSchemaProvider(new ArtifactSchema("single")));

        var result = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            artifact,
            new ArtifactSchemaProvider(new ArtifactSchema("single")),
            _loggerResolver,
            new CompilationOptions(useConstantFolding: false));

        AssertArtifactFailure(result);
        Assert.IsTrue(result.Errors.Any(static diagnostic => diagnostic.Message.Contains("compilation options signature", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void CreateExecutableFromArtifactWithDiagnostics_WhenScriptMismatch_ReturnsArtifactDiagnostic()
    {
        const string query = "select i.Value from #artifact.items() i";
        var artifact = CompileArtifact(query, new ArtifactSchemaProvider(new ArtifactSchema("single")));

        var result = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            "select i.Value from #artifact.items() i where i.Value = 'single'",
            artifact,
            new ArtifactSchemaProvider(new ArtifactSchema("single")),
            _loggerResolver);

        AssertArtifactFailure(result);
        Assert.IsTrue(result.Errors.Any(static diagnostic => diagnostic.Message.Contains("ScriptSha256", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void CreateExecutableFromArtifactWithDiagnostics_WhenSchemaShapeChanges_ReturnsArtifactDiagnostic()
    {
        const string query = "select i.Value from #artifact.items() i";
        var artifact = CompileArtifact(query, new ArtifactSchemaProvider(new ArtifactSchema("single")));

        var result = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            artifact,
            new ArtifactSchemaProvider(new ArtifactIntSchema(1)),
            _loggerResolver);

        AssertArtifactFailure(result);
        Assert.IsTrue(result.Errors.Any(static diagnostic => diagnostic.Message.Contains("GeneratedCodeSha256", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void CreateExecutableFromArtifactWithDiagnostics_WhenLoaderReturnsWrongType_ReturnsArtifactDiagnostic()
    {
        const string query = "select i.Value from #artifact.items() i";
        var artifact = CompileArtifact(query, new ArtifactSchemaProvider(new ArtifactSchema("single")));

        var result = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            artifact,
            new ArtifactSchemaProvider(new ArtifactSchema("single")),
            _loggerResolver,
            typeLoader: _ => typeof(string));

        AssertArtifactFailure(result);
        Assert.IsTrue(result.Errors.Any(static diagnostic => diagnostic.Message.Contains("does not implement", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void CreateExecutableFromArtifactWithDiagnostics_WhenAssemblyBytesAreInvalid_ReturnsArtifactDiagnostic()
    {
        const string query = "select i.Value from #artifact.items() i";
        var artifact = CompileArtifact(query, new ArtifactSchemaProvider(new ArtifactSchema("single")));
        var broken = new CompiledQueryArtifact(
            [1, 2, 3, 4],
            null,
            artifact.RunnableTypeName,
            artifact.EngineVersion,
            artifact.ArtifactFormatVersion,
            artifact.CompilationOptionsSignature,
            artifact.Metadata);

        var result = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            broken,
            new ArtifactSchemaProvider(new ArtifactSchema("single")),
            _loggerResolver);

        AssertArtifactFailure(result);
        Assert.IsTrue(result.Errors.Any(static diagnostic => diagnostic.Message.Contains("type loading failed", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void CompileArtifactWithDiagnostics_WhenQueryIsInvalid_ReturnsDiagnosticsWithoutArtifact()
    {
        var result = InstanceCreator.CompileArtifactWithDiagnostics(
            "select Missing from #artifact.items() i",
            "ArtifactInvalid",
            new ArtifactSchemaProvider(new ArtifactSchema("single")),
            _loggerResolver);

        Assert.IsFalse(result.Succeeded);
        Assert.IsNull(result.Artifact);
        Assert.IsTrue(result.Errors.Count > 0);
        Assert.IsTrue(result.Errors.Any(static diagnostic => diagnostic.Code == DiagnosticCode.MQ3001_UnknownColumn));
    }

    [TestMethod]
    public void CompiledQueryArtifact_WhenConstructed_DefensivelyCopiesBytesAndMetadata()
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["key"] = "value"
        };
        var assemblyBytes = new byte[] { 1, 2, 3 };
        var symbolsBytes = new byte[] { 4, 5 };
        var artifact = new CompiledQueryArtifact(
            assemblyBytes,
            symbolsBytes,
            "Runnable",
            "Engine",
            "1",
            "Options",
            metadata);

        assemblyBytes[0] = 9;
        symbolsBytes[0] = 9;
        metadata["key"] = "changed";
        var returnedAssembly = artifact.AssemblyBytes;
        returnedAssembly[1] = 9;

        Assert.AreEqual(1, artifact.AssemblyBytes[0]);
        Assert.AreEqual(2, artifact.AssemblyBytes[1]);
        Assert.AreEqual(4, artifact.SymbolsBytes![0]);
        Assert.AreEqual("value", artifact.Metadata["key"]);
    }

    private ICompiledQueryArtifact CompileArtifact(
        string query,
        ISchemaProvider provider,
        CompilationOptions? options = null)
    {
        var result = InstanceCreator.CompileArtifactWithDiagnostics(
            query,
            "ArtifactBasic",
            provider,
            _loggerResolver,
            options);

        Assert.IsTrue(result.Succeeded, string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToDetailedString())));
        return result.Artifact;
    }

    private static void AssertArtifactFailure(BuildResult result)
    {
        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(result.Errors.Any(static diagnostic => diagnostic.Code == DiagnosticCode.MQ8002_CompiledArtifactIncompatible));
    }
}

public sealed class ArtifactSchemaProvider(ISchema schema) : ISchemaProvider
{
    public ISchema GetSchema(string schemaName)
    {
        return schema;
    }
}

public sealed class ArtifactSchema(string value) : SchemaBase("artifact", CreateLibrary())
{
    public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
    {
        return new ArtifactTable(typeof(ArtifactRow), typeof(string));
    }

    public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
    {
        return EnsureSourceType<T, ArtifactRow>(name, new ArtifactRowSource(value));
    }

    public override SchemaMethodInfo[] GetConstructors()
    {
        return TypeHelper.GetSchemaMethodInfosForType<ArtifactRowSource>("items");
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();
        return new MethodsAggregator(methodsManager);
    }
}

public sealed class ArtifactIntSchema(int value) : SchemaBase("artifact", CreateLibrary())
{
    public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
    {
        return new ArtifactTable(typeof(ArtifactIntRow), typeof(int));
    }

    public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
    {
        return EnsureSourceType<T, ArtifactIntRow>(name, new ArtifactIntRowSource(value));
    }

    public override SchemaMethodInfo[] GetConstructors()
    {
        return TypeHelper.GetSchemaMethodInfosForType<ArtifactIntRowSource>("items");
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();
        return new MethodsAggregator(methodsManager);
    }
}

public sealed class SettingsArtifactSchemaProvider : ISchemaProvider
{
    public SettingsArtifactSchema Schema { get; } = new();

    public ISchema GetSchema(string schemaName)
    {
        return Schema;
    }
}

public sealed class SettingsArtifactSchema() : SchemaBase("settings", CreateLibrary())
{
    public int DescribeRuntimeSettingsCount { get; private set; }

    public int PlanCount { get; private set; }

    public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
    {
        return new SettingsArtifactTable();
    }

    public override IReadOnlyList<SourceRuntimeSettingRequirement> DescribeSourceRuntimeSettings(
        string name,
        SourceRuntimeSettingsDescribeContext context,
        params object?[] parameters)
    {
        DescribeRuntimeSettingsCount++;
        return
        [
            new SourceRuntimeSettingRequirement(
                "TOKEN",
                Required: true,
                Secret: false,
                SourceRuntimeSettingPhase.All,
                "Token used by artifact tests.")
        ];
    }

    public override SourcePlanResult TryPlanSource(string name, SourcePlanRequest request, params object?[] parameters)
    {
        PlanCount++;
        return SourcePlanResult.RejectAll(request);
    }

    public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
    {
        executionContext.SourceRuntimeSettings.TryGetValue("TOKEN", out var token);
        return EnsureSourceType<T, SettingsArtifactRow>(name, new SettingsArtifactRowSource(token ?? "<missing>"));
    }

    public override SchemaMethodInfo[] GetConstructors()
    {
        return TypeHelper.GetSchemaMethodInfosForType<SettingsArtifactRowSource>("items");
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();
        return new MethodsAggregator(methodsManager);
    }
}

public sealed class TokenSettingsResolver(string token) : ISourceRuntimeSettingsResolver
{
    public IReadOnlyDictionary<string, string> Resolve(SourceRuntimeSettingsResolutionRequest request)
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["TOKEN"] = token
        };
    }
}

public sealed class ArtifactTable(Type rowType, Type columnType) : ISchemaTable
{
    public ISchemaColumn[] Columns => [new SchemaColumn("Value", 0, columnType)];

    public SchemaTableMetadata Metadata { get; } = new(rowType);

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(column => column.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(column => column.ColumnName == name).ToArray();
    }
}

public sealed class SettingsArtifactTable : ISchemaTable
{
    public ISchemaColumn[] Columns => [new SchemaColumn("Token", 0, typeof(string))];

    public SchemaTableMetadata Metadata { get; } = new(typeof(SettingsArtifactRow));

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(column => column.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(column => column.ColumnName == name).ToArray();
    }
}

public sealed class ArtifactRow(string value)
{
    public string Value { get; } = value;
}

public sealed class ArtifactIntRow(int value)
{
    public int Value { get; } = value;
}

public sealed class SettingsArtifactRow(string token)
{
    public string Token { get; } = token;
}

public sealed class ArtifactRowSource() : RowSourceBase<ArtifactRow>
{
    private readonly string _value = string.Empty;

    public ArtifactRowSource(string value) : this()
    {
        _value = value;
    }

    protected override void CollectChunks(IChunkWriter<ArtifactRow> writer)
    {
        writer.Write([new ArtifactRow(_value)]);
    }
}

public sealed class ArtifactIntRowSource() : RowSourceBase<ArtifactIntRow>
{
    private readonly int _value;

    public ArtifactIntRowSource(int value) : this()
    {
        _value = value;
    }

    protected override void CollectChunks(IChunkWriter<ArtifactIntRow> writer)
    {
        writer.Write([new ArtifactIntRow(_value)]);
    }
}

public sealed class SettingsArtifactRowSource() : RowSourceBase<SettingsArtifactRow>
{
    private readonly string _token = string.Empty;

    public SettingsArtifactRowSource(string token) : this()
    {
        _token = token;
    }

    protected override void CollectChunks(IChunkWriter<SettingsArtifactRow> writer)
    {
        writer.Write([new SettingsArtifactRow(_token)]);
    }
}
