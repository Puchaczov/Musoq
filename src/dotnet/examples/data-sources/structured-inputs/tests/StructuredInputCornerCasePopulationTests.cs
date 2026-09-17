using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Schema.StructuralInputs;
using System.Security.Cryptography;
using System.Text;

namespace Musoq.Examples.DataSources.StructuredInputs.Tests;

/// <summary>
/// Deterministic compiled execution population for structural source arguments.
/// Every run receives a fresh host representation and checks source invocation
/// boundaries as well as result semantics.
/// </summary>
[TestClass]
public sealed class StructuredInputCornerCasePopulationTests
{
    private const int CaseCount = 128;
    private const int Seed = 0xC7E_20;
    private const int OriginCount = 4;
    private const int OperatorCount = 8;
    private const int ModeCount = 4;

    [TestMethod]
    public void CompiledExecutionPopulation_ShouldCompileDistinctQueriesAndSnapshotEquivalentHosts()
    {
        var random = new Random(Seed);
        var normalizedHashes = new HashSet<string>(StringComparer.Ordinal);
        var coverageCells = new HashSet<string>(StringComparer.Ordinal);

        for (var originIndex = 0; originIndex < OriginCount; originIndex++)
        {
            for (var operatorIndex = 0; operatorIndex < OperatorCount; operatorIndex++)
            {
                for (var modeIndex = 0; modeIndex < ModeCount; modeIndex++)
                {
                    var index = originIndex * OperatorCount * ModeCount + operatorIndex * ModeCount + modeIndex;
                    var sourceContext = (SourceContext)((originIndex + operatorIndex + modeIndex) % Enum.GetValues<SourceContext>().Length);
                    if (originIndex is (int)InputOrigin.Host or (int)InputOrigin.Let)
                        sourceContext = SourceContext.From;
                    if (originIndex == (int)InputOrigin.Cte)
                        sourceContext = SourceContext.From;
                    if (originIndex == (int)InputOrigin.Cte && sourceContext == SourceContext.Coupled)
                        sourceContext = SourceContext.From;

                    var specs = CreateSpecs(index, random);
                    var testCase = CreateExecutionCase(
                        (InputOrigin)originIndex,
                        (CteOperator)operatorIndex,
                        sourceContext,
                        modeIndex,
                        index,
                        specs);

                    Assert.IsTrue(
                        normalizedHashes.Add(HashNormalizedQuery(testCase.Query)),
                        $"Execution case {index} duplicated a normalized query hash.");
                    coverageCells.Add($"origin|{testCase.Origin}");
                    coverageCells.Add($"operator|{testCase.Operator}");
                    coverageCells.Add($"context|{testCase.Context}");
                    coverageCells.Add($"mode|{testCase.Mode}");

                    using var compiled = Compile(testCase.Query, $"structured-input-corner-population-{index}", testCase.Options);
                    if (testCase.Host is not null)
                        compiled.Parameters["patterns"] = testCase.Host;
                    StructuredInputSourceCounters.Reset();

                    using var table = compiled.Run();
                    var actual = table.Rows
                        .Select(static row => $"{row[0]}:{row[1]}")
                        .ToArray();

                    CollectionAssert.AreEqual(
                        testCase.Expected,
                        actual,
                        $"Compiled execution case {index} ({testCase.Origin}/{testCase.Operator}/{testCase.Context}/{testCase.Mode}). Expected=[{string.Join(",", testCase.Expected)}] Actual=[{string.Join(",", actual)}]");
                    Assert.AreEqual(1, StructuredInputSourceCounters.MatchConstructed, $"Case {index} source invocation count.");
                }
            }
        }

        Assert.AreEqual(CaseCount, normalizedHashes.Count, "Every compiled case must have a distinct normalized query hash.");
        Assert.AreEqual(OriginCount, coverageCells.Count(static cell => cell.StartsWith("origin|", StringComparison.Ordinal)));
        Assert.AreEqual(OperatorCount, coverageCells.Count(static cell => cell.StartsWith("operator|", StringComparison.Ordinal)));
        Assert.AreEqual(Enum.GetValues<SourceContext>().Length, coverageCells.Count(static cell => cell.StartsWith("context|", StringComparison.Ordinal)));
        Assert.AreEqual(ModeCount, coverageCells.Count(static cell => cell.StartsWith("mode|", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void CompiledExecutionMetamorphicCases_ShouldRespectRecordEquivalenceAndArrayOrder()
    {
        const string query =
            "select m.PatternId, m.MatchText from #inputs.match('TODO FIXME', patterns: array { " +
            "(Id: 'todo', Pattern: 'TODO'), (Id: 'fixme', Pattern: 'FIXME') }) m";
        const string reorderedQuery =
            "select m.PatternId, m.MatchText from #inputs.match('TODO FIXME', patterns: array { " +
            "(pattern: 'TODO', id: 'todo',), (PATTERN: 'FIXME', ID: 'fixme') }) m";
        const string reversedArrayQuery =
            "select m.PatternId, m.MatchText from #inputs.match('TODO FIXME', patterns: array { " +
            "(Id: 'fixme', Pattern: 'FIXME'), (Id: 'todo', Pattern: 'TODO') }) m";

        using var first = Compile(query, "structured-input-metamorphic-first");
        using var reordered = Compile(reorderedQuery, "structured-input-metamorphic-reordered");
        using var reversed = Compile(reversedArrayQuery, "structured-input-metamorphic-reversed");

        StructuredInputSourceCounters.Reset();
        using var firstTable = first.Run();
        var firstRows = firstTable.Rows.Select(static row => $"{row[0]}:{row[1]}").ToArray();

        StructuredInputSourceCounters.Reset();
        using var reorderedTable = reordered.Run();
        var reorderedRows = reorderedTable.Rows.Select(static row => $"{row[0]}:{row[1]}").ToArray();
        CollectionAssert.AreEqual(firstRows, reorderedRows, "Field order and casing should preserve binding.");

        StructuredInputSourceCounters.Reset();
        using var reversedTable = reversed.Run();
        var reversedRows = reversedTable.Rows.Select(static row => $"{row[0]}:{row[1]}").ToArray();
        CollectionAssert.AreNotEqual(firstRows, reversedRows, "Array order is observable and must not be treated as equivalent.");
        Assert.AreEqual(1, StructuredInputSourceCounters.MatchConstructed);
    }

    [TestMethod]
    public void CompiledExecutionMetamorphicHosts_ShouldProduceEquivalentSnapshots()
    {
        const string query =
            "param(patterns: (Id: string, Pattern: string)[]) " +
            "select m.PatternId, m.MatchText from #inputs.match('TODO FIXME', patterns: $patterns) m";
        using var compiled = Compile(query, "structured-input-host-representations");

        var list = new List<Dictionary<string, object?>>
        {
            new(StringComparer.OrdinalIgnoreCase) { ["Id"] = "todo", ["Pattern"] = "TODO" },
            new(StringComparer.OrdinalIgnoreCase) { ["Id"] = "fixme", ["Pattern"] = "FIXME" }
        };
        var array = list.Select(static record => new Dictionary<string, object?>(record, StringComparer.OrdinalIgnoreCase)).ToArray();
        var structural = StructuralValue.FromCollection(list.Select(static record => StructuralValue.FromRecord(
            record.Select(static field => new KeyValuePair<string, StructuralValue>(field.Key, StructuralValue.FromScalar(field.Value))))));
        var hosts = new object[] { list, array, structural };
        string[]? expected = null;

        foreach (var host in hosts)
        {
            compiled.Parameters["patterns"] = host;
            StructuredInputSourceCounters.Reset();
            using var table = compiled.Run();
            var actual = table.Rows.Select(static row => $"{row[0]}:{row[1]}").ToArray();
            expected ??= actual;
            CollectionAssert.AreEqual(expected, actual, $"Host representation {host.GetType().Name} changed the normalized snapshot.");
            Assert.AreEqual(1, StructuredInputSourceCounters.MatchConstructed);
        }
    }

    private static PatternSpec[] CreateSpecs(int index, Random random)
    {
        var count = 1 + index % 3;
        var result = new PatternSpec[count];
        if (count >= 1)
            result[0] = new PatternSpec($"todo-{index}", "TODO", "TODO");
        if (count >= 2)
            result[1] = new PatternSpec($"fixme-{index}", "FIXME", "FIXME");
        if (count >= 3)
            result[2] = new PatternSpec($"issue-{index}-{random.Next(1, 100_000)}", "ISSUE-[0-9]+", "ISSUE-42");
        return result;
    }

    private static ExecutionCase CreateExecutionCase(
        InputOrigin origin,
        CteOperator @operator,
        SourceContext context,
        int modeIndex,
        int index,
        IReadOnlyList<PatternSpec> specs)
    {
        var text = $"TODO FIXME ISSUE-42 case-{index}";
        var argument = origin switch
        {
            InputOrigin.Host => "$patterns",
            InputOrigin.Inline => $"array {{ {FormatInlineRecords(specs)} }}",
            InputOrigin.Let => "$patterns",
            InputOrigin.Cte => "patterns",
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };

        var declaration = origin switch
        {
            InputOrigin.Host => $"param(patterns: (Id: string, Pattern: string, Mode: string = 'literal')[]) ",
            InputOrigin.Let => $"let patterns = array {{ {FormatInlineRecords(specs)} }}; ",
            InputOrigin.Cte => CreateCteDeclaration(@operator, specs, index),
            _ => string.Empty
        };

        var coupling = context == SourceContext.Coupled
            ? "table MatchRows { PatternId: string, MatchText: string, Offset: int }; couple #inputs.match with table MatchRows as Matches; "
            : string.Empty;
        var sourceName = context == SourceContext.Coupled ? "Matches" : "#inputs.match";
        var sourceCall = $"{sourceName}('{text}', patterns: {argument})";
        var projection = @operator == CteOperator.Window
            ? "m.PatternId, m.MatchText, RowNumber() over (order by m.Offset) as RowNo"
            : @operator == CteOperator.Distinct
                ? "distinct m.PatternId, m.MatchText"
                : "m.PatternId, m.MatchText";
        var suffix = origin == InputOrigin.Cte
            ? string.Empty
            : CreateResultModifier(@operator);
        var query = coupling + declaration + context switch
        {
            SourceContext.From => $"select {projection} from {sourceCall} m{suffix}",
            SourceContext.CrossApply => $"select {projection} from #inputs.numbers(values: array {{ 0 }}) d cross apply {sourceCall} m{suffix}",
            SourceContext.CrossJoin => $"select {projection} from #inputs.numbers(values: array {{ 0 }}) d cross join {sourceCall} m{suffix}",
            SourceContext.Coupled => $"select {projection} from {sourceCall} m{suffix}",
            _ => throw new ArgumentOutOfRangeException(nameof(context))
        };

        return new ExecutionCase(
            query,
            CreateExpected(specs, origin, @operator),
            origin.ToString(),
            @operator.ToString(),
            context.ToString(),
            ModeName(modeIndex),
            OptionsForMode(modeIndex),
            origin == InputOrigin.Host
                ? index % 2 == 0 ? CreateListHost(specs, index) : CreateArrayHost(specs, index)
                : null);
    }

    private static string FormatInlineRecords(IReadOnlyList<PatternSpec> specs)
    {
        return string.Join(", ", specs.Select(static spec =>
            spec.MatchText.StartsWith("ISSUE", StringComparison.Ordinal)
                ? $"(Pattern: '{spec.Pattern}', Id: '{spec.Id}', Mode: 'regex')"
                : $"(Id: '{spec.Id}', Pattern: '{spec.Pattern}')"));
    }

    private static string CreateCteDeclaration(CteOperator @operator, IReadOnlyList<PatternSpec> specs, int index)
    {
        var rows = FormatCteRecords(specs);
        var baseRows = $"from values {{ {rows} }} p";
        return @operator switch
        {
            CteOperator.Filter => $"with patterns as (select p.Id, p.Pattern, p.Mode {baseRows} where p.Id = '{specs[0].Id}') ",
            CteOperator.OrderAsc => $"with patterns as (select p.Id, p.Pattern, p.Mode {baseRows} order by p.Id) ",
            CteOperator.Distinct => $"with patterns as (select distinct p.Id, p.Pattern, p.Mode {baseRows}) ",
            CteOperator.Group => $"with patterns as (select p.Id, p.Pattern, p.Mode {baseRows} group by p.Id, p.Pattern, p.Mode order by p.Id) ",
            CteOperator.Set => $"with patterns as (select p.Id, p.Pattern, p.Mode {baseRows} union all (Id, Pattern, Mode) select q.Id, q.Pattern, q.Mode from values {{ {rows} }} q) ",
            CteOperator.Page => $"with patterns as (select p.Id, p.Pattern, p.Mode {baseRows} order by p.Id take 1) ",
            CteOperator.Window => $"with patterns as (select p.Id, p.Pattern, p.Mode {baseRows}) ",
            CteOperator.Plain => $"with patterns as (select p.Id, p.Pattern, p.Mode {baseRows}) ",
            _ => throw new ArgumentOutOfRangeException(nameof(@operator), @operator, null)
        };
    }

    private static string FormatCteRecords(IReadOnlyList<PatternSpec> specs)
    {
        return string.Join(", ", specs.Select(static spec =>
            $"(Id: '{spec.Id}', Pattern: '{spec.Pattern}', Mode: '{(spec.MatchText.StartsWith("ISSUE", StringComparison.Ordinal) ? "regex" : "literal")}')"));
    }

    private static string CreateResultModifier(CteOperator @operator) => @operator switch
    {
        CteOperator.Plain => string.Empty,
        CteOperator.Filter => " where m.Offset >= 0",
        CteOperator.OrderAsc => " order by m.PatternId",
        CteOperator.Distinct => string.Empty,
        CteOperator.Group => " group by m.PatternId, m.MatchText order by m.PatternId",
        CteOperator.Set => " skip 0",
        CteOperator.Page => " order by m.PatternId take 1",
        CteOperator.Window => string.Empty,
        _ => throw new ArgumentOutOfRangeException(nameof(@operator), @operator, null)
    };

    private static string[] CreateExpected(
        IReadOnlyList<PatternSpec> specs,
        InputOrigin origin,
        CteOperator @operator)
    {
        IEnumerable<PatternSpec> ordered = specs;
        if (origin == InputOrigin.Cte)
        {
            ordered = @operator switch
            {
                CteOperator.Filter => specs.Take(1),
                CteOperator.OrderAsc or CteOperator.Group => specs.OrderBy(static spec => spec.Id, StringComparer.Ordinal),
                CteOperator.Page => specs.OrderBy(static spec => spec.Id, StringComparer.Ordinal).Take(1),
                CteOperator.Distinct or CteOperator.Window or CteOperator.Plain => specs,
                CteOperator.Set => specs.Concat(specs),
                _ => specs
            };
        }
        else
        {
            ordered = @operator switch
            {
                CteOperator.OrderAsc or CteOperator.Group => specs.OrderBy(static spec => spec.Id, StringComparer.Ordinal),
                CteOperator.Page => specs.OrderBy(static spec => spec.Id, StringComparer.Ordinal).Take(1),
                _ => specs
            };
        }

        return ordered.Select(static spec => $"{spec.Id}:{spec.MatchText}").ToArray();
    }

    private static CompilationOptions OptionsForMode(int modeIndex)
    {
        var options = modeIndex switch
        {
            0 => new CompilationOptions(
                parallelizationMode: ParallelizationMode.None,
                useCteParallelization: false,
                usePrimitiveTypeValidation: false),
            1 => new CompilationOptions(
                parallelizationMode: ParallelizationMode.Full,
                useCteParallelization: false,
                usePrimitiveTypeValidation: false),
            2 => new CompilationOptions(
                parallelizationMode: ParallelizationMode.Full,
                useCteParallelization: true,
                usePrimitiveTypeValidation: false),
            3 => new CompilationOptions(
                parallelizationMode: ParallelizationMode.None,
                useCteParallelization: false,
                usePrimitiveTypeValidation: false),
            _ => throw new ArgumentOutOfRangeException(nameof(modeIndex))
        };

        return modeIndex == 2 ? options.WithStabilityAwareScalarReuse() : options;
    }

    private static string ModeName(int modeIndex) => modeIndex switch
    {
        0 => "serial-minimal",
        1 => "parallel-no-cte",
        2 => "parallel-cte-stable",
        3 => "serial-cte-no-cse",
        _ => throw new ArgumentOutOfRangeException(nameof(modeIndex))
    };

    private static string HashNormalizedQuery(string query)
    {
        var builder = new StringBuilder(query.Length);
        var inString = false;
        var pendingSpace = false;
        for (var index = 0; index < query.Length; index++)
        {
            var character = query[index];
            if (character == '\'' && (index == 0 || query[index - 1] != '\\'))
            {
                if (pendingSpace && builder.Length > 0)
                    builder.Append(' ');
                pendingSpace = false;
                inString = !inString;
                builder.Append(character);
                continue;
            }

            if (!inString && char.IsWhiteSpace(character))
            {
                pendingSpace = true;
                continue;
            }

            if (pendingSpace && builder.Length > 0)
                builder.Append(' ');
            pendingSpace = false;
            builder.Append(character);
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString().Trim())));
    }

    private static List<Dictionary<string, object?>> CreateListHost(
        IReadOnlyList<PatternSpec> specs,
        int caseIndex)
    {
        var result = new List<Dictionary<string, object?>>(specs.Count);
        for (var index = 0; index < specs.Count; index++)
            result.Add(CreateHostRecord(specs[index], caseIndex, index));
        return result;
    }

    private static Dictionary<string, object?>[] CreateArrayHost(
        IReadOnlyList<PatternSpec> specs,
        int caseIndex)
    {
        var result = new Dictionary<string, object?>[specs.Count];
        for (var index = 0; index < specs.Count; index++)
            result[index] = CreateHostRecord(specs[index], caseIndex, index);
        return result;
    }

    private static Dictionary<string, object?> CreateHostRecord(
        PatternSpec spec,
        int caseIndex,
        int patternIndex)
    {
        var fields = new Dictionary<string, object?>(
            patternIndex % 2 == 0 ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);
        if ((caseIndex + patternIndex) % 2 == 0)
        {
            fields[patternIndex % 3 == 0 ? "ID" : "id"] = spec.Id;
            fields[patternIndex % 3 == 1 ? "PATTERN" : "Pattern"] = spec.Pattern;
        }
        else
        {
            fields[patternIndex % 3 == 0 ? "Pattern" : "pattern"] = spec.Pattern;
            fields[patternIndex % 3 == 1 ? "ID" : "Id"] = spec.Id;
        }

        if (patternIndex == 2 || (caseIndex + patternIndex) % 5 == 0)
            fields[patternIndex % 2 == 0 ? "Mode" : "mode"] = patternIndex == 2 ? "regex" : "literal";

        return fields;
    }

    private static CompiledQuery Compile(
        string query,
        string queryId,
        CompilationOptions? options = null)
    {
        var build = InstanceCreator.CompileWithDiagnostics(
            query,
            queryId,
            new StructuredInputsSchemaProvider(),
            new NullLoggerResolver(),
            options ?? new CompilationOptions(usePrimitiveTypeValidation: false));
        var details = build.CaughtException?.ToString() ??
            string.Join(Environment.NewLine, build.Diagnostics.Select(static diagnostic => diagnostic.ToDetailedString()));
        Assert.IsTrue(build.Succeeded && build.CompiledQuery != null, details + Environment.NewLine + query);
        return build.CompiledQuery!;
    }

    private readonly record struct PatternSpec(string Id, string Pattern, string MatchText);

    private readonly record struct ExecutionCase(
        string Query,
        string[] Expected,
        string Origin,
        string Operator,
        string Context,
        string Mode,
        CompilationOptions Options,
        object? Host);

    private enum InputOrigin
    {
        Host,
        Inline,
        Let,
        Cte
    }

    private enum CteOperator
    {
        Plain,
        Filter,
        OrderAsc,
        Distinct,
        Group,
        Set,
        Page,
        Window
    }

    private enum SourceContext
    {
        From,
        CrossApply,
        CrossJoin,
        Coupled
    }

    private sealed class NullLoggerResolver : ILoggerResolver
    {
        public ILogger ResolveLogger() => NullLogger.Instance;

        public ILogger<T> ResolveLogger<T>() => NullLogger<T>.Instance;
    }
}
