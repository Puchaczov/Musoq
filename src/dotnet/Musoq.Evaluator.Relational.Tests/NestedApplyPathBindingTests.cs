using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class NestedApplyPathBindingTests
{
    [TestMethod]
    [DataRow("child", "Child")]
    [DataRow("CHILD", "child")]
    [DataRow("child", "child")]
    [DataRow("root", "Child")]
    [DataRow("child", "Nested")]
    public void ResidualNestedMember_ShouldUseAvailableIntermediateCarrier(string alias, string member)
    {
        var sql = $"binary LeafSample {{ Score: byte }} binary RootSample {{ {member}: LeafSample }} select {alias}.{member}.Score as Score " +
            $"from values {{ (Dummy: 'x') }} d cross apply Interpret<RootSample>(FromHexToBytes('49')) {alias} where {alias}.{member}.Score = 73";
        using var query = Compile(sql);
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)73]);
    }

    [TestMethod]
    [DataRow("binary LeafSample { Child: byte } binary RootSample { Child: LeafSample }", "child.Child.Child", "49", "")]
    [DataRow("binary LeafSample { Score: byte } binary RootSample { Child: LeafSample[1] }", "child.Child[0].Score", "49", "")]
    [DataRow("binary LeafSample { Score: byte } binary BranchSample { Child: LeafSample } binary RootSample { Child: BranchSample }", "child.Child.Child.Score", "49", "")]
    [DataRow("binary LeafSample { Score: byte } binary RootSample { Child: LeafSample }", "child.Child.Score", "49", " cross apply Split('x', ',') c")]
    public void ReboundPath_ShouldPreserveOrderedMembersAndIndexes(string schemas, string path, string bytes, string chain)
    {
        var sql = schemas + $" select {path} as Score from values {{ (Dummy: 'x') }} d cross apply Interpret<RootSample>(FromHexToBytes('{bytes}')) child{chain} where {path} = 73";
        using var query = Compile(sql);
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)73]);
    }

    [TestMethod]
    [DataRow("00", true)]
    [DataRow("0149", false)]
    public void MaterializedOptionalMember_ShouldKeepNullableLeaf(string bytes, bool missing)
    {
        var condition = missing ? "child.Child.Score is null" : "child.Child.Score = 73";
        var sql = "binary LeafSample { Score: byte } binary RootSample { Has: byte, Child: LeafSample when Has > 0 } " +
            $"select child.Child.Score as Score from values {{ (Dummy: 'x') }} d cross apply Interpret<RootSample>(FromHexToBytes('{bytes}')) child where {condition}";
        using var query = Compile(sql);
        using var table = query.Run();
        Assert.AreEqual(typeof(byte?), table.Columns.First().ColumnType);
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { missing ? null : (byte?)73 });
    }

    [TestMethod]
    [DataRow("select child.Child as Record", "select c.Record.Score from c c", true)]
    [DataRow("select child.Child.Score as Score", "select c.Score from c c", false)]
    public void ReboundNestedMember_ShouldSurviveCteProjection(string projection, string final, bool complex)
    {
        var sql = "binary LeafSample { Score: byte } binary RootSample { Child: LeafSample } with c as (" + projection +
            " from values { (Dummy: 'x') } d cross apply Interpret<RootSample>(FromHexToBytes('49')) child where child.Child.Score = 73) " + final;
        using var query = Compile(sql, complex);
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)73]);
    }

    [TestMethod]
    public void RepeatedMemberNames_ShouldKeepBothProjectedRecordRoots()
    {
        const string sql = "binary LeafSample { Score: byte } binary BranchSample { Child: LeafSample } binary RootSample { Child: BranchSample } " +
            "with c as (select child.Child as Parent, child.Child.Child as Child from values { (Dummy: 'x') } d " +
            "cross apply Interpret<RootSample>(FromHexToBytes('49')) child where child.Child.Child.Score = 73) " +
            "select c.Parent.Child.Score, c.Child.Score from c c";
        using var query = Compile(sql, true);
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)73, (byte)73]);
    }

    [TestMethod]
    public void RepeatedMembersWithDifferentLeafValues_ShouldNotReadTheEnclosingLeaf()
    {
        const string sql = "binary LeafSample { Score: byte } binary BranchSample { Child: LeafSample, Score: byte } binary RootSample { Child: BranchSample } " +
            "select child.Child.Child.Score as InnerScore, child.Child.Score as OuterScore from values { (Dummy: 'x') } d " +
            "cross apply Interpret<RootSample>(FromHexToBytes('4913')) child where child.Child.Child.Score = 73 and child.Child.Score = 19";
        using var query = Compile(sql);
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)73, (byte)19]);
    }

    [TestMethod]
    public void MemberMatchingCteCarrier_ShouldKeepTheNestedRecordIdentity()
    {
        const string sql = "binary LeafSample { Score: byte } binary BranchSample { Child: LeafSample } binary RootSample { Child: BranchSample } " +
            "with c as (select s.Child as Child from values { (Dummy: 'x') } d cross apply Interpret<RootSample>(FromHexToBytes('49')) s), " +
            "q as (select child.Child.Child as Decoded from c child) select q.Decoded.Score from q q";
        using var query = Compile(sql, true);
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)73]);
    }

    [TestMethod]
    public void ParameterAndInjectedCarrierChain_ShouldKeepNestedAccess()
    {
        const string sql = "param(batchLabel: string = 'demo') binary LeafSample { Score: int le } binary RootSample { Child: LeafSample } " +
            "select batch.Label, child.Child.Score as Score from #fixture.batches($batchLabel, false) batch cross apply #fixture.samples(batch.Key) sample " +
            "cross apply Interpret<RootSample>(sample.ReadPayload()) child where child.Child.Score = 73";
        using var query = InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString(),
            new ApplyCarrierSchemaProvider(), new TestsLoggerResolver(), new CompilationOptions());
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["batch-alpha", 73]);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(" where child.Child is not null")]
    public void NestedMemberWithoutResidualLeafPredicate_ShouldRemainValid(string where)
    {
        var sql = "binary LeafSample { Score: byte } binary RootSample { Child: LeafSample } select child.Child.Score as Score " +
            "from values { (Dummy: 'x') } d cross apply Interpret<RootSample>(FromHexToBytes('49')) child" + where;
        using var query = Compile(sql);
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)73]);
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public void NestedPath_ShouldRemainTypedAcrossExecutionAndReuseModes(bool parallel, bool reuse)
    {
        const string sql = "binary LeafSample { Score: byte } binary RootSample { Child: LeafSample } select child.Child.Score as Score " +
            "from values { (Dummy: 'x') } d cross apply Interpret<RootSample>(FromHexToBytes('49')) child where child.Child.Score = 73";
        var options = new CompilationOptions(parallelizationMode: parallel ? null : ParallelizationMode.None,
            useCommonSubexpressionElimination: reuse).WithStabilityAwareScalarReuse(reuse).WithLoopInvariantCodeMotion(reuse);
        using var query = InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString(), new TestSchemaProvider(),
            new TestsLoggerResolver(), options);
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)73]);
        Assert.IsFalse(GeneratedExecutionIlInspection.GetGeneratedComputeMethods(query)
            .Any(GeneratedExecutionIlInspection.ContainsBoxOpcode));
    }

    [TestMethod]
    public void ReboundMember_ShouldUseConcreteGeneratedFieldAndAvailableCarrier()
    {
        const string sql = "binary LeafSample { Score: byte } binary RootSample { Child: LeafSample } select child.Child.Score as Score " +
            "from values { (Dummy: 'x') } d cross apply Interpret<RootSample>(FromHexToBytes('49')) child where child.Child.Score = 73";
        var inspection = InstanceCreator.CompileForInspection(sql, Guid.NewGuid().ToString(), new TestSchemaProvider(),
            new TestsLoggerResolver(), new CompilationOptions());
        Assert.Contains("dchild.child.Child.Score", inspection.ExecutionPlanText);
        Assert.DoesNotContain("If [(child.Child.Score", inspection.ExecutionPlanText);
        Assert.Contains("Musoq.Generated.Interpreters.LeafSample child_Child", inspection.GeneratedCSharpCode);
        Assert.DoesNotContain("((object)", inspection.GeneratedCSharpCode);
        Assert.DoesNotContain(".GetValue<object>(", inspection.GeneratedCSharpCode);
    }

    private static CompiledQuery Compile(string sql, bool complex = false) => InstanceCreator.CompileForExecution(sql,
        Guid.NewGuid().ToString(), new TestSchemaProvider(), new TestsLoggerResolver(), new CompilationOptions(usePrimitiveTypeValidation: !complex));

}
