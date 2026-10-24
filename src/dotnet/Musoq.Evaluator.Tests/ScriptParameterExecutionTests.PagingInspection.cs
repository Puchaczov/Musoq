using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Tests.Schema.EnvironmentVariable;

namespace Musoq.Evaluator.Tests;

public partial class ScriptParameterExecutionTests
{
    [TestMethod]
    public void Paging_RuntimeCounts_ShouldValidateOnceOutsideGeneratedRowLoopsWithoutBoxing()
    {
        const string sql = "params(offset: int = 0, limit: int = 1) select Key from #EnvironmentVariables.All() order by Key skip $offset take $limit";
        var code = InstanceCreator.GetGeneratedCSharpCode(sql, Guid.NewGuid().ToString(), new EnvironmentVariablesSchemaProvider(), LoggerResolver);
        Assert.AreEqual(2, Regex.Matches(code, @"PagingCountValidator\.Validate<int>").Count);
        using var query = CreateAndRunVirtualMachine(sql, CreateEnvironmentVariableSources());
        Assert.AreEqual(1, query.Run(TestContext.CancellationToken).Count);
        Assert.IsFalse(GeneratedExecutionIlInspection.GetGeneratedComputeMethods(query).Any(GeneratedExecutionIlInspection.ContainsBoxOpcode));
        var validator = typeof(PagingCountValidator).GetMethod(nameof(PagingCountValidator.Validate))!.MakeGenericMethod(typeof(int));
        Assert.IsFalse(GeneratedExecutionIlInspection.ContainsBoxOpcode(validator));
    }

    [TestMethod]
    [DataRow("select Key, RowNumber() as RowNo from #EnvironmentVariables.All() order by Key skip $offset take $limit")]
    [DataRow("select Key, RowNumber() over (order by Key) as RowNo from #EnvironmentVariables.All() order by Key skip $offset take $limit")]
    public void Paging_RowNumbers_ShouldBeAssignedBeforeSlicing(string sql)
    {
        using var query = CreateAndRunVirtualMachine($"params(offset: int = 1, limit: int = 1) {sql}", CreateEnvironmentVariableSources());
        var table = query.Run(TestContext.CancellationToken);
        Assert.AreEqual("KEY_2", table[0].ReadValue(0));
        Assert.AreEqual(2L, Convert.ToInt64(table[0].ReadValue(1)));
    }
    [TestMethod]
    public void Paging_JoinInspection_ShouldValidateCountsBeforeSources()
    {
        const string sql = "params(offset: int = 1, limit: int = 1) select a.Key from #EnvironmentVariables.All() a inner join #EnvironmentVariables.All() b on a.Key = b.Key order by a.Key skip $offset take $limit";
        var inspection = InstanceCreator.CompileForInspection(sql, Guid.NewGuid().ToString(), new EnvironmentVariablesSchemaProvider(), LoggerResolver);
        Assert.AreEqual(2, Regex.Matches(inspection.GeneratedCSharpCode, @"PagingCountValidator\.Validate<int>").Count);
    }
    [TestMethod]
    public void Paging_DocumentedValuesExample_ShouldReturnOrderedRows()
    {
        const string sql = """
            params(offset: int = 0, limit: int = 100)
            select r.Value
            from values { (Value: 3), (Value: 1), (Value: 2) } r
            order by r.Value
            skip $offset take $limit
            """;
        using var query = CreateAndRunVirtualMachine(sql, CreateEnvironmentVariableSources());
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, query.Run(TestContext.CancellationToken).Select(static row => row.GetValue<int>(0)).ToArray());
    }

    [TestMethod]
    public void Paging_EmptySource_ShouldReturnNoRows()
    {
        var sources = CreateEnvironmentVariableSources();
        sources["*"] = [];
        using var query = CreateAndRunVirtualMachine("params(offset: int = 1, limit: int = 2) select Key from #EnvironmentVariables.All() order by Key skip $offset take $limit", sources);
        Assert.AreEqual(0, query.Run(TestContext.CancellationToken).Count);
    }
}
