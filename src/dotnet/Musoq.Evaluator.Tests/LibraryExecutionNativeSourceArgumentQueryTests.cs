using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;
using Musoq.Schema.Reflection;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class LibraryExecutionNativeSourceArgumentQueryTests : MSTestContextTestBase
{
    internal const string ApplyQuery = """
            select a.Id as Id, b.Amount as Amount, b.Label as Label
            from #native.rows() a
            cross apply #native.echo(a.Amount, a.Label) b
            order by Id
            """;

    internal static ISchemaProvider CreateProvider() => new NativeProvider(new NativeSchema());

    [TestMethod]
    public void ApplyArguments_ShouldRetainConcreteNullableValuesForEveryInvocation()
    {
        const string sql = ApplyQuery;
        var schema = new NativeSchema();
        using var query = InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString(),
            new NativeProvider(schema), new TestsLoggerResolver());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table,
            ("Id", typeof(int)), ("Amount", typeof(decimal?)), ("Label", typeof(string)));
        var rows = table.Select(row => (row.GetValue<int>(0), row.GetValue<decimal?>(1), row.GetValue<string>(2))).ToArray();
        CollectionAssert.AreEqual(new (int, decimal?, string)[]
            { (1, 1.25m, "first"), (2, null, "empty"), (3, -4m, "last") }, rows);
        CollectionAssert.AreEqual(new (decimal?, string)[]
            { (1.25m, "first"), (null, "empty"), (-4m, "last") }, schema.Invocations.ToArray());
    }

    [TestMethod]
    [DataRow("1.25", "value")]
    [DataRow("null", "empty")]
    public void SourceLiteralArguments_ShouldUseDeclaredNullableSignature(string amount, string label)
    {
        var schema = new NativeSchema();
        using var query = InstanceCreator.CompileForExecution(
            $"select Amount, Label from #native.echo({amount}, '{label}')",
            Guid.NewGuid().ToString(), new NativeProvider(schema), new TestsLoggerResolver());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Amount", typeof(decimal?)), ("Label", typeof(string)));
        var rows = table.Select(row => (row.GetValue<decimal?>(0), row.GetValue<string>(1))).ToArray();
        Assert.HasCount(1, rows);
        Assert.AreEqual(amount == "null" ? (decimal?)null : 1.25m, rows[0].Item1);
        Assert.AreEqual(label, rows[0].Item2);
        CollectionAssert.AreEqual(rows, schema.Invocations.ToArray());
    }

    [TestMethod]
    public void NativeArgumentRead_ShouldAllocateNothingForNumericValues()
    {
        var (allocated, total) = AllocationTestThread.Run(MeasureNativeArgumentReads);
        Assert.AreEqual(82500m, total);
        Assert.AreEqual(0L, allocated);
    }

    private static (long Allocated, decimal Total) MeasureNativeArgumentReads()
    {
        var arguments = SourceArguments.Create((decimal?)1.25m,
            SourceArguments.Create(7L, SourceArguments.Empty));
        for (var index = 0; index < 1000; index++)
            _ = arguments.GetValue<decimal?>(0)!.Value + arguments.GetValue<long>(1);
        var before = GC.GetAllocatedBytesForCurrentThread();
        decimal total = 0m;
        for (var index = 0; index < 10000; index++)
            total += arguments.GetValue<decimal?>(0)!.Value + arguments.GetValue<long>(1);
        return (GC.GetAllocatedBytesForCurrentThread() - before, total);
    }

    public sealed record NativeEntity(int Id, decimal? Amount, string Label);
    public sealed class EchoSignature(decimal? amount, string label)
    {
        public decimal? Amount => amount;
        public string Label => label;
    }

    private sealed class NativeProvider(NativeSchema schema) : ISchemaProvider
    {
        public ISchema GetSchema(string name) => schema;
    }

    private sealed class NativeSchema() : SchemaBase("native", new MethodsAggregator(new MethodsManager()))
    {
        public List<(decimal?, string)> Invocations { get; } = [];
        public override ISchemaTable GetTableByName(string name, SourceMetadataContext context,
            params object?[] parameters) => new GenericEntityTable<NativeEntity>();
        public override SchemaMethodInfo[] GetRawConstructors(SourceMetadataContext context)
            => [..GetRawConstructors("rows", context), ..GetRawConstructors("echo", context)];
        public override SchemaMethodInfo[] GetRawConstructors(string name, SourceMetadataContext context)
            => name == "echo"
                ? [new SchemaMethodInfo(name, new Musoq.Schema.Reflection.ConstructorInfo(
                    typeof(EchoSignature).GetConstructors().Single(), false,
                    ("amount", typeof(decimal?)), ("label", typeof(string))))]
                : [new SchemaMethodInfo(name, Musoq.Schema.Reflection.ConstructorInfo.Empty())];
        public override RowSource<T> GetRowSource<T, TArguments>(string name,
            SourceExecutionContext context, TArguments arguments)
        {
            NativeEntity[] rows;
            if (name == "echo")
            {
                var amount = arguments.GetValue<decimal?>(0);
                var label = arguments.GetValue<string>(1);
                Invocations.Add((amount, label));
                rows = [new NativeEntity(0, amount, label)];
            }
            else
            {
                rows = [new NativeEntity(1, 1.25m, "first"), new NativeEntity(2, null, "empty"),
                    new NativeEntity(3, -4m, "last")];
            }
            return EnsureSourceType<T, NativeEntity>(name,
                new EntitySource<NativeEntity>([rows], new Dictionary<string, int>(),
                    new Dictionary<int, Func<NativeEntity, object?>>()));
        }
    }
}
