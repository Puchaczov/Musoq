using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator;
using MusoqApi = Musoq.Converter.Musoq;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class TypedRegexBudgetTests
{
    static TypedRegexBudgetTests() => Culture.ApplyWithDefaultCulture();

    [TestMethod]
    [DataRow(ParallelizationMode.None, 1000)]
    [DataRow(ParallelizationMode.Full, 3000)]
    public void TypedExecution_ShouldCarryCustomBudgetIntoDynamicMatcher(ParallelizationMode mode, int milliseconds)
    {
        var chunks = Enumerable.Range(0, 4)
            .Select(static _ => Enumerable.Repeat(new PatternInput("value", "v.lue"), 1024).ToArray());
        var options = new CompilationOptions(mode, maxDegreeOfParallelismOverride: 2)
            .WithRegexMatchTimeout(TimeSpan.FromMilliseconds(milliseconds));
        var builder = MusoqApi.Query("select p.Value rlike p.Pattern as Match from #A.entities() p")
            .Source<PatternInput>("#A", "entities", chunks).WithCompilationOptions(options);
        var inspection = builder.InspectTyped<PatternOutput>();

        var rows = builder.CompileAndRun<PatternOutput>(CancellationToken.None).ToArray();

        Assert.HasCount(4096, rows);
        Assert.IsTrue(rows.All(static row => row.Match));
        StringAssert.Contains(inspection.GeneratedCSharpCode,
            $"TimeSpan.FromTicks({options.RegexMatchTimeout.Ticks.ToString(CultureInfo.InvariantCulture)}L)");
    }

    public sealed record PatternInput(string Value, string Pattern);
    public sealed record PatternOutput(bool Match);
}
