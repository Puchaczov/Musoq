using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Build;
using Musoq.Evaluator.Tests.Components;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.Attributes;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;
using SchemaColumn = Musoq.Schema.DataSources.SchemaColumn;

namespace Musoq.Evaluator.Tests;

/// <summary>
/// REC-137 qualifies the supported cache boundary. Every case reuses the same
/// query text through cold, warm and changed-contract states, while test-owned
/// cache clearing keeps the observations independent of other test classes.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class DiagnosticRec137ColdWarmCacheDiagnosticsTests
{
    private const int CasesPerFamily = 12;

    private static readonly CompilationOptions DefaultOptions = new();

    [TestInitialize]
    public void Initialize()
    {
        ClearTestOwnedCaches();
    }

    [TestCleanup]
    public void Cleanup()
    {
        ClearTestOwnedCaches();
    }

    [TestMethod]
    public void ColdWarmSchemaChanges_ShouldUseCurrentSchemaForDiagnostics()
    {
        foreach (var index in Enumerable.Range(1, CasesPerFamily))
        {
            var caseId = CaseId("COLD", index);
            var query =
                $"select e.Name from #rec137.items() e /* REC-137-{caseId}-candidate */";
            var provider = new REC137VersionedSchemaProvider
            {
                SchemaSignature = $"{caseId}-name",
                IncludeName = true
            };

            var cold = Compile(query, $"REC137_{caseId}_cold", provider);
            AssertSucceeded(cold, caseId + "/cold");
            Assert.IsFalse(cold.BuildItems!.StopAfterPlanning, caseId + "/cold must compile");
            Assert.AreEqual($"{caseId}-name-name", RunSingleValue(cold, caseId + "/cold"));
            Assert.IsTrue(
                InstanceCreator.HasExecutionCompilationCacheEntryForTests(query, provider, DefaultOptions),
                caseId + "/cold did not publish an execution cache entry");

            var warm = Compile(query, $"REC137_{caseId}_warm", provider);
            AssertSucceeded(warm, caseId + "/warm");
            Assert.AreEqual($"{caseId}-name-name", RunSingleValue(warm, caseId + "/warm"));

            provider.SchemaSignature = $"{caseId}-without-name";
            provider.IncludeName = false;
            var changed = Compile(query, $"REC137_{caseId}_changed", provider);
            AssertUnknownColumn(changed, "Name", caseId + "/changed");
            Assert.IsFalse(changed.BuildItems?.StopAfterPlanning == true, caseId + "/changed reused a stale success");

            provider.SchemaSignature = $"{caseId}-name-restored";
            provider.IncludeName = true;
            var restored = Compile(query, $"REC137_{caseId}_restored", provider);
            AssertSucceeded(restored, caseId + "/restored");
            Assert.AreEqual($"{caseId}-name-restored-name", RunSingleValue(restored, caseId + "/restored"));
        }
    }

    [TestMethod]
    public void InvalidValidInvalidSequences_ShouldRecomputeAgainstCurrentSchema()
    {
        foreach (var index in Enumerable.Range(1, CasesPerFamily))
        {
            var caseId = CaseId("SEQUENCE", index);
            var query =
                $"select e.Extra from #rec137.items() e /* REC-137-{caseId}-candidate */";
            var provider = new REC137VersionedSchemaProvider
            {
                SchemaSignature = $"{caseId}-invalid-a",
                IncludeExtra = false
            };

            var invalidA = Compile(query, $"REC137_{caseId}_invalid_a", provider);
            AssertUnknownColumn(invalidA, "Extra", caseId + "/invalid-a");

            provider.SchemaSignature = $"{caseId}-valid";
            provider.IncludeExtra = true;
            var valid = Compile(query, $"REC137_{caseId}_valid", provider);
            AssertSucceeded(valid, caseId + "/valid");
            Assert.AreEqual($"{caseId}-valid-extra", RunSingleValue(valid, caseId + "/valid"));

            provider.SchemaSignature = $"{caseId}-invalid-b";
            provider.IncludeExtra = false;
            var invalidB = Compile(query, $"REC137_{caseId}_invalid_b", provider);
            AssertUnknownColumn(invalidB, "Extra", caseId + "/invalid-b");
            Assert.IsFalse(invalidB.BuildItems?.StopAfterPlanning == true, caseId + "/invalid-b reused a stale success");
        }
    }

    [TestMethod]
    public void EnumDescriptorChanges_ShouldInvalidateWarmArtifactAndRejectStaleExecution()
    {
        foreach (var index in Enumerable.Range(1, CasesPerFamily))
        {
            var caseId = CaseId("ENUM", index);
            var query =
                $"select e.Status from #rec137.items() e /* REC-137-{caseId}-candidate */";
            var provider = new REC137VersionedSchemaProvider
            {
                SchemaSignature = $"{caseId}-enum",
                EnumVersion = 1,
                IncludeEnum = true
            };

            var first = Compile(query, $"REC137_{caseId}_first", provider);
            AssertSucceeded(first, caseId + "/first");
            Assert.IsFalse(first.BuildItems!.StopAfterPlanning, caseId + "/first must compile");
            string firstFingerprint;
            using (var firstTable = first.CompiledQuery!.Run())
            {
                Assert.HasCount(1, firstTable, caseId + "/first");
                var firstDescriptor = firstTable.Columns.Single().EnumType;
                Assert.IsNotNull(firstDescriptor, caseId + "/first has no enum descriptor");
                Assert.AreEqual(1, Convert.ToInt32(firstTable[0][0]), caseId + "/first returned the wrong enum value");
                firstFingerprint = firstDescriptor.Fingerprint;
            }

            provider.EnumVersion = 2;
            provider.DriftAtExecution = true;
            var staleException = Assert.Throws<Exception>(() =>
            {
                using var table = first.CompiledQuery!.Run();
                _ = table[0];
            });
            StringAssert.Contains(staleException.ToString(), "recompile the query", caseId + "/stale");
            first.CompiledQuery!.Dispose();

            provider.DriftAtExecution = false;
            var rebuilt = Compile(query, $"REC137_{caseId}_rebuilt", provider);
            AssertSucceeded(rebuilt, caseId + "/rebuilt");
            Assert.IsFalse(rebuilt.BuildItems!.StopAfterPlanning, caseId + "/rebuilt reused the old descriptor artifact");
            var rebuiltFingerprint = RunSingleEnumFingerprint(rebuilt, caseId + "/rebuilt");
            Assert.AreNotEqual(firstFingerprint, rebuiltFingerprint, caseId + "/descriptor fingerprint did not change");
        }
    }

    [TestMethod]
    public void SourceSettingsAndMetadataChanges_ShouldNotReuseAStaleCompilation()
    {
        foreach (var index in Enumerable.Range(1, CasesPerFamily))
        {
            var caseId = CaseId("SETTINGS", index);
            var query =
                $"select e.Token from #rec137settings.items() e /* REC-137-{caseId}-candidate */";
            var provider = new REC137SettingsProvider
            {
                MetadataVersion = $"{caseId}-metadata-a"
            };
            var resolver = new REC137SettingsResolver($"{caseId}-token-a");
            var options = new CompilationOptions(sourceRuntimeSettingsResolver: resolver);

            var first = Compile(query, $"REC137_{caseId}_first", provider, options);
            AssertSucceeded(first, caseId + "/first");
            Assert.IsFalse(first.BuildItems!.StopAfterPlanning, caseId + "/first must not use an execution cache");
            Assert.AreEqual($"{caseId}-token-a", RunSingleValue(first, caseId + "/first"));

            provider.MetadataVersion = $"{caseId}-metadata-b";
            resolver.Value = $"{caseId}-token-b";
            var second = Compile(query, $"REC137_{caseId}_second", provider, options);
            AssertSucceeded(second, caseId + "/second");
            Assert.IsFalse(second.BuildItems!.StopAfterPlanning, caseId + "/second reused a settings compilation");
            Assert.AreEqual($"{caseId}-token-b", RunSingleValue(second, caseId + "/second"));
            Assert.IsGreaterThanOrEqualTo(2, provider.Schema.DescribeRuntimeSettingsCount, caseId + "/settings were not re-described");
            Assert.AreEqual($"{caseId}-metadata-b", provider.Schema.LastMetadataVersion, caseId + "/metadata identity was stale");
        }
    }

    private static BuildResult Compile(
        string query,
        string assemblyName,
        ISchemaProvider provider,
        CompilationOptions? options = null)
    {
        return InstanceCreator.CompileWithDiagnostics(
            query,
            assemblyName,
            provider,
            new TestsLoggerResolver(),
            options ?? DefaultOptions);
    }

    private static object? RunSingleValue(BuildResult result, string caseId)
    {
        AssertSucceeded(result, caseId);
        using var query = result.CompiledQuery ?? throw new AssertFailedException(caseId + " has no compiled query");
        using var table = query.Run();
        Assert.HasCount(1, table, caseId);
        Assert.HasCount(1, table.Columns, caseId);
        return table[0][0];
    }

    private static string RunSingleEnumFingerprint(BuildResult result, string caseId)
    {
        AssertSucceeded(result, caseId);
        using var query = result.CompiledQuery ?? throw new AssertFailedException(caseId + " has no compiled query");
        using var table = query.Run();
        Assert.HasCount(1, table, caseId);
        var descriptor = table.Columns.Single().EnumType;
        Assert.IsNotNull(descriptor, caseId + " has no enum descriptor");
        Assert.AreEqual(1, Convert.ToInt32(table[0][0]), caseId + " returned a stale enum value");
        return descriptor.Fingerprint;
    }

    private static void AssertSucceeded(BuildResult result, string caseId)
    {
        Assert.IsTrue(result.Succeeded, caseId + ": " + FormatDiagnostics(result));
    }

    private static void AssertUnknownColumn(BuildResult result, string column, string caseId)
    {
        Assert.IsFalse(result.Succeeded, caseId + " unexpectedly succeeded");
        var diagnostic = result.Diagnostics.Single(item => item.Code == DiagnosticCode.MQ3001_UnknownColumn);
        Assert.Contains(column, diagnostic.Message, caseId);
        Assert.IsTrue(
            diagnostic.Span.Start >= 0 && diagnostic.Span.Length > 0,
            caseId + " has no source span");
    }

    private static string FormatDiagnostics(BuildResult result)
    {
        return string.Join(
            Environment.NewLine,
            result.Diagnostics.Select(static diagnostic =>
                $"{diagnostic.Code}: {diagnostic.Message} ({diagnostic.Span.Start},{diagnostic.Span.Length})"));
    }

    private static string CaseId(string family, int index) => $"{family}-{index:00}";

    private static void ClearTestOwnedCaches()
    {
        InstanceCreator.ClearExecutionCompilationCacheForTests();
        SemanticTemplateCache.Clear();
        ParsedQueryTemplateCache.Clear();
    }
}

public sealed class REC137VersionedSchemaProvider : ISchemaProvider
{
    public string SchemaSignature { get; set; } = "default";

    public bool IncludeName { get; set; } = true;

    public bool IncludeExtra { get; set; }

    public int EnumVersion { get; set; } = 1;

    public bool IncludeEnum { get; set; }

    public bool DriftAtExecution { get; set; }

    public ISchema GetSchema(string schema)
    {
        if (!string.Equals(schema.TrimStart('#'), "rec137", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException(schema);

        return new REC137VersionedSchema(this);
    }
}

public sealed class REC137VersionedSchema(REC137VersionedSchemaProvider provider)
    : SchemaBase("rec137", new MethodsAggregator(new MethodsManager())), IQueryScopedRowSourceSchema
{
    public override ISchemaTable GetTableByName(
        string name,
        SourceMetadataContext metadataContext,
        params object?[] parameters)
    {
        if (!string.Equals(name, "items", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException(name);

        return new REC137VersionedTable(
            provider.IncludeName,
            provider.IncludeExtra,
            provider.IncludeEnum,
            provider.EnumVersion);
    }

    public override SourceDescriptor DescribeSource(
        string name,
        SourceDescribeContext context,
        params object?[] parameters)
    {
        var table = GetTableByName(name, context.MetadataContext, parameters);
        return new SourceDescriptor
        {
            Identity = context.Identity,
            RowType = table.Metadata?.TableEntityType,
            Columns = table.Columns,
            TransferCapabilities = provider.IncludeEnum
                ? SourceTransferCapabilities.QueryScopedRows |
                  SourceTransferCapabilities.LogicalScalarReads
                : SourceTransferCapabilities.None
        };
    }

    public override RowSource<T> GetRowSource<T>(
        string name,
        SourceExecutionContext executionContext,
        params object?[] parameters)
    {
        if (!string.Equals(name, "items", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException(name);

        if (provider.DriftAtExecution)
        {
            var requested = executionContext.AllColumns.SingleOrDefault(column =>
                string.Equals(column.ColumnName, "Status", StringComparison.OrdinalIgnoreCase));
            var current = REC137VersionedTable.CreateEnumDescriptor(provider.EnumVersion);
            if (requested?.EnumType is { } requestedDescriptor && !requestedDescriptor.Equals(current))
            {
                throw new InvalidOperationException(
                    "The enum descriptor changed after compilation; recompile the query.");
            }
        }

        return EnsureSourceType<T, REC137Row>(
            name,
            new REC137RowSource(provider.SchemaSignature));
    }

    public RowSource<TRow> GetQueryScopedRowSource<TRow, TMaterializer>(
        string name,
        QueryScopedRowSourceRequest request,
        params object?[] parameters)
        where TMaterializer : struct, IQueryRowMaterializer<TRow>
    {
        if (!provider.IncludeEnum || !string.Equals(name, "items", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("REC-137 query-scoped enum rows were not enabled.");

        if (provider.DriftAtExecution)
        {
            var requested = request.Shape.Fields.SingleOrDefault(field =>
                string.Equals(field.Name, nameof(REC137Row.Status), StringComparison.OrdinalIgnoreCase));
            var current = REC137VersionedTable.CreateEnumDescriptor(provider.EnumVersion);
            if (requested?.EnumType is { } requestedDescriptor && !requestedDescriptor.Equals(current))
            {
                throw new InvalidOperationException(
                    "The enum descriptor changed after compilation; recompile the query.");
            }
        }

        return new REC137QueryScopedSource<TRow, TMaterializer>(request.Shape.Fields);
    }
}

public sealed class REC137VersionedTable : ISchemaTable
{
    public REC137VersionedTable(bool includeName, bool includeExtra, bool includeEnum, int enumVersion)
    {
        var columns = new List<ISchemaColumn>();
        if (includeName)
            columns.Add(new SchemaColumn(nameof(REC137Row.Name), columns.Count, typeof(string)));
        if (includeExtra)
            columns.Add(new SchemaColumn(nameof(REC137Row.Extra), columns.Count, typeof(string)));

        columns.Add(includeEnum
            ? new SchemaColumn(
                nameof(REC137Row.Status),
                columns.Count,
                typeof(int),
                typeof(int),
                CreateEnumDescriptor(enumVersion))
            : new SchemaColumn(nameof(REC137Row.Status), columns.Count, typeof(int)));
        Columns = columns.ToArray();
    }

    public ISchemaColumn[] Columns { get; }

    public SchemaTableMetadata Metadata { get; } = new(typeof(REC137Row));

    public ISchemaColumn? GetColumnByName(string name) =>
        Columns.SingleOrDefault(column =>
            string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase));

    public ISchemaColumn[] GetColumnsByName(string name) =>
        Columns.Where(column =>
            string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase)).ToArray();

    public static EnumTypeDescriptor CreateEnumDescriptor(int version)
    {
        var memberName = version == 1 ? "First" : "Renamed";
        return new EnumTypeDescriptor(
            "REC137Status",
            EnumTypeOrigin.NativeClr,
            EnumUnderlyingKind.Int32,
            isFlags: false,
            [new EnumMemberDescriptor(memberName, EnumScalarValue.FromInt32(1))]);
    }
}

public sealed record REC137Row(string Name, string Extra, int Status);

public sealed class REC137RowSource(string signature) : RowSource<REC137Row>
{
    public override IEnumerable<IReadOnlyList<REC137Row>> Chunks =>
        [[new REC137Row($"{signature}-name", $"{signature}-extra", 1)]];
}

public sealed class REC137QueryScopedSource<TRow, TMaterializer>(
    IReadOnlyList<QueryRowField> fields) : RowSourceBase<TRow>
    where TMaterializer : struct, IQueryRowMaterializer<TRow>
{
    protected override void CollectChunks(IChunkWriter<TRow> writer)
    {
        var reader = new REC137QueryRowReader(fields);
        writer.Write([TMaterializer.Materialize<REC137QueryRowReader>(ref reader)]);
    }
}

public ref struct REC137QueryRowReader(IReadOnlyList<QueryRowField> fields) : IQuerySourceFieldReader
{
    public T Read<T>(int slot)
    {
        var field = fields[slot];
        object value = string.Equals(field.Name, nameof(REC137Row.Status), StringComparison.OrdinalIgnoreCase)
            ? 1
            : throw new InvalidOperationException($"Unexpected REC-137 field '{field.Name}'.");
        return (T)value;
    }
}

public sealed class REC137SettingsProvider : ISchemaProvider
{
    public REC137SettingsSchema Schema { get; } = new();

    public string MetadataVersion { get; set; } = "default";

    public ISchema GetSchema(string schema)
    {
        if (!string.Equals(schema.TrimStart('#'), "rec137settings", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException(schema);

        Schema.MetadataVersion = MetadataVersion;
        return Schema;
    }
}

public sealed class REC137SettingsSchema : SchemaBase
{
    public REC137SettingsSchema()
        : base("rec137settings", new MethodsAggregator(new MethodsManager()))
    {
        AddTable<REC137SettingsTable>("items");
        AddSource<REC137SettingsSource>("items");
    }

    public string MetadataVersion { get; set; } = "default";

    public string? LastMetadataVersion { get; private set; }

    public int DescribeRuntimeSettingsCount { get; private set; }

    public override ISchemaTable GetTableByName(
        string name,
        SourceMetadataContext metadataContext,
        params object?[] parameters)
    {
        LastMetadataVersion = MetadataVersion;
        return base.GetTableByName(name, metadataContext, parameters);
    }

    public override IReadOnlyList<SourceRuntimeSettingRequirement> DescribeSourceRuntimeSettings(
        string name,
        SourceRuntimeSettingsDescribeContext context,
        params object?[] parameters)
    {
        DescribeRuntimeSettingsCount++;
        return base.DescribeSourceRuntimeSettings(name, context, parameters);
    }
}

public sealed class REC137SettingsTable : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } =
    [
        new SchemaColumn(nameof(REC137SettingsRow.Token), 0, typeof(string))
    ];

    public SchemaTableMetadata Metadata { get; } = new(typeof(REC137SettingsRow));

    public ISchemaColumn? GetColumnByName(string name) =>
        Columns.SingleOrDefault(column =>
            string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase));

    public ISchemaColumn[] GetColumnsByName(string name) =>
        Columns.Where(column =>
            string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase)).ToArray();
}

public sealed record REC137SettingsRow(string Token);

public sealed class REC137SettingsSource : RowSource<REC137SettingsRow>
{
    private readonly string _token;

    [SourceRuntimeSetting("TOKEN", Required = true)]
    public REC137SettingsSource(SourceExecutionContext context)
    {
        _token = context.SourceRuntimeSettings.TryGetValue("TOKEN", out var token)
            ? token
            : string.Empty;
    }

    public override IEnumerable<IReadOnlyList<REC137SettingsRow>> Chunks =>
        [[new REC137SettingsRow(_token)]];
}

public sealed class REC137SettingsResolver(string value) : ISourceRuntimeSettingsResolver
{
    public string Value { get; set; } = value;

    public IReadOnlyDictionary<string, string> Resolve(SourceRuntimeSettingsResolutionRequest request)
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["TOKEN"] = Value
        };
    }
}
