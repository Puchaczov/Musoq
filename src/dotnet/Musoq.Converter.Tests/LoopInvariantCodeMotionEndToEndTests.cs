using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;

namespace Musoq.Converter.Tests;

[TestClass]
[DoNotParallelize]
public sealed class LoopInvariantCodeMotionEndToEndTests
{
    private readonly TestsLoggerResolver _loggerResolver = new();

    [TestMethod]
    public void StableApplyProjection_WhenLicmIsEnabled_ShouldEvaluateAtOwnerLoopScopes()
    {
        var on = Run(
            "select a.Value, b.Value, c.Value from #licm.outers() a cross apply a.Middles b cross apply b.Leaves c",
            enabled: true,
            useCse: false);

        Assert.AreEqual(24, on.RowCount);
        Assert.AreEqual(2, on.OuterValueReads);
        Assert.AreEqual(6, on.MiddleValueReads);
        Assert.AreEqual(24, on.LeafValueReads);

        var off = Run(
            "select a.Value, b.Value, c.Value from #licm.outers() a cross apply a.Middles b cross apply b.Leaves c",
            enabled: false,
            useCse: false);

        Assert.AreEqual(24, off.RowCount);
        Assert.AreEqual(6, off.OuterValueReads);
        Assert.AreEqual(6, off.MiddleValueReads);
        Assert.AreEqual(24, off.LeafValueReads);
        AssertTablesEqual(on.TableValues, off.TableValues);
    }

    [TestMethod]
    public void LicmAndCseToggles_ShouldRemainIndependent()
    {
        const string query =
            "select a.Value, a.Value from #licm.outers() a cross apply a.Middles b";

        var licmOnCseOff = Run(query, enabled: true, useCse: false);
        var licmOffCseOn = Run(query, enabled: false, useCse: true);
        var bothOff = Run(query, enabled: false, useCse: false);

        Assert.AreEqual(6, licmOnCseOff.RowCount);
        Assert.AreEqual(2, licmOnCseOff.OuterValueReads);
        Assert.AreEqual(6, licmOffCseOn.OuterValueReads);
        Assert.AreEqual(12, bothOff.OuterValueReads);
        AssertTablesEqual(licmOnCseOff.TableValues, licmOffCseOn.TableValues);
        AssertTablesEqual(licmOnCseOff.TableValues, bothOff.TableValues);
    }

    [TestMethod]
    public void VolatileOuterColumn_WhenLicmIsEnabled_ShouldRemainAtLeaf()
    {
        var result = Run(
            "select a.VolatileValue, b.Value, c.Value from #licm.outers() a cross apply a.Middles b cross apply b.Leaves c",
            enabled: true,
            useCse: false);

        Assert.AreEqual(24, result.RowCount);
        Assert.AreEqual(6, result.OuterVolatileValueReads);
        Assert.AreEqual(6, result.MiddleValueReads);
        Assert.AreEqual(24, result.LeafValueReads);
    }

    [TestMethod]
    public void StableAndVolatileFunctions_ShouldRespectOwnerAndOutputScopes()
    {
        var result = Run(
            "select a.StableOf(a.Value), a.StablePair(a.Value, b.Value), a.VolatileOf(b.Value), a.VolatileOf(b.Value) from #licm.outers() a cross apply a.Middles b",
            enabled: true,
            useCse: true);

        Assert.AreEqual(6, result.RowCount);
        Assert.AreEqual(2, result.StableOuterFunctionCalls);
        Assert.AreEqual(6, result.StablePairFunctionCalls);
        Assert.AreEqual(12, result.VolatileFunctionCalls);
    }

    [TestMethod]
    public void TwoVolatileLeafReferences_ShouldNeverCollapseAcrossThreeLoops()
    {
        var result = Run(
            "select a.VolatileOf(c.Value), a.VolatileOf(c.Value) from #licm.outers() a cross apply a.Middles b cross apply b.Leaves c",
            enabled: true,
            useCse: true);

        Assert.AreEqual(24, result.RowCount);
        Assert.AreEqual(48, result.VolatileFunctionCalls);
    }

    [TestMethod]
    public void EmptyCrossApply_ShouldEagerlyReadStableOuterAndSkipVolatileLeaf()
    {
        var result = Run(
            "select a.Value, b.VolatileValue from #licm.outers() a cross apply a.EmptyMiddles b",
            enabled: true,
            useCse: false);

        Assert.AreEqual(0, result.RowCount);
        Assert.AreEqual(2, result.OuterValueReads);
        Assert.AreEqual(0, result.MiddleVolatileValueReads);
    }

    [TestMethod]
    public void OuterApplyAndOrdinality_ShouldPreserveNullAndOrderParity()
    {
        var query = "select a.Id, a.Value, b.Ordinal from #licm.outers() a outer apply a.EmptyMiddles b with ordinality order by a.Id, b.Ordinal";
        var on = Run(query, enabled: true, useCse: false);
        var off = Run(query, enabled: false, useCse: false);

        Assert.AreEqual(2, on.RowCount);
        Assert.IsNull(on.TableValues[0][2]);
        Assert.IsNull(on.TableValues[1][2]);
        Assert.AreEqual(2, on.OuterValueReads);
        AssertTablesEqual(on.TableValues, off.TableValues);
    }

    [TestMethod]
    public void FiltersAggregatesAndCteMaterialization_ShouldPreserveResultParity()
    {
        var queries = new[]
        {
            "select a.Id, b.Value from #licm.outers() a cross apply a.Middles b where b.Value > 0 order by a.Id, b.Value",
            "select a.Id, sum(a.StablePair(a.Value, b.Value)) from #licm.outers() a cross apply a.Middles b group by a.Id order by a.Id",
            "with x as (select a.Id, a.Value from #licm.outers() a) select x.Id, x.Value from x order by x.Id",
            "with x as (select a.Id, a.Value from #licm.outers() a) select x.Id, x.Value from x cross apply #licm.outers() y where y.Id = x.Id order by x.Id"
        };

        foreach (var query in queries)
        {
            var on = Run(query, enabled: true, useCse: false);
            var off = Run(query, enabled: false, useCse: false);
            AssertTablesEqual(on.TableValues, off.TableValues);
        }
    }

    [TestMethod]
    public void GeneratedShape_WhenLicmIsEnabled_ShouldUseEagerLocalsWithoutLazyGuards()
    {
        var inspection = InstanceCreator.CompileForInspection(
            "select a.Value, b.Value, c.Value from #licm.outers() a cross apply a.Middles b cross apply b.Leaves c",
            "LicmGeneratedShape",
            new LoopInvariantSchemaProvider(),
            _loggerResolver,
            CreateOptions(enabled: true, useCse: false));

        Assert.IsNotNull(inspection.GeneratedCSharpCode);
        var tree = CSharpSyntaxTree.ParseText(inspection.GeneratedCSharpCode);
        var locals = tree.GetRoot()
            .DescendantNodes()
            .OfType<VariableDeclaratorSyntax>()
            .Select(static declarator => declarator.Identifier.ValueText)
            .ToArray();

        CollectionAssert.Contains(locals, "aValue");
        Assert.IsTrue(locals.Any(static name =>
            name.StartsWith("ab", StringComparison.Ordinal) &&
            name.EndsWith("Value", StringComparison.Ordinal)));
        Assert.IsFalse(locals.Any(static name =>
            name.Contains("initialized", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("cache", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains("??=", StringComparison.Ordinal));
    }

    private RunResult Run(string query, bool enabled, bool useCse)
    {
        LoopInvariantCounters.Reset();
        using var compiled = InstanceCreator.CompileForExecution(
            query,
            $"Licm_{Guid.NewGuid():N}",
            new LoopInvariantSchemaProvider(),
            _loggerResolver,
            CreateOptions(enabled, useCse));
        using var table = compiled.Run();
        var rowCount = table.Count;
        var values = Enumerable.Range(0, rowCount)
            .Select(row => Enumerable.Range(0, table.Columns.Count()).Select(column => table[row][column]).ToArray())
            .ToArray();

        return new RunResult(
            rowCount,
            values,
            LoopInvariantCounters.OuterValueReads,
            LoopInvariantCounters.OuterVolatileValueReads,
            LoopInvariantCounters.MiddleValueReads,
            LoopInvariantCounters.MiddleVolatileValueReads,
            LoopInvariantCounters.LeafValueReads,
            LoopInvariantCounters.StableOuterFunctionCalls,
            LoopInvariantCounters.StablePairFunctionCalls,
            LoopInvariantCounters.VolatileFunctionCalls);
    }

    private static CompilationOptions CreateOptions(bool enabled, bool useCse)
    {
        return new CompilationOptions(
                ParallelizationMode.None,
                useCommonSubexpressionElimination: useCse)
            .WithLoopInvariantCodeMotion(enabled);
    }

    private static void AssertTablesEqual(object?[][] expected, object?[][] actual)
    {
        Assert.HasCount(expected.Length, actual);
        for (var row = 0; row < expected.Length; row++)
        {
            Assert.HasCount(expected[row].Length, actual[row]);
            for (var column = 0; column < expected[row].Length; column++)
                Assert.AreEqual(expected[row][column], actual[row][column]);
        }
    }

    private sealed record RunResult(
        int RowCount,
        object?[][] TableValues,
        int OuterValueReads,
        int OuterVolatileValueReads,
        int MiddleValueReads,
        int MiddleVolatileValueReads,
        int LeafValueReads,
        int StableOuterFunctionCalls,
        int StablePairFunctionCalls,
        int VolatileFunctionCalls);
}
