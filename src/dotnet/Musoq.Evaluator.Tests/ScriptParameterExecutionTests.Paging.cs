using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Diagnostics;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests;

public partial class ScriptParameterExecutionTests
{
    [TestMethod]
    public void Paging_DefaultsAndRepeatedOverrides_ShouldUseCurrentExecutionValues()
    {
        using var query = CreateAndRunVirtualMachine("""
            params(offset: int = 0, limit: int = 1)
            select Key from #EnvironmentVariables.All() order by Key skip $offset take $limit
            """, CreateEnvironmentVariableSources());
        Assert.AreEqual("KEY_1", query.Run(TestContext.CancellationToken)[0].ReadValue(0));
        NativeParameterTestInputs.Set(query.TypedParameters, query.ParameterDefinitions, "offset", 1);
        Assert.AreEqual("KEY_2", query.Run(TestContext.CancellationToken)[0].ReadValue(0));
        NativeParameterTestInputs.Set(query.TypedParameters, query.ParameterDefinitions, "limit", 0);
        Assert.AreEqual(0, query.Run(TestContext.CancellationToken).Count);
        NativeParameterTestInputs.Set(query.TypedParameters, query.ParameterDefinitions, "offset", 0);
        NativeParameterTestInputs.Set(query.TypedParameters, query.ParameterDefinitions, "limit", 2);
        Assert.AreEqual(2, query.Run(TestContext.CancellationToken).Count);
    }

    [TestMethod]
    [DataRow("skip $count", 1)]
    [DataRow("take $count", 1)]
    [DataRow("skip $count take 1", 1)]
    [DataRow("skip 0 take $count", 1)]
    public void Paging_UnorderedReferences_ShouldSliceRows(string paging, int expectedCount)
    {
        using var query = CreateAndRunVirtualMachine($"param(count: int = 1) select Key from #EnvironmentVariables.All() {paging}", CreateEnvironmentVariableSources());
        Assert.AreEqual(expectedCount, query.Run(TestContext.CancellationToken).Count);
    }

    [TestMethod]
    [DataRow("byte", (byte)1)]
    [DataRow("sbyte", (sbyte)1)]
    [DataRow("short", (short)1)]
    [DataRow("ushort", (ushort)1)]
    [DataRow("int", 1)]
    [DataRow("uint", (uint)1)]
    [DataRow("long", (long)1)]
    [DataRow("ulong", (ulong)1)]
    [DataRow("int?", 1)]
    public void Paging_IntegralParameter_ShouldAcceptTypedCount(string type, object value)
    {
        using var query = CreateAndRunVirtualMachine($"param(count: {type}) select Key from #EnvironmentVariables.All() order by Key skip $count take 1", CreateEnvironmentVariableSources());
        NativeParameterTestInputs.Set(query.TypedParameters, query.ParameterDefinitions, "count", value);
        Assert.AreEqual("KEY_2", query.Run(TestContext.CancellationToken)[0].ReadValue(0));
    }

    [TestMethod]
    [DataRow("int", -1)]
    [DataRow("long", 2147483648L)]
    [DataRow("ulong", 18446744073709551615UL)]
    [DataRow("int?", null)]
    public void Paging_InvalidRuntimeCount_ShouldReportStructuredError(string type, object? value)
    {
        using var query = CreateAndRunVirtualMachine($"param(count: {type}) select Key from #EnvironmentVariables.All() take $count", CreateEnvironmentVariableSources());
        NativeParameterTestInputs.Set(query.TypedParameters, query.ParameterDefinitions, "count", value);
        var exception = Assert.ThrowsExactly<PagingCountException>(() => _ = query.Run(TestContext.CancellationToken).Count);
        Assert.AreEqual(DiagnosticCode.MQ7019_InvalidRuntimePagingCount, exception.Code);
        StringAssert.Contains(exception.Message, "TAKE count '$count'");
    }

    [TestMethod]
    [DataRow("let offset: int = 1", "skip $offset take 1", "KEY_2")]
    [DataRow("let limit: long = 1L", "take $limit", "KEY_1")]
    public void Paging_LetCount_ShouldFoldConstant(string declaration, string paging, string expected)
    {
        using var query = CreateAndRunVirtualMachine($"{declaration} select Key from #EnvironmentVariables.All() order by Key {paging}", CreateEnvironmentVariableSources());
        Assert.AreEqual(expected, query.Run(TestContext.CancellationToken)[0].ReadValue(0));
    }

    [TestMethod]
    [DataRow("skip $count take 1", 0)]
    [DataRow("skip 0 take $count", 2)]
    [DataRow("skip $count take $count", 0)]
    public void Paging_MaximumCount_ShouldNotOverflow(string paging, int expected)
    {
        using var query = CreateAndRunVirtualMachine($"param(count: int = 2147483647) select Key from #EnvironmentVariables.All() order by Key {paging}", CreateEnvironmentVariableSources());
        Assert.AreEqual(expected, query.Run(TestContext.CancellationToken).Count);
    }

    [TestMethod]
    [DataRow("with rows as (select Key from #EnvironmentVariables.All() order by Key skip $offset take $limit) select Key from rows", "KEY_2")]
    [DataRow("select r.Key from (select Key from #EnvironmentVariables.All() order by Key skip $offset take $limit) r", "KEY_2")]
    [DataRow("select Key from #EnvironmentVariables.All() union all select Key from #EnvironmentVariables.All() order by Key skip $offset take $limit", "KEY_1")]
    [DataRow("select Key, Count(*) from #EnvironmentVariables.All() group by Key order by Key skip $offset take $limit", "KEY_2")]
    [DataRow("select a.Key from #EnvironmentVariables.All() a inner join #EnvironmentVariables.All() b on a.Key = b.Key order by a.Key skip $offset take $limit", "KEY_2")]
    [DataRow("select (select Key from #EnvironmentVariables.All() order by Key skip $offset take $limit) as Chosen from #EnvironmentVariables.All()", "KEY_2")]
    public void Paging_QueryBoundaries_ShouldCaptureRuntimeCounts(string sql, string expected)
    {
        using var query = CreateAndRunVirtualMachine($"params(offset: int = 1, limit: int = 1) {sql}", CreateEnvironmentVariableSources());
        Assert.AreEqual(expected, query.Run(TestContext.CancellationToken)[0].ReadValue(0));
    }
}
