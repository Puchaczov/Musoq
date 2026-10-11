using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class RegexBudgetExecutionTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("p.Name rlike r'v.lue'", "value", "v.lue", 1000)]
    [DataRow("p.Name rlike p.City", "value", "v.lue", 3000)]
    [DataRow("p.Name like r'v_lue'", "value", "v_lue", 1000)]
    [DataRow("p.Name like p.City", "value", "v_lue", 3000)]
    [DataRow("p.Name like 'k%'", "Kelvin", "unused", 3000)]
    public void TableExecution_ShouldCarryBudgetToMatchersAndUnicodeFallback(
        string predicate, string name, string pattern, int milliseconds)
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = new[] { new BasicEntity { Name = name, City = pattern } }
        };
        var options = new CompilationOptions(ParallelizationMode.None)
            .WithRegexMatchTimeout(TimeSpan.FromMilliseconds(milliseconds));
        var query = $"select {predicate} as Match from #A.Entities() p";
        var provider = new BasicSchemaProvider<BasicEntity>(sources);
        var loggerResolver = new TestsLoggerResolver();
        var result = InstanceCreator.CompileForInspection(query, Guid.NewGuid().ToString(),
            provider, loggerResolver, options);
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            provider, loggerResolver, options);

        using var table = compiled.Run(TestContext.CancellationToken);

        Assert.HasCount(1, table);
        Assert.AreEqual(true, table[0].ReadValue(0));
        Assert.AreEqual(options.RegexMatchTimeout, result.ExecutionPlan!.RegexMatchTimeout);
        StringAssert.Contains(result.GeneratedCSharpCode,
            $"TimeSpan.FromTicks({options.RegexMatchTimeout.Ticks.ToString(CultureInfo.InvariantCulture)}L)");
    }

    [TestMethod]
    [DataRow(ParallelizationMode.None, 1000)]
    [DataRow(ParallelizationMode.Full, 3000)]
    public void DynamicPatterns_ShouldRetainBudgetInSerialAndParallelExecution(ParallelizationMode mode, int milliseconds)
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = Enumerable.Range(0, 4096).Select(static _ => new BasicEntity { Name = "value", City = "v.lue" })
        };
        var options = new CompilationOptions(mode, maxDegreeOfParallelismOverride: 2)
            .WithRegexMatchTimeout(TimeSpan.FromMilliseconds(milliseconds));
        using var query = CreateAndRunVirtualMachine(
            "select p.Name from #A.Entities() p where p.Name rlike p.City", sources, options);

        using var table = query.Run(TestContext.CancellationToken);

        Assert.HasCount(4096, table);
        Assert.IsTrue(table.All(static row => (string)row.ReadValue(0) == "value"));
    }

    [TestMethod]
    [DataRow("select Count(Name) filter (where Name like 'k%') as Matches from #A.Entities()")]
    [DataRow("select Count(Name) filter (where Name like 'k%') over (order by Id) as Matches from #A.Entities()")]
    public void AggregateFilters_ShouldRetainCustomBudgetForUnicodeFallback(string query)
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = new[] { new BasicEntity { Name = "Kelvin", Id = 1 } }
        };
        var options = new CompilationOptions(ParallelizationMode.None).WithRegexMatchTimeout(TimeSpan.FromSeconds(3));
        var provider = new BasicSchemaProvider<BasicEntity>(sources);
        var loggerResolver = new TestsLoggerResolver();
        var inspection = InstanceCreator.CompileForInspection(query, Guid.NewGuid().ToString(), provider, loggerResolver, options);
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), provider, loggerResolver, options);

        using var result = compiled.Run(TestContext.CancellationToken);

        Assert.HasCount(1, result);
        Assert.AreEqual(1, Convert.ToInt32(result[0].ReadValue(0), CultureInfo.InvariantCulture));
        StringAssert.Contains(inspection.GeneratedCSharpCode, "TimeSpan.FromTicks(30000000L)");
        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains("TimeSpan.FromTicks(20000000L)", StringComparison.Ordinal));
    }
}
