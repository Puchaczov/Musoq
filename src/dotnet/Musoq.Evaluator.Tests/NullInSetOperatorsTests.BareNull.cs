using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class NullInSetOperatorsTests
{
    [TestMethod]
    [DataRow("union all", false)]
    [DataRow("union all", true)]
    [DataRow("union", false)]
    [DataRow("union", true)]
    public void BareNullOppositeValueColumn_ShouldLiftBothUnionArms(string operation, bool nullFirst)
    {
        const string value = "select e.Id as K from #A.Entities() e";
        const string missing = "select null as K from #A.Entities() e";
        var sql = nullFirst ? $"{missing} {operation} (K) {value}" : $"{value} {operation} (K) {missing}";
        using var query = CreateAndRunVirtualMachine(sql, SetNullSources());
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("K", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [7], [null]);
    }

    [TestMethod]
    [DataRow(false, 7)]
    [DataRow(true, null)]
    public void BareNullExceptValueColumn_ShouldPreserveLeftRowsAndNullableSchema(bool nullFirst, int? expected)
    {
        const string value = "select e.Id as K from #A.Entities() e";
        const string missing = "select null as K from #A.Entities() e";
        var sql = nullFirst ? $"{missing} except (K) {value}" : $"{value} except (K) {missing}";
        using var query = CreateAndRunVirtualMachine(sql, SetNullSources());
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("K", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [expected]);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void BareNullIntersectValueColumn_ShouldRetainNullableSchemaForEmptyResult(bool nullFirst)
    {
        const string value = "select e.Id as K from #A.Entities() e";
        const string missing = "select null as K from #A.Entities() e";
        var sql = nullFirst ? $"{missing} intersect (K) {value}" : $"{value} intersect (K) {missing}";
        using var query = CreateAndRunVirtualMachine(sql, SetNullSources());
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("K", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table);
    }

    [TestMethod]
    [DataRow("e.Id", typeof(int?), 7)]
    [DataRow("e.Id * 2", typeof(int?), 14)]
    [DataRow("e.Name", typeof(string), "seven")]
    [DataRow("ToDouble(e.Id) / 8", typeof(double?), 0.875)]
    public void BareNullUnion_ShouldPreserveContextualColumnType(string expression, Type type, object expected)
    {
        var sql = $"select {expression} as K from #A.Entities() e union all (K) select null as K from #A.Entities() e";
        using var query = CreateAndRunVirtualMachine(sql, SetNullSources());
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("K", type));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [expected], [null]);
    }

    [TestMethod]
    [DataRow("e.Id", "8", "null", new object?[] { 7, 8, null })]
    [DataRow("null", "e.Id", "8", new object?[] { null, 7, 8 })]
    [DataRow("null", "null", "e.Id", new object?[] { null, null, 7 })]
    [DataRow("e.Id", "null", "null", new object?[] { 7, null, null })]
    public void ThreeWayBareNullUnion_ShouldPropagateNullableCarriers(string first, string second, string third,
        object?[] expected)
    {
        var sql = $"select {first} as K from #A.Entities() e union all (K) " +
                  $"select {second} as K from #A.Entities() e union all (K) select {third} as K from #A.Entities() e";
        using var query = CreateAndRunVirtualMachine(sql, SetNullSources());
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("K", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, expected.Select(value => new[] { value }).ToArray());
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void BareNullOppositeDecodedByte_ShouldUseNullableNativeOutput(bool nullFirst)
    {
        const string value = "select r.K as K from #A.Entities() e cross apply Interpret<R>(FromHexToBytes('01')) r";
        const string missing = "select null as K from #A.Entities() e";
        var sql = "binary R { K: byte } " +
                  (nullFirst ? $"{missing} union all (K) {value}" : $"{value} union all (K) {missing}");
        using var query = CreateAndRunVirtualMachine(sql, SetNullSources());
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("K", typeof(byte?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [(byte)1], [null]);
    }

    [TestMethod]
    public void NullFirstSetCte_ShouldPreserveLeftColumnNameAndNullableType()
    {
        const string sql = "with q as (select null as K from #A.Entities() e union all (K) " +
                           "select e.Id as Other from #A.Entities() e) select q.K from q";
        using var query = CreateAndRunVirtualMachine(sql, SetNullSources());
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("K", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [7], [null]);
    }

    [TestMethod]
    public void TypedNullWorkaround_ShouldRetainUnreachableCaseWarning()
    {
        const string sql = "select e.Id as K from #A.Entities() e union all (K) " +
                           "select case when 1 = 0 then ToInt32(0) else null end as K from #A.Entities() e";
        var inspection = InstanceCreator.CompileForInspection(sql, Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(SetNullSources()), LoggerResolver);

        Assert.IsTrue(inspection.Warnings.Any(warning => warning.Code == DiagnosticCode.MQ5008_UnreachableCode));
    }

    [TestMethod]
    public void LiftedSetComputeMethods_ShouldNotBoxOrInvokeReflection()
    {
        const string sql = "select e.Id as K from #A.Entities() e union all (K) select null as K from #A.Entities() e";
        using var query = CreateAndRunVirtualMachine(sql, SetNullSources());
        var boxedMethods = GeneratedExecutionIlInspection.GetGeneratedComputeMethods(query)
            .Where(GeneratedExecutionIlInspection.ContainsBoxOpcode).Select(static method => method.Name).ToArray();

        Assert.IsEmpty(boxedMethods, string.Join(", ", boxedMethods));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void LiftedSetProjection_ShouldUseDirectTypedConversions(bool decoded)
    {
        var sql = decoded
            ? "binary R { K: byte } select r.K as K from #A.Entities() e cross apply Interpret<R>(FromHexToBytes('01')) r"
            : "select e.Id as K from #A.Entities() e";
        sql += " union all (K) select null as K from #A.Entities() e";
        var inspection = InstanceCreator.CompileForInspection(sql, Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(SetNullSources()), LoggerResolver);

        TestContext.WriteLine(inspection.GeneratedCSharpCode);
        Assert.DoesNotContain("StrictCastRuntime.", inspection.GeneratedCSharpCode);
        Assert.DoesNotContain("Convert.ChangeType", inspection.GeneratedCSharpCode);
        Assert.DoesNotContain("DynamicInvoke", inspection.GeneratedCSharpCode);
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> SetNullSources() => new()
    {
        ["#A"] = [new BasicEntity("seven") { Id = 7 }]
    };
}
