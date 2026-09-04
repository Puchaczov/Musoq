using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;
using Musoq.Parser.Diagnostics;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class CrossJoinDiagnosticRegressionTests
{
    private readonly TestsLoggerResolver _loggerResolver = new();

    [TestMethod]
    public void CrossJoin_ShouldExecuteWithoutSyntheticTautologicalWarning()
    {
        const string query = """
            select r.Id, marker.Label
            from values { { Id: 1 } } r
            cross join values { { Label: 'x' }, { Label: 'y' } } marker
            order by marker.Label
            """;

        var result = InstanceCreator.CompileWithDiagnostics(
            query,
            $"CrossJoinDiagnostic_{Guid.NewGuid():N}",
            new SystemSchemaProvider(),
            _loggerResolver);

        Assert.IsTrue(result.Succeeded, FormatDiagnostics(result));
        Assert.IsFalse(result.Warnings.Any(static warning => warning.Code == DiagnosticCode.MQ5010_TautologicalCondition),
            FormatDiagnostics(result));

        using var table = result.CompiledQuery!.Run();
        Assert.HasCount(2, table);
        Assert.AreEqual(1, table[0][0]);
        Assert.AreEqual("x", table[0][1]);
        Assert.AreEqual(1, table[1][0]);
        Assert.AreEqual("y", table[1][1]);
    }

    [TestMethod]
    public void ExplicitTrueInnerJoin_ShouldKeepTautologicalWarning()
    {
        const string query = "select d.Dummy from #system.dual() d inner join #system.dual() e on true";

        var result = InstanceCreator.CompileWithDiagnostics(
            query,
            $"ExplicitTrueJoinDiagnostic_{Guid.NewGuid():N}",
            new SystemSchemaProvider(),
            _loggerResolver);

        Assert.IsTrue(result.Succeeded, FormatDiagnostics(result));
        Assert.AreEqual(
            1,
            result.Warnings.Count(static warning => warning.Code == DiagnosticCode.MQ5010_TautologicalCondition),
            FormatDiagnostics(result));
    }

    [TestMethod]
    public void DerivedApplyWithoutPredicate_ShouldNotExposeSyntheticTautologicalWarning()
    {
        var offendingApplies = new List<string>();
        foreach (var apply in new[] { "cross apply", "outer apply" })
        {
            var query = $$"""
                select r.Id, d.Label
                from values { { Id: 1 } } r
                {{apply}} (
                    select marker.Label
                    from values { { Label: 'x' } } marker
                ) d
                """;

            var result = InstanceCreator.CompileWithDiagnostics(
                query,
                $"DerivedApplyDiagnostic_{apply.Replace(' ', '_')}_{Guid.NewGuid():N}",
                new SystemSchemaProvider(),
                _loggerResolver);

            Assert.IsTrue(result.Succeeded, FormatDiagnostics(result));
            if (result.Warnings.Any(static warning => warning.Code == DiagnosticCode.MQ5010_TautologicalCondition))
                offendingApplies.Add($"{apply}: {FormatDiagnostics(result)}");

            using var table = result.CompiledQuery!.Run();
            Assert.HasCount(1, table);
            Assert.AreEqual(1, table[0][0]);
            Assert.AreEqual("x", table[0][1]);
        }

        Assert.IsEmpty(offendingApplies, string.Join(Environment.NewLine, offendingApplies));
    }

    private static string FormatDiagnostics(BuildResult result)
    {
        return $"{result.CaughtException}{Environment.NewLine}" +
               string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToDetailedString()));
    }
}
