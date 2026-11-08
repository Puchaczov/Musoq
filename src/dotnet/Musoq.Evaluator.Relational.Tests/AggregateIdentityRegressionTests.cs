using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Plugins;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class AggregateIdentityRegressionTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    public void DistinctWrappedAggregates_ShouldRetainTheirOwnValues(int profile)
    {
        const string sql = """
            select Trim(Max(case when e.Name = 'A' then ' a ' else null end)) as A,
                   Trim(Max(case when e.Name = 'B' then ' b ' else null end)) as B
            from #A.Entities() e
            """;

        using var query = CreateAndRunVirtualMachine(sql, Sources(), Options(profile));
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, ["a", "b"]);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void GroupedNestedWrappers_ShouldPreserveDistinctAndNullAggregateResults(bool parallel)
    {
        const string sql = """
            select e.City as City,
                   Trim(Trim(Max(case when e.Name = 'A' then ' a ' else null end))) as A,
                   Trim(Trim(Max(case when e.Name = 'B' then ' b ' else null end))) as B
            from #A.Entities() e group by e.City order by e.City
            """;
        var options = new CompilationOptions(parallel ? ParallelizationMode.Full : ParallelizationMode.None);

        using var query = CreateAndRunVirtualMachine(sql, Sources(), options);
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, ["one", "a", "b"], ["two", null, null]);
    }

    [TestMethod]
    [DataRow(false, 2, false)]
    [DataRow(true, 1, false)]
    [DataRow(false, 2, true)]
    [DataRow(true, 1, true)]
    public void GeneratedProjection_ShouldReuseOnlyIdenticalWrappedAggregates(bool repeated, int expectedCalls, bool grouped)
    {
        const string first = "Max(case when e.Name = 'A' then ' a ' else null end)";
        var second = repeated ? first : "Max(case when e.Name = 'B' then ' b ' else null end)";
        var groupColumn = grouped ? "e.City as City, " : string.Empty;
        var sql = $"select {groupColumn}Trim({first}) as A, Trim({second}) as B from #A.Entities() e" +
                  (grouped ? " group by e.City order by e.City" : string.Empty);
        using var query = CreateAndRunVirtualMachine(sql, Sources());
        using var table = query.Run(TestContext.CancellationToken);
        var inspection = InstanceCreator.CompileForInspection(sql, Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(Sources()), LoggerResolver);

        Assert.AreEqual(expectedCalls, Regex.Matches(inspection.GeneratedCSharpCode, @"\.Trim\(").Count);
        TableMaterializationTestHelper.AssertRowsInOrder(table, grouped
            ? [["one", "a", repeated ? "a" : "b"], ["two", null, null]]
            : [["a", repeated ? "a" : "b"]]);
    }

    [TestMethod]
    [DataRow("group")]
    [DataRow("group-type")]
    [DataRow("generated-group-type")]
    [DataRow("field")]
    [DataRow("depth")]
    [DataRow("prefix")]
    [DataRow("owner")]
    public void AggregateFingerprint_ShouldDistinguishStorageIdentity(string difference)
    {
        var original = AggregateRead();
        var changed = difference switch
        {
            "group" => original with { Group = original.Group with { Name = "other" } },
            "group-type" => original with { Group = new ExecutionVariable("group", typeof(string)) },
            "generated-group-type" => original with { Group = original.Group with { GeneratedRowTypeName = "OtherGroup" } },
            "field" => original with { Accumulator = original.Accumulator with { FieldName = "__agg1" } },
            "depth" => original with { Accumulator = original.Accumulator with { ParentDepth = 1 } },
            "prefix" => original with { Accumulator = original.Accumulator with { OwnerPrefixLength = 1 } },
            "owner" => original with { Accumulator = original.Accumulator with { OwnerFieldName = "parent" } },
            _ => throw new ArgumentOutOfRangeException(nameof(difference))
        };

        Assert.AreNotEqual(ExecutionExpressionFingerprint.ForHoist(original),
            ExecutionExpressionFingerprint.ForHoist(changed));
    }

    [TestMethod]
    public void AggregateFingerprint_ShouldIgnoreDisplayLabelsForTheSameStorage()
    {
        var original = AggregateRead();
        var renamed = original with { DisplayName = "Another SQL label" };

        Assert.AreEqual(ExecutionExpressionFingerprint.ForHoist(original),
            ExecutionExpressionFingerprint.ForHoist(renamed));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void WrappedAggregateComputeMethods_ShouldNotBoxOrInvokeReflection(bool grouped)
    {
        const string projection = """
            select Trim(Max(case when e.Name = 'A' then ' a ' else null end)) as A,
                   Trim(Max(case when e.Name = 'B' then ' b ' else null end)) as B
            from #A.Entities() e
            """;
        var sql = projection + (grouped ? " group by e.City" : string.Empty);
        using var query = CreateAndRunVirtualMachine(sql, Sources());

        var boxedMethods = GeneratedExecutionIlInspection.GetGeneratedComputeMethods(query)
            .Where(GeneratedExecutionIlInspection.ContainsBoxOpcode)
            .Select(static method => method.Name)
            .ToArray();

        Assert.IsEmpty(boxedMethods,
            $"Wrapped aggregate execution must retain typed values: {string.Join(", ", boxedMethods)}");
    }

    private static ExecutionAggregateCall AggregateRead()
    {
        var declaration = typeof(LibraryBase).GetMethod(nameof(LibraryBase.Count), [typeof(int?), typeof(int)])!;
        var accumulator = new AggregateAccumulatorField("Count(Value)", "__agg0", AggregateKernelDescriptor.Create(declaration));
        return new ExecutionAggregateCall(new ExecutionVariable("group", typeof(object)), declaration, [], typeof(long), accumulator);
    }

    private static CompilationOptions Options(int profile) =>
        new CompilationOptions(useCommonSubexpressionElimination: profile != 1)
            .WithLoopInvariantCodeMotion(profile != 2)
            .WithStabilityAwareScalarReuse(profile != 3);

    private static Dictionary<string, IEnumerable<BasicEntity>> Sources() => new()
    {
        ["#A"] =
        [
            new BasicEntity("A") { City = "one" },
            new BasicEntity("B") { City = "one" },
            new BasicEntity((string?)null) { City = "two" }
        ]
    };
}
