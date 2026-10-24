using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class SqlConcatenationTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("'a' || 'b'", "ab")]
    [DataRow("'🙂' || 'β'", "🙂β")]
    [DataRow("null || 'x'", null)]
    [DataRow("'x' || null", null)]
    [DataRow("null || null", null)]
    [DataRow("'a' || 'b' + 'c'", "abc")]
    [DataRow("'a' + 'b' || 'c'", "abc")]
    [DataRow("('a' || null) || 'b'", null)]
    [DataRow("case when 1 = 1 then 'a' || 'b' end", "ab")]
    [DataRow("ToString(1) || 'x'", "1x")]
    public void SqlConcatenation_ShouldPreserveStringAndNullSemantics(string expression, string? expected)
    {
        using var query = CreateAndRunVirtualMachine($"select {expression} as A from #A.Entities() e", Sources());
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("A", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { expected });
    }

    [TestMethod]
    [DataRow("'a' || 1")]
    [DataRow("1 || 'b'")]
    [DataRow("null || 1")]
    [DataRow("e.Population || 'b'")]
    public void SqlConcatenation_ShouldRejectImplicitFormatting(string expression)
    {
        var analysis = new QueryAnalyzer(new BasicSchemaProvider<BasicEntity>(Sources()))
            .Analyze($"select {expression} from #A.Entities() e");

        Assert.IsTrue(analysis.HasErrors);
        Assert.AreEqual(DiagnosticCode.MQ3007_InvalidOperandTypes, analysis.Errors.First().Code);
    }

    [TestMethod]
    [DataRow(2, true)]
    [DataRow(3, true)]
    [DataRow(4, true)]
    [DataRow(5, true)]
    [DataRow(8, true)]
    [DataRow(16, true)]
    [DataRow(8, false)]
    public void HomogeneousChains_ShouldPreserveNativeStringsAndOptimizerToggles(int arity, bool optimize)
    {
        var expression = string.Join(" || ", Enumerable.Repeat("e.Name", arity));
        var options = new CompilationOptions(ParallelizationMode.None, useCommonSubexpressionElimination: optimize,
            useConstantFolding: optimize).WithLoopInvariantCodeMotion(optimize);
        using var query = CreateAndRunVirtualMachine($"select {expression} as A from #A.Entities() e", Sources(), options);
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { string.Concat(Enumerable.Repeat("🙂", arity)) });
        Assert.IsEmpty(GeneratedExecutionIlInspection.GetGeneratedComputeMethods(query)
            .Where(GeneratedExecutionIlInspection.ContainsBoxOpcode).Select(method => method.Name).ToArray());
    }

    [TestMethod]
    public void SqlConcatenation_ShouldPreserveBitwisePipeAndLegacyPlus()
    {
        using var query = CreateAndRunVirtualMachine(
            "select 1 | 2 as Bits, e.Name + e.City as Legacy, e.Name || e.City as Sql from #A.Entities() e", Sources());
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [3, "🙂", null]);
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> Sources() => new()
    {
        ["#A"] = [new BasicEntity("🙂") { City = null, Population = 1m }]
    };
}
