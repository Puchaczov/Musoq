using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class ApplyBranchPredicateTests
{
    private const string Schema = "binary Two { A: byte, B: byte } ";

    [TestMethod]
    [DataRow("union all", "r", "1;2")]
    [DataRow("union all", "r2", "1;2")]
    [DataRow("union", "r", "1;2")]
    [DataRow("except", "r", "1")]
    [DataRow("intersect", "r", "")]
    public void SetBranches_ShouldGuardInterpretationIndependentlyOfAliasSpelling(string operation, string alias, string expected)
    {
        var sql = Schema + Branch("r", "A") + $" {operation} (A) " + Branch(alias, "B");
        var provider = new TestSchemaProvider();
        var inspection = InstanceCreator.CompileForInspection(sql, Guid.NewGuid().ToString(), provider, new TestsLoggerResolver());
        Assert.AreEqual(2, inspection.PlanningText.Split("-> PreApplyRight (High)").Length - 1, inspection.PlanningText);
        using var query = InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString(), provider, new TestsLoggerResolver());
        using var table = query.Run();
        Assert.AreEqual(expected, string.Join(";", table.Select(row => row.GetValue<byte>(0)).Order()));
        Assert.IsFalse(GeneratedExecutionIlInspection.GetGeneratedComputeMethods(query).Any(GeneratedExecutionIlInspection.ContainsBoxOpcode));
    }

    [TestMethod]
    public void ReusedAliasesInCteDefinitions_ShouldKeepEachBranchGuard()
    {
        var sql = Schema + "with first as (" + Branch("r", "A") + "), second as (" + Branch("r", "B") +
            ") select f.A from first f union all (A) select s.A from second s";
        using var query = InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString(), new TestSchemaProvider(), new TestsLoggerResolver());
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)1], [(byte)2]);
    }

    [TestMethod]
    [DataRow("union all")]
    [DataRow("union")]
    [DataRow("except")]
    [DataRow("intersect")]
    public void SetBranches_ShouldNeverEnumerateRejectedApplyInputs(string operation)
    {
        var fixture = ApplyTraversalFixture.Create();
        var sql = "select b.X as X from #counting.parents() a cross apply a.Children b where a.Name = 'keep' " +
            operation + " (X) select b.X as X from #counting.parents() a cross apply a.Children b where a.Name = 'keep'";
        using var query = CompileCounting(sql, fixture);
        using var table = query.Run();
        Assert.AreEqual(operation == "except" ? 0 : operation == "union all" ? 4 : 2, table.Count);
        Assert.AreEqual(0, fixture.Rejected.Children.EnumerationCount);
        Assert.AreEqual(2, fixture.Kept.Children.EnumerationCount);
    }

    [TestMethod]
    [DataRow("b.X = 1", "right-side or future APPLY alias", 2)]
    [DataRow("a.Name = ToString(Rand())", "non-deterministic", 0)]
    public void SetBranchPredicates_WithRightDependenciesOrVolatility_ShouldRemainResidual(string predicate, string reason, int expectedRows)
    {
        var fixture = ApplyTraversalFixture.Create();
        var branch = "select b.X as X from #counting.parents() a cross apply a.Children b where " + predicate;
        var sql = branch + " union all (X) " + branch;
        var inspection = InstanceCreator.CompileForInspection(sql, Guid.NewGuid().ToString(),
            CountingProvider(fixture), new TestsLoggerResolver(), new CompilationOptions(usePrimitiveTypeValidation: false));
        Assert.Contains(reason, inspection.PlanningText);
        Assert.DoesNotContain("-> PreApplyRight (High)", inspection.PlanningText);
        using var query = CompileCounting(sql, fixture);
        using var table = query.Run();
        Assert.AreEqual(expectedRows, table.Count);
        Assert.AreEqual(2, fixture.Rejected.Children.EnumerationCount);
        Assert.AreEqual(2, fixture.Kept.Children.EnumerationCount);
    }

    private static CompiledQuery CompileCounting(string sql, ApplyTraversalFixture fixture) =>
        InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString(), CountingProvider(fixture),
            new TestsLoggerResolver(), new CompilationOptions(usePrimitiveTypeValidation: false));

    private static ISchemaProvider CountingProvider(ApplyTraversalFixture fixture) =>
        new GenericSchemaProvider(new Dictionary<string, ISchema> { ["#counting"] = fixture.Schema });

    private static string Branch(string alias, string field) =>
        $"select t.{field} as A from values {{ (Value: 0), (Value: 1) }} {alias} cross apply " +
        $"Interpret<Two>(case when {alias}.Value = 0 then FromHexToBytes('0102') else FromHexToBytes('FF') end) t where {alias}.Value = 0";

}
