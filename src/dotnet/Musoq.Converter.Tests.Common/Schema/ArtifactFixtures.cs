using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Helpers;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;

namespace Musoq.Converter.Tests;

public sealed class ArtifactSchemaProvider(ISchema schema) : ISchemaProvider
{
    public ISchema GetSchema(string schemaName)
    {
        return schema;
    }
}

public sealed class TestLifetimeOwner : IDisposable
{
    public bool Disposed { get; private set; }

    public void Dispose()
    {
        Disposed = true;
    }
}

public sealed class TestCompiledQueryArtifact(
    byte[] assemblyBytes,
    byte[]? symbolsBytes,
    string runnableTypeName,
    string engineVersion,
    string artifactFormatVersion,
    string compilationOptionsSignature,
    IReadOnlyDictionary<string, string> metadata)
    : ICompiledQueryArtifact
{
    public byte[] AssemblyBytes { get; } = assemblyBytes;

    public byte[]? SymbolsBytes { get; } = symbolsBytes;

    public string RunnableTypeName { get; } = runnableTypeName;

    public string EngineVersion { get; } = engineVersion;

    public string ArtifactFormatVersion { get; } = artifactFormatVersion;

    public string CompilationOptionsSignature { get; } = compilationOptionsSignature;

    public IReadOnlyDictionary<string, string> Metadata { get; } = metadata;
}

public sealed class ArtifactSchema(string value) : SchemaBase("artifact", CreateLibrary())
{
    public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
    {
        return new ArtifactTable(typeof(ArtifactRow), typeof(string));
    }

    public override RowSource<T> GetRowSource<T, TArguments>(string name, SourceExecutionContext executionContext, TArguments nativeArguments)
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

public sealed class ArtifactReadModifierSchema(string modifierValue) : SchemaBase("artifact", CreateLibrary())
{
    public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
    {
        return new ArtifactReadModifierTable(modifierValue);
    }

    public override RowSource<T> GetRowSource<T, TArguments>(string name, SourceExecutionContext executionContext, TArguments nativeArguments)
    {
        return EnsureSourceType<T, ArtifactRow>(name, new ArtifactRowSource("single"));
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

public sealed class ArtifactIntendedTypeSchema(string intendedTypeName) : SchemaBase("artifact", CreateLibrary())
{
    public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
    {
        return new ArtifactIntendedTypeTable(intendedTypeName);
    }

    public override RowSource<T> GetRowSource<T, TArguments>(string name, SourceExecutionContext executionContext, TArguments nativeArguments)
    {
        return EnsureSourceType<T, ArtifactRow>(name, new ArtifactRowSource("single"));
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

    public override RowSource<T> GetRowSource<T, TArguments>(string name, SourceExecutionContext executionContext, TArguments nativeArguments)
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

    public override RowSource<T> GetRowSource<T, TArguments>(string name, SourceExecutionContext executionContext, TArguments nativeArguments)
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

public sealed class ArtifactReadModifierTable(string modifierValue) : ISchemaTable
{
    public ISchemaColumn[] Columns =>
    [
        new SchemaColumn(
            "Value",
            0,
            typeof(string),
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["encoding"] = modifierValue
            })
    ];

    public SchemaTableMetadata Metadata { get; } = new(typeof(ArtifactRow));

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(column => column.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(column => column.ColumnName == name).ToArray();
    }
}

public sealed class ArtifactIntendedTypeTable(string intendedTypeName) : ISchemaTable
{
    public ISchemaColumn[] Columns => [new SchemaColumn("Value", 0, typeof(string), intendedTypeName)];

    public SchemaTableMetadata Metadata { get; } = new(typeof(ArtifactRow));

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

public sealed class ArtifactObjectRow(object value)
{
    public object Value { get; } = value;
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

public sealed class ArtifactObjectRowSource() : RowSourceBase<ArtifactObjectRow>
{
    private readonly object _value = string.Empty;

    public ArtifactObjectRowSource(object value) : this()
    {
        _value = value;
    }

    protected override void CollectChunks(IChunkWriter<ArtifactObjectRow> writer)
    {
        writer.Write([new ArtifactObjectRow(_value)]);
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
