using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Exceptions;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;
using Musoq.Parser.Diagnostics;
using Musoq.Evaluator.Tests.Schema.RuntimeDynamic;
using SchemaConstructorInfo = Musoq.Schema.Reflection.ConstructorInfo;
using RuntimeSchemaColumn = Musoq.Schema.DataSources.SchemaColumn;

namespace Musoq.Evaluator.Tests.Schema.RuntimeDynamic
{

[DynamicObjectPropertyDefaultTypeHint(typeof(double))]
[DynamicObjectPropertyTypeHint("Raw", typeof(ulong))]
public sealed class RuntimeDynamicBranch : DynamicObject
{
    private readonly IReadOnlyDictionary<string, object?> _values;
    private readonly ConcurrentDictionary<string, int> _accessCounts = new(StringComparer.Ordinal);

    public RuntimeDynamicBranch(IReadOnlyDictionary<string, object?> values)
    {
        _values = values;
    }

    public int GetAccessCount(string name) =>
        _accessCounts.TryGetValue(name, out var count) ? count : 0;

    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        _accessCounts.AddOrUpdate(binder.Name, 1, static (_, count) => count + 1);
        if (_values.TryGetValue(binder.Name, out result))
            return true;

        result = null;
        return false;
    }
}

public sealed class RuntimeDynamicRow : DynamicObject
{
    private readonly IReadOnlyDictionary<string, object?> _values;
    private readonly ConcurrentDictionary<string, int> _accessCounts = new(StringComparer.Ordinal);

    public RuntimeDynamicRow(
        string label,
        IReadOnlyDictionary<string, object?> values,
        RuntimeDynamicBranch? staticBranch = null)
    {
        Label = label;
        StaticBranch = staticBranch;
        _values = values;
    }

    public string Label { get; }

    public RuntimeDynamicBranch? StaticBranch { get; }

    public int GetAccessCount(string name) =>
        _accessCounts.TryGetValue(name, out var count) ? count : 0;

    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        _accessCounts.AddOrUpdate(binder.Name, 1, static (_, count) => count + 1);
        if (_values.TryGetValue(binder.Name, out result))
            return true;

        result = null;
        return false;
    }
}

public sealed class RuntimeDynamicLookupRow
{
    public RuntimeDynamicLookupRow(int id, double factor)
    {
        Id = id;
        Factor = factor;
    }

    public int Id { get; }

    public double Factor { get; }
}

public sealed class RuntimeMetaObjectProvider : IDynamicMetaObjectProvider
{
    public DynamicMetaObject GetMetaObject(Expression parameter) =>
        throw new NotSupportedException();

    public IEnumerable<string> GetDynamicMemberNames() => [];
}

public sealed class RuntimeDynamicLibrary : LibraryBase
{
    [BindableMethod]
    public double Scale(double value, double factor) => value * factor;
}

public sealed class RuntimeDynamicSchemaProvider : ISchemaProvider
{
    private readonly IReadOnlyList<RuntimeDynamicRow> _rows;
    private readonly IReadOnlyList<RuntimeDynamicLookupRow> _lookupRows;
    private readonly string _runtimeKeyName;

    public RuntimeDynamicSchemaProvider(
        IReadOnlyList<RuntimeDynamicRow>? rows = null,
        IReadOnlyList<RuntimeDynamicLookupRow>? lookupRows = null,
        string runtimeKeyName = "RuntimeKey")
    {
        _rows = rows ?? [];
        _lookupRows = lookupRows ?? [];
        _runtimeKeyName = runtimeKeyName;
    }

    public List<Type> RequestedRowTypes { get; } = [];

    public ISchema GetSchema(string schema) => new RuntimeDynamicSchema(_rows, _lookupRows, RequestedRowTypes, _runtimeKeyName);
}

public sealed class RuntimeDynamicSchema(
    IReadOnlyList<RuntimeDynamicRow> rows,
    IReadOnlyList<RuntimeDynamicLookupRow> lookupRows,
    List<Type> requestedRowTypes,
    string runtimeKeyName)
    : SchemaBase("runtime", CreateMethods())
{
    private readonly ISchemaColumn[] _eventColumns = CreateEventColumns(runtimeKeyName);

    private static readonly ISchemaColumn[] LookupColumns =
    [
        new RuntimeSchemaColumn("Id", 0, typeof(int)),
        new RuntimeSchemaColumn("Factor", 1, typeof(double))
    ];

    private static MethodsAggregator CreateMethods()
    {
        var manager = new MethodsManager();
        manager.RegisterLibraries(new RuntimeDynamicLibrary());
        return new MethodsAggregator(manager);
    }

    public override ISchemaTable GetTableByName(
        string name,
        SourceMetadataContext metadataContext,
        params object?[] parameters) =>
        name.Equals("events", StringComparison.OrdinalIgnoreCase)
            ? new RuntimeDynamicTable(_eventColumns, typeof(RuntimeDynamicRow))
            : name.Equals("lookup", StringComparison.OrdinalIgnoreCase)
                ? new RuntimeDynamicTable(LookupColumns, typeof(RuntimeDynamicLookupRow))
                : throw new SchemaNotFoundException();

    public override SchemaMethodInfo[] GetRawConstructors(
        string methodName,
        SourceMetadataContext metadataContext) =>
        methodName.Equals("events", StringComparison.OrdinalIgnoreCase) ||
        methodName.Equals("lookup", StringComparison.OrdinalIgnoreCase)
            ? [new SchemaMethodInfo(methodName, SchemaConstructorInfo.Empty())]
            : [];

    public override RowSource<T> GetRowSource<T>(
        string name,
        SourceExecutionContext executionContext,
        params object?[] parameters)
    {
        requestedRowTypes.Add(typeof(T));

        if (name.Equals("events", StringComparison.OrdinalIgnoreCase))
        {
            if (typeof(T) != typeof(RuntimeDynamicRow))
                throw new InvalidOperationException($"events requested as {typeof(T).FullName}");

            return (RowSource<T>)(object)new RuntimeDynamicRowSource(rows);
        }

        if (name.Equals("lookup", StringComparison.OrdinalIgnoreCase))
        {
            if (typeof(T) != typeof(RuntimeDynamicLookupRow))
                throw new InvalidOperationException($"lookup requested as {typeof(T).FullName}");

            return (RowSource<T>)(object)new RuntimeDynamicLookupRowSource(lookupRows);
        }

        throw new SchemaNotFoundException();
    }

    private static ISchemaColumn[] CreateEventColumns(string runtimeKeyName)
    {
        if (runtimeKeyName.Equals("RuntimeKey", StringComparison.Ordinal))
        {
            return
            [
                new RuntimeSchemaColumn("Label", 0, typeof(string)),
                new RuntimeSchemaColumn("RuntimeKey", 1, typeof(int)),
                new RuntimeSchemaColumn("Enabled", 2, typeof(bool)),
                new RuntimeSchemaColumn("Metric", 3, typeof(double)),
                new RuntimeSchemaColumn("Payload", 4, typeof(string)),
                new RuntimeSchemaColumn("Branch", 5, typeof(RuntimeDynamicBranch)),
                new RuntimeSchemaColumn("Branch.Measurement", 6, typeof(double)),
                new RuntimeSchemaColumn("Branch.Raw", 7, typeof(ulong)),
                new RuntimeSchemaColumn("StaticBranch", 8, typeof(RuntimeDynamicBranch)),
                new RuntimeSchemaColumn("StaticBranch.Measurement", 9, typeof(double)),
                new RuntimeSchemaColumn("StaticBranch.Raw", 10, typeof(ulong))
            ];
        }

        return
        [
            new RuntimeSchemaColumn("Label", 0, typeof(string)),
            new RuntimeSchemaColumn(runtimeKeyName, 1, typeof(int))
        ];
    }
}

public sealed class RuntimeDynamicTable(ISchemaColumn[] columns, Type entityType) : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } = columns;

    public SchemaTableMetadata Metadata { get; } = new(entityType);

    public ISchemaColumn? GetColumnByName(string name) =>
        Columns.FirstOrDefault(column => column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase));

    public ISchemaColumn[] GetColumnsByName(string name) =>
        Columns.Where(column => column.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase)).ToArray();
}

public sealed class RuntimeDynamicRowSource(IReadOnlyList<RuntimeDynamicRow> rows)
    : RowSourceBase<RuntimeDynamicRow>
{
    protected override void CollectChunks(IChunkWriter<RuntimeDynamicRow> writer) => writer.Write(rows);
}

public sealed class RuntimeDynamicLookupRowSource(IReadOnlyList<RuntimeDynamicLookupRow> rows)
    : RowSourceBase<RuntimeDynamicLookupRow>
{
    protected override void CollectChunks(IChunkWriter<RuntimeDynamicLookupRow> writer) => writer.Write(rows);
}
}

namespace Musoq.Evaluator.Tests
{

[TestClass]
public sealed class RuntimeDynamicSourceExecutionTests
{
    private readonly ILoggerResolver _loggerResolver = new TestsLoggerResolver();

    [TestMethod]
    public void ConstantProjection_UsesConcreteDynamicRoot_WithoutReadingMembers()
    {
        var row = CreateRow("constant", 1, true, 2.5, "payload", branch: null);
        var provider = new RuntimeDynamicSchemaProvider([row]);

        var inspection = InstanceCreator.CompileForInspection(
            "select 1 as Marker from #runtime.events()",
            Guid.NewGuid().ToString(),
            provider,
            _loggerResolver,
            new CompilationOptions(usePrimitiveTypeValidation: false));

        StringAssert.Contains(inspection.GeneratedCSharpCode, "GetRowSource<Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicRow>");
        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains("GetRowSource<object>", StringComparison.Ordinal));
        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains("GetRowSource<dynamic>", StringComparison.Ordinal));
        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains("((dynamic)", StringComparison.Ordinal));

        var table = InstanceCreator.CompileForExecution(
                "select 1 as Marker from #runtime.events()",
                Guid.NewGuid().ToString(),
                provider,
                _loggerResolver,
                new CompilationOptions(usePrimitiveTypeValidation: false))
            .Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0, row.GetAccessCount("RuntimeKey"));
        Assert.AreEqual(0, row.GetAccessCount("Metric"));
    }

    [TestMethod]
    public void RuntimeMembers_FilterAndProject_WithCanonicalNameAndSingleReads()
    {
        var accepted = CreateRow("accepted", 2, true, 8.5, "unused", CreateBranch(3.5, 9));
        var rejected = CreateRow("rejected", 3, false, 1.5, "never-read", CreateBranch(1.5, 2));
        var provider = new RuntimeDynamicSchemaProvider([accepted, rejected]);

        var table = InstanceCreator.CompileForExecution(
                "select label, metric, payload from #runtime.events() where 2 = runtimekey and runtimekey = 2 and true = enabled",
                Guid.NewGuid().ToString(),
                provider,
                _loggerResolver,
                new CompilationOptions(usePrimitiveTypeValidation: false))
            .Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("accepted", table[0][0]);
        Assert.AreEqual(8.5, table[0][1]);
        Assert.AreEqual("unused", table[0][2]);
        Assert.AreEqual(1, accepted.GetAccessCount("RuntimeKey"));
        Assert.AreEqual(1, accepted.GetAccessCount("Enabled"));
        Assert.AreEqual(1, accepted.GetAccessCount("Metric"));
        Assert.AreEqual(1, accepted.GetAccessCount("Payload"));
        Assert.AreEqual(1, rejected.GetAccessCount("RuntimeKey"));
        Assert.AreEqual(0, rejected.GetAccessCount("Metric"));
        Assert.AreEqual(0, rejected.GetAccessCount("Payload"));
    }

    [TestMethod]
    public void NestedDynamicBranch_NullGuardAndHints_ProduceTypedValues()
    {
        var branch = CreateBranch(4.25, 42);
        var present = CreateRow("present", 1, true, 1, "unused", branch);
        var absent = CreateRow("absent", 2, true, 2, "unused", branch: null);
        var provider = new RuntimeDynamicSchemaProvider([present, absent]);

        var table = InstanceCreator.CompileForExecution(
                "select branch.measurement, branch.raw from #runtime.events() where branch is not null",
                Guid.NewGuid().ToString(),
                provider,
                _loggerResolver,
                new CompilationOptions(usePrimitiveTypeValidation: false))
            .Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(4.25, table[0][0]);
        Assert.AreEqual((ulong)42, table[0][1]);
        Assert.AreEqual(1, present.GetAccessCount("Branch"));
        Assert.AreEqual(1, branch.GetAccessCount("Measurement"));
        Assert.AreEqual(1, branch.GetAccessCount("Raw"));
        Assert.AreEqual(0, absent.GetAccessCount("Measurement"));
        Assert.AreEqual(0, absent.GetAccessCount("Raw"));
    }

    [TestMethod]
    public void StaticClrProperty_ContainingDynamicValue_UsesTypedRootAndDynamicLeaf()
    {
        var branch = CreateBranch(6.5, 64);
        var row = new RuntimeDynamicRow("static", new Dictionary<string, object?>(), branch);
        var provider = new RuntimeDynamicSchemaProvider([row]);

        var table = InstanceCreator.CompileForExecution(
                "select staticbranch.measurement from #runtime.events() where staticbranch is not null",
                Guid.NewGuid().ToString(),
                provider,
                _loggerResolver,
                new CompilationOptions(usePrimitiveTypeValidation: false))
            .Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(6.5, table[0][0]);
        Assert.AreEqual(1, branch.GetAccessCount("Measurement"));
        Assert.IsFalse(provider.RequestedRowTypes.Contains(typeof(object)));
    }

    [TestMethod]
    public void RuntimeJoinKey_AndLibraryArgument_AreStaticallyTyped()
    {
        var row = CreateRow("joined", 7, true, 3.5, "unused", CreateBranch(1, 1));
        var unmatched = CreateRow("unmatched", 8, true, 99, "never-read", CreateBranch(2, 2));
        var provider = new RuntimeDynamicSchemaProvider(
            [row, unmatched],
            [new RuntimeDynamicLookupRow(7, 2)]);

        var inspection = InstanceCreator.CompileForInspection(
            "select Scale(e.metric, l.factor) from #runtime.events() e inner join #runtime.lookup() l on e.runtimekey = l.id",
            Guid.NewGuid().ToString(),
            provider,
            _loggerResolver,
            new CompilationOptions(usePrimitiveTypeValidation: false));

        StringAssert.Contains(inspection.GeneratedCSharpCode, "GetRowSource<Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicRow>");
        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains("dynamic.Scale", StringComparison.Ordinal));
        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains("GetMember", StringComparison.Ordinal));

        var table = InstanceCreator.CompileForExecution(
                "select Scale(e.metric, l.factor) from #runtime.events() e inner join #runtime.lookup() l on e.runtimekey = l.id",
                Guid.NewGuid().ToString(),
                provider,
                _loggerResolver,
                new CompilationOptions(usePrimitiveTypeValidation: false))
            .Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(7, (double)table[0][0]);
        Assert.AreEqual(0, unmatched.GetAccessCount("Metric"));
    }

    [TestMethod]
    public void AlternateSchemas_OnOneDynamicRootType_DoNotContaminateFreshOrWarmCompilations()
    {
        var alphaRow = new RuntimeDynamicRow(
            "alpha",
            new Dictionary<string, object?> { ["AlphaKey"] = 11 });
        var betaRow = new RuntimeDynamicRow(
            "beta",
            new Dictionary<string, object?> { ["BetaKey"] = 22 });

        var alphaProvider = new RuntimeDynamicSchemaProvider([alphaRow], runtimeKeyName: "AlphaKey");
        var betaProvider = new RuntimeDynamicSchemaProvider([betaRow], runtimeKeyName: "BetaKey");

        foreach (var (provider, memberName, expected) in new[]
                 {
                     (alphaProvider, "alphaKey", 11),
                     (betaProvider, "betakey", 22),
                     (alphaProvider, "ALPHAKEY", 11)
                 })
        {
            var query = $"select {memberName} from #runtime.events()";
            var table = InstanceCreator.CompileForExecution(
                    query,
                    Guid.NewGuid().ToString(),
                    provider,
                    _loggerResolver,
                    new CompilationOptions(usePrimitiveTypeValidation: false))
                .Run(CancellationToken.None);

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual(expected, table[0][0]);
        }

        var warmInspection = InstanceCreator.CompileForInspection(
            "select alphakey from #runtime.events()",
            "runtime-dynamic-warm-cache",
            alphaProvider,
            _loggerResolver,
            new CompilationOptions(usePrimitiveTypeValidation: false));
        StringAssert.Contains(warmInspection.GeneratedCSharpCode, ".AlphaKey");

        var warmQuery = InstanceCreator.CompileForExecution(
            "select alphakey from #runtime.events()",
            "runtime-dynamic-warm-cache-execution",
            alphaProvider,
            _loggerResolver,
            new CompilationOptions(usePrimitiveTypeValidation: false));
        Assert.AreEqual(11, warmQuery.Run(CancellationToken.None)[0][0]);

        var warmQueryHit = InstanceCreator.CompileForExecution(
            "select alphakey from #runtime.events()",
            "runtime-dynamic-warm-cache-execution",
            alphaProvider,
            _loggerResolver,
            new CompilationOptions(usePrimitiveTypeValidation: false));
        Assert.AreEqual(11, warmQueryHit.Run(CancellationToken.None)[0][0]);
    }

    [TestMethod]
    public void TryGetMemberFalse_ForAdvertisedColumn_IsAContractFailure()
    {
        var provider = new RuntimeDynamicSchemaProvider(
        [
            new RuntimeDynamicRow("broken", new Dictionary<string, object?>())
        ]);

        var query = InstanceCreator.CompileForExecution(
            "select runtimekey from #runtime.events()",
            Guid.NewGuid().ToString(),
            provider,
            _loggerResolver,
            new CompilationOptions(usePrimitiveTypeValidation: false));

        var exception = Assert.ThrowsExactly<QueryExecutionException>(() => query.Run(CancellationToken.None).Count);
        Assert.IsNotNull(exception.Envelope);
        Assert.AreEqual(DiagnosticCode.MQ9002_InternalExecutionError, exception.Envelope.Code);
    }

    private static RuntimeDynamicRow CreateRow(
        string label,
        int key,
        bool enabled,
        double metric,
        string payload,
        RuntimeDynamicBranch? branch) =>
        new(
            label,
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["RuntimeKey"] = key,
                ["Enabled"] = enabled,
                ["Metric"] = metric,
                ["Payload"] = payload,
                ["Branch"] = branch
            });

    private static RuntimeDynamicBranch CreateBranch(double measurement, ulong raw) =>
        new(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Measurement"] = measurement,
            ["Raw"] = raw
        });
}
}
