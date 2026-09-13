using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator;

namespace Musoq.Examples.DataSources.StructuredInputs.Tests;

[TestClass]
public sealed class StructuredInputExecutionModeQualificationTests
{
    [TestMethod]
    public void Sql_InlineStructuredSourceProducesEquivalentResultsAcrossExecutionModes()
    {
        const string query =
            "select m.PatternId, m.MatchText from #inputs.match('TODO FIXME', patterns: array { (Id: 'todo', Pattern: 'TODO'), (Id: 'fixme', Pattern: 'FIXME') }) m";

        var results = new Dictionary<ParallelizationMode, string[]>(2);
        foreach (var mode in new[] { ParallelizationMode.None, ParallelizationMode.Full })
        {
            StructuredInputSourceCounters.Reset();
            using var compiled = Compile(query, $"structured-mode-{mode}", new CompilationOptions(
                parallelizationMode: mode,
                useCteParallelization: mode == ParallelizationMode.Full));
            using var table = compiled.Run();
            results[mode] = table.Rows
                .Select(static row => $"{row[0]}:{row[1]}")
                .ToArray();
            Assert.AreEqual(1, StructuredInputSourceCounters.MatchConstructed);
        }

        CollectionAssert.AreEqual(results[ParallelizationMode.None], results[ParallelizationMode.Full]);
    }

    [TestMethod]
    public void Sql_IndependentStructuredCtesRemainEquivalentWhenCteParallelizationChanges()
    {
        const string query =
            "with leftRows as (select n.Value from #inputs.numbers(values: array { 1, 2, 2 }) n), " +
            "rightRows as (select n.Value from #inputs.numbers(values: array { 3, 4 }) n) " +
            "select l.Value, r.Value from leftRows l cross join rightRows r order by l.Value, r.Value";

        var results = new Dictionary<bool, string[]>(2);
        foreach (var enabled in new[] { false, true })
        {
            StructuredInputSourceCounters.Reset();
            using var compiled = Compile(query, $"structured-cte-parallel-{enabled}", new CompilationOptions(
                parallelizationMode: ParallelizationMode.Full,
                useCteParallelization: enabled));
            using var table = compiled.Run();
            results[enabled] = table.Rows
                .Select(static row => $"{row[0]}:{row[1]}")
                .ToArray();
            Assert.AreEqual(2, StructuredInputSourceCounters.NumbersConstructed);
        }

        CollectionAssert.AreEqual(results[false], results[true]);
    }

    [TestMethod]
    public void Sql_ParallelCtesCanBePassedDirectlyToTypedSources()
    {
        const string query =
            "with patterns as (select p1.Id, p1.Pattern from values { (Id: 'todo', Pattern: 'TODO'), (Id: 'fixme', Pattern: 'FIXME') } p1), " +
            "numbers as (select p2.Value from values { (Value: 1), (Value: 2) } p2) " +
            "select m.PatternId, n.Value from #inputs.match('TODO FIXME', patterns: patterns) m " +
            "cross join #inputs.numbers(values: numbers) n";

        StructuredInputSourceCounters.Reset();
        using var compiled = Compile(query, "structured-parallel-cte-source", new CompilationOptions(
            parallelizationMode: ParallelizationMode.Full,
            useCteParallelization: true));
        using var table = compiled.Run();

        Assert.AreEqual(4, table.Rows.Count);
        Assert.AreEqual(1, StructuredInputSourceCounters.MatchConstructed);
        Assert.AreEqual(1, StructuredInputSourceCounters.NumbersConstructed);
    }

    [TestMethod]
    public void Sql_SameCompiledArtifactPreservesInvocationAndInputIsolationAcrossConcurrentRuns()
    {
        const string query =
            "param(patterns: (Id: string, Pattern: string)[]) select m.PatternId, m.MatchText, m.Offset from #inputs.match('TODO FIXME', patterns: $patterns) m";
        var compiled = InstanceCreator.CompileForTypedExecution<PatternMatchRow>(
            query,
            "structured-concurrent-same-artifact",
            new StructuredInputsSchemaProvider(),
            new NullLoggerResolver(),
            new CompilationOptions(ParallelizationMode.None),
            CancellationToken.None);
        StructuredInputSourceCounters.Reset();

        var results = Task.WhenAll(
            Enumerable.Range(0, 4).Select(index => Task.Run(() =>
            {
                var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["patterns"] = new[]
                    {
                        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["Id"] = $"run-{index}",
                            ["Pattern"] = index % 2 == 0 ? "TODO" : "FIXME"
                        }
                    }
                };
                return compiled.Run(new TypedQueryRunOptions(CancellationToken.None, parameters)).ToArray();
            })));

        foreach (var (result, index) in results.GetAwaiter().GetResult().Select((result, index) => (result, index)))
        {
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual($"run-{index}", result[0].PatternId);
            Assert.AreEqual(index % 2 == 0 ? "TODO" : "FIXME", result[0].MatchText);
        }
        Assert.AreEqual(0, StructuredInputSourceCounters.NumbersConstructed);
        Assert.AreEqual(4, StructuredInputSourceCounters.MatchConstructed);
    }

    private static CompiledQuery Compile(string query, string name, CompilationOptions options)
    {
        var build = InstanceCreator.CompileWithDiagnostics(
            query,
            name,
            new StructuredInputsSchemaProvider(),
            new NullLoggerResolver(),
            options);
        Assert.IsTrue(build.Succeeded, string.Join(Environment.NewLine, build.Diagnostics.Select(static diagnostic => diagnostic.ToDetailedString())));
        return build.CompiledQuery!;
    }

    private sealed class NullLoggerResolver : ILoggerResolver
    {
        public ILogger ResolveLogger() => NullLogger.Instance;

        public ILogger<T> ResolveLogger<T>() => NullLogger<T>.Instance;
    }
}
