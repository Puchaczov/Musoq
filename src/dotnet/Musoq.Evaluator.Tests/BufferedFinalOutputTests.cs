using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.RuntimeV2;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class BufferedFinalOutputTests
{
    public static IEnumerable<object[]> Scenarios =>
        from file in new[] { "Q03_InnerJoin.cs", "Q24_AggregateNoGroupBy.cs", "Q101_RuntimeV2WindowRunningSum.cs", "Q227_PerformanceJoinAggregate.cs" }
        from mode in new[] { QueryInstrumentationMode.Disabled, QueryInstrumentationMode.SourceBoundaries, QueryInstrumentationMode.Full }
        select new object[] { file, mode };

    [TestMethod]
    [DynamicData(nameof(Scenarios))]
    public void TerminalBufferedOutput_PreservesRowsProfilesAndOwnership(string file, QueryInstrumentationMode mode)
    {
        var sample = GeneratedCodeSamplesCatalog.GetByFileName(file);
        var scenario = CreateScenario(file);
        using var query = InstanceCreator.CompileForExecution(sample.Query, Guid.NewGuid().ToString(),
            scenario.Provider, new TestsLoggerResolver(), sample.CompilationOptions.WithInstrumentationMode(mode));

        using var first = Run(query, mode, scenario);
        TableMaterializationTestHelper.AssertRowsUnordered(first, scenario.Rows);
        if (file == "Q24_AggregateNoGroupBy.cs")
            first[0].AssignValue(0, 99L);
        else
            first[0].AssignValue(0, "mutated");
        using var second = Run(query, mode, scenario);

        TableMaterializationTestHelper.AssertRowsUnordered(second, scenario.Rows);
        Assert.AreNotSame(first[0], second[0]);
    }

    private static Table Run(CompiledQuery query, QueryInstrumentationMode mode, Scenario scenario)
    {
        if (mode == QueryInstrumentationMode.Disabled)
            return query.Run();

        var result = query.RunWithProfile();
        Assert.AreEqual(scenario.SourceRows, result.Profile.Sources.Sum(static source => source.RowsRead));
        if (mode == QueryInstrumentationMode.Full)
            Assert.AreEqual((long)scenario.Rows.Length, result.Profile.Operators
                .Where(static operation => operation.Name == "AppendShape").Sum(static operation => operation.OutputRows));
        return result.Result;
    }

    private static Scenario CreateScenario(string file)
    {
        var basic = new BasicSchemaProvider<BasicEntity>(new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity { Id = 1, Name = "A", Country = "PL", City = "X", Population = 10 },
                new BasicEntity { Id = 2, Name = "B", Country = "DE", City = "X", Population = 20 }],
            ["#B"] = [new BasicEntity { Name = "one", City = "X" }, new BasicEntity { Name = "two", City = "X" }]
        });
        return file switch
        {
            "Q03_InnerJoin.cs" => new Scenario(basic, [["A", "PL"], ["B", "DE"]], 4),
            "Q24_AggregateNoGroupBy.cs" => new Scenario(basic, [[2L, 30m, 10m, 20m]], 2),
            "Q227_PerformanceJoinAggregate.cs" => new Scenario(basic, [["X", 4L]], 4),
            "Q101_RuntimeV2WindowRunningSum.cs" => new Scenario(new RuntimeV2RegressionSchemaProvider([
                    new RuntimeV2RegressionEntity { Name = "A", Department = "X", Salary = 10 },
                    new RuntimeV2RegressionEntity { Name = "B", Department = "X", Salary = 20 }]),
                [["A", "X", 10m], ["B", "X", 30m]], 2),
            _ => throw new ArgumentOutOfRangeException(nameof(file))
        };
    }

    private sealed record Scenario(ISchemaProvider Provider, object?[][] Rows, long SourceRows);
}
