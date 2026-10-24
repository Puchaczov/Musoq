using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed partial class NullExtendedScalarProjectionTests
{
    [TestMethod]
    [DataRow("select Coalesce(b.Dummy, 'fallback') as F from values { (Dummy: 'x') } a left outer join values { (Dummy: 'x') } b on 1 = 0", "fallback")]
    [DataRow("select Coalesce(a.Dummy, 'fallback') as F from values { (Dummy: 'x') } a right outer join values { (Dummy: 'x') } b on 1 = 0", "fallback")]
    [DataRow("select Coalesce(b.Dummy, 'fallback') as F from values { (Dummy: 'x') } a full outer join values { (Dummy: 'x') } b on 1 = 0 where a.Dummy is not null", "fallback")]
    [DataRow("select Coalesce(b.Value, -1) as F from values { (Value: 0) } a left outer join values { (Value: 0) } b on 1 = 0", "-1")]
    [DataRow("select Coalesce(b.Dummy, ToString(a.Value)) as F from values { (Value: 7) } a left outer join values { (Dummy: 'x') } b on 1 = 0", "7")]
    [DataRow("select Coalesce(b.Dummy, 'fallback') as F from values { (Dummy: 'x') } a left outer join values { (Dummy: 'x') } b on 1 = 1", "x")]
    [DataRow("with b as (select case when 1=1 then null else 'x' end as W from values { (Dummy: 'x') } d) select Coalesce(b.W, 'fallback') as F from values { (Dummy: 'x') } a left outer join b b on 1=1", "fallback")]
    [DataRow("select Coalesce(b.Value, 'fallback') as F from values { (Dummy: 'x') } a outer apply Split('x', ',') b where b.Value = 'x'", "x")]
    [DataRow("select Coalesce(b.Value, 'fallback') as F from values { (Dummy: 'x') } a outer apply Split(case when 1=1 then null else 'x' end, ',') b", "fallback")]
    public void ScalarCall_OnNullExtendedRows_ShouldEvaluateNormally(string sql, string expected)
    {
        using var query = Compile(sql);
        using var table = query.Run();
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(expected, Convert.ToString(table[0].ReadValue(0), System.Globalization.CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public void Coalesce_InOriginalCteJoin_ShouldReturnFallbackForMissingRows()
    {
        const string sql = "with a as (select x.Value as K from values { (Value: 0), (Value: 1), (Value: 2), (Value: 3), (Value: 4) } x), b as (select y.Value as K, 'b' as W from values { (Value: 3), (Value: 4) } y) select a.K, Coalesce(b.W, 'fallback') as F from a a left outer join b b on a.K = b.K order by a.K";
        using var query = Compile(sql);
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            [0, "fallback"], [1, "fallback"], [2, "fallback"], [3, "b"], [4, "b"]);
    }

    [TestMethod]
    public void MissingRawColumn_ShouldRemainNull()
    {
        using var query = Compile("select b.Dummy, b.Dummy is null as Missing from values { (Dummy: 'x') } a left outer join values { (Dummy: 'x') } b on 1=0");
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [null, true]);
    }

    private static CompiledQuery Compile(string sql) => InstanceCreator.CompileForExecution(sql,
        Guid.NewGuid().ToString(), new TestSchemaProvider(), new TestsLoggerResolver(), new CompilationOptions());

}
