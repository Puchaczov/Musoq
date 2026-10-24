using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class ImplicitElseTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("case when e.Id = 1 then e.Id end")]
    [DataRow("case e.Id when 1 then e.Id end")]
    [DataRow("case when e.NullableValue = 1 then e.Id end")]
    [DataRow("case e.NullableValue when 1 then e.Id end")]
    [DataRow("case when e.Id = 1 then case when e.Name = 'A' then e.Id end end")]
    public void OmittedElse_ShouldReturnMatchingValueOrContextualNull(string expression)
    {
        using var query = CreateAndRunVirtualMachine($"select {expression} as A from #A.Entities() e", Sources());
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("A", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1], [null], [null]);
    }

    [TestMethod]
    public void UnmatchedOmittedElse_ShouldNotEvaluateItsThenArm()
    {
        const string sql = "select case when e.Id = 0 then 1 / (e.Id - e.Id) end as A from #A.Entities() e";
        using var query = CreateAndRunVirtualMachine(sql, Sources());
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("A", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [null], [null], [null]);
    }

    [TestMethod]
    public void WrappedAggregatesWithImplicitElse_ShouldPreserveSeparateState()
    {
        const string sql = "select Trim(Max(case when e.Id = 1 then ' a ' end)) as A, " +
                           "Trim(Max(case when e.Id = 2 then ' b ' end)) as B from #A.Entities() e";
        using var query = CreateAndRunVirtualMachine(sql, Sources());
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, ["a", "b"]);
    }

    [TestMethod]
    [DataRow("case when e.Id = 1 then e.Id")]
    [DataRow("case e.Id when 1 then e.Id")]
    public void ImplicitAndExplicitNullElse_ShouldGenerateEquivalentTypedExecution(string expression)
    {
        var provider = new BasicSchemaProvider<BasicEntity>(Sources());
        var implicitForm = InstanceCreator.CompileForInspection(
            $"select {expression} end as A from #A.Entities() e", Guid.NewGuid().ToString(), provider, LoggerResolver);
        var explicitForm = InstanceCreator.CompileForInspection(
            $"select {expression} else null end as A from #A.Entities() e", Guid.NewGuid().ToString(), provider, LoggerResolver);

        Assert.AreEqual(GeneratedCodeSampleArtifacts.NormalizeForComparison(explicitForm.GeneratedCSharpCode),
            GeneratedCodeSampleArtifacts.NormalizeForComparison(implicitForm.GeneratedCSharpCode));
    }

    [TestMethod]
    public void ImplicitElseCompute_ShouldRemainTypedWithoutBoxingOrReflection()
    {
        using var query = CreateAndRunVirtualMachine(
            "select case when e.Id = 1 then e.Id end as A from #A.Entities() e", Sources());
        var boxed = GeneratedExecutionIlInspection.GetGeneratedComputeMethods(query)
            .Where(GeneratedExecutionIlInspection.ContainsBoxOpcode).Select(method => method.Name).ToArray();

        Assert.IsEmpty(boxed, string.Join(", ", boxed));
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> Sources() => new()
    {
        ["#A"] = [new BasicEntity("A") { Id = 1, NullableValue = 1 },
            new BasicEntity("B") { Id = 2, NullableValue = 2 },
            new BasicEntity((string?)null) { Id = 3, NullableValue = null }]
    };
}
