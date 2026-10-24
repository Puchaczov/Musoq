using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Exceptions;
using Musoq.Schema;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Tests;

/// <summary>Investigation characterizations, not acceptance tests for future corrected behavior.</summary>
[TestClass]
public sealed class ReportedQueryInvestigationTests : BinaryOrTextualEvaluatorTestBase
{
    public TestContext TestContext { get; set; } = null!;

    public static IEnumerable<object[]> Cases =>
    [
        Case("01-no-cse", $"select Trim(Max(case when x.Value = 1 then 'a' else null end)) as A, Trim(Max(case when x.Value = 2 then 'b' else null end)) as B from {InlineRange(0, 3)} x", "success", "a|b"),
        Case("01-no-licm", $"select Trim(Max(case when x.Value = 1 then 'a' else null end)) as A, Trim(Max(case when x.Value = 2 then 'b' else null end)) as B from {InlineRange(0, 3)} x", "success", "a|b"),
        Case("01-no-reuse", $"select Trim(Max(case when x.Value = 1 then 'a' else null end)) as A, Trim(Max(case when x.Value = 2 then 'b' else null end)) as B from {InlineRange(0, 3)} x", "success", "a|b"),
        Case("02-range-column", $"select x.Value as K from {InlineRange(1, 2)} x union all (K) select null as K from values {{ (Dummy: 'x') }} d", "success", "1;null"),
        Case("02-range-computed", $"select ToDouble(x.Value) / 8 as K from {InlineRange(1, 2)} x union all (K) select null as K from values {{ (Dummy: 'x') }} d", "success", "0.125;null"),
        Case("03-count", $"select Count(*) as N from {InlineRange(0, 3000)} x", "success", "3000"),
        Case("01-original", $"select Trim(Max(case when x.Value = 1 then 'a' else null end)) as A, Trim(Max(case when x.Value = 2 then 'b' else null end)) as B, from {InlineRange(0, 3)} x", "compile:MQ2014_TrailingComma"),
        Case("01-no-trailing-comma", $"select Trim(Max(case when x.Value = 1 then 'a' else null end)) as A, Trim(Max(case when x.Value = 2 then 'b' else null end)) as B from {InlineRange(0, 3)} x", "success", "a|b"),
        Case("01-no-trim", $"select Max(case when x.Value = 1 then 'a' else null end) as A, Max(case when x.Value = 2 then 'b' else null end) as B from {InlineRange(0, 3)} x", "success", "a|b"),
        Case("01-single", $"select Trim(Max(case when x.Value = 1 then 'a' else null end)) as A from {InlineRange(0, 3)} x", "success", "a"),
        Case("01-different-text", $"select Trim(Max(case when x.Value = 1 then ' a ' else null end)) as A, Trim(Max(case when x.Value = 2 then ' b ' else null end)) as B from {InlineRange(0, 3)} x", "success", "a|b"),
        Case("02-original", "binary R { K: byte } select r.K as K from values { (Dummy: 'x') } d cross apply Interpret<R>(FromHexToBytes('01')) r union all (K) select null as K from values { (Dummy: 'x') } d", "success", "1;null"),
        Case("02-null-first", "binary R { K: byte } with seed as (select d.Dummy from values { (Dummy: 'x') } d) select null as K from seed d union all (K) select r.K as K from seed d cross apply Interpret<R>(FromHexToBytes('01')) r", "success", "null;1"),
        Case("02-plain-value", "select ToByte(1) as K from values { (Dummy: 'x') } d union all (K) select null as K from values { (Dummy: 'x') } d", "success", "1;null"),
        Case("02-computed", "binary R { K: byte } select ToDouble(r.K) / 8 as K from values { (Dummy: 'x') } d cross apply Interpret<R>(FromHexToBytes('01')) r union all (K) select null as K from values { (Dummy: 'x') } d", "success", "0.125;null"),
        Case("02-typed-null", "binary R { K: byte } select r.K as K from values { (Dummy: 'x') } d cross apply Interpret<R>(FromHexToBytes('01')) r union all (K) select case when 1 = 0 then ToByte(0) else null end as K from values { (Dummy: 'x') } d", "success", "1;null"),
        Case("03-original", $"select Sum(x.Value * 1000000) as S from {InlineRange(0, 3000)} x", "runtime:MQ7018_NumericOverflow"),
        Case("03-aggregate-only-overflow", $"select Sum(x.Value * 1000000) as S from {InlineRange(0, 100)} x", "runtime:MQ7018_NumericOverflow"),
        Case("03-long-before-multiply", $"select Sum((x.Value::long) * 1000000) as S from {InlineRange(0, 3000)} x", "success", "4498500000000"),
        Case("03-long-after-multiply", $"select Sum(((x.Value * 1000000)::long)) as S from {InlineRange(0, 3000)} x", "success", "839187863808"),
        Case("04-original", "select case when d.Dummy = 'x' then 'a' end as A from values { (Dummy: 'x') } d", "success", "a"),
        Case("04-explicit-else", "select case when d.Dummy = 'x' then 'a' else null end as A from values { (Dummy: 'x') } d", "success", "a"),
        Case("05-original", "select 'a' || 'b' as a from values { (Dummy: 'x') } d", "success", "ab"),
        Case("05-plus", "select 'a' + 'b' as a from values { (Dummy: 'x') } d", "success", "ab"),
        Case("06-original", "select ToDouble(1) / 10.0 as a from values { (Dummy: 'x') } d", "compile:MQ3007_InvalidOperandTypes"),
        Case("06-int-decimal", "select 1 / 10.0 as a from values { (Dummy: 'x') } d", "success", "0.1"),
        Case("06-explicit-double", "select ToDouble(1) / ToDouble(10.0) as a from values { (Dummy: 'x') } d", "success", "0.1"),
        Case("07-repeat-10000", "binary Rec { X: byte } binary S { Records: Rec repeat until eof } select Count(r.X) as N from #test.files() f cross apply Interpret<S>(f.Content) s cross apply s.Records r", "success", "10000", 10000),
        Case("07-repeat-10001", "binary Rec { X: byte } binary S { Records: Rec repeat until eof } select Count(r.X) as N from #test.files() f cross apply Interpret<S>(f.Content) s cross apply s.Records r", "runtime:MQ7109_InterpretationMaxIterationsExceeded", bytes: 10001),
        Case("07-fixed-10001", "binary Rec { X: byte } binary S { Records: Rec[10001] } select Count(r.X) as N from #test.files() f cross apply Interpret<S>(f.Content) s cross apply s.Records r", "success", "10001", 10001),
        Case("07-recursive-default", "with recursive n as (select 0 as V from values { (Dummy: 'x') } d union all select n.V + 1 as V from n where n.V < 1001) select Count(*) as N from n", "runtime:MQ7007_RecursiveCteIterationLimitExceeded"),
        Case("07-recursive-raised", "with recursive n as (select 0 as V from values { (Dummy: 'x') } d union all select n.V + 1 as V from n where n.V < 1001) select Count(*) as N from n", "success", "1002", recursiveLimit: 2000)
    ];

    [TestMethod]
    [DynamicData(nameof(Cases))]
    public void ReportedQuery_ShouldRecordCurrentBehavior(string name, string sql, string expectedOutcome,
        string? expectedValues, int bytes, int recursiveLimit)
    {
        var observation = Observe(name, sql, bytes, recursiveLimit);
        TestContext.WriteLine($"{name}\nSQL: {sql}\n{observation}");
        Assert.AreEqual(expectedOutcome, observation.Outcome, observation.ToString());
        AssertExpectedValues(expectedValues, observation);
    }

    private Observation Observe(string name, string sql, int bytes, int recursiveLimit)
    {
        var options = CreateOptions(name, recursiveLimit);
        var provider = new BinarySchemaProvider(new Dictionary<string, IEnumerable<BinaryEntity>>
        {
            ["#test"] = [new BinaryEntity { Content = new byte[bytes] }]
        });
        CaptureInspection(name, sql, provider, options);
        CompiledQuery query;
        try
        {
            query = InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString(), provider, LoggerResolver, options);
        }
        catch (MusoqQueryException exception)
        {
            return new Observation($"compile:{exception.PrimaryEnvelope?.Code}", null, null, exception.ToString());
        }

        using (query)
            return Execute(query);
    }

    private Observation Execute(CompiledQuery query)
    {
        try
        {
            using var table = query.Run(TestContext.CancellationToken);
            var values = string.Join(";", table.Select(row => string.Join("|", row.ReadValues().Select(FormatValue))));
            var types = string.Join("|", table.Columns.Select(column => column.ColumnType.ToString()));
            return new Observation("success", values, types, null);
        }
        catch (ParseException exception)
        {
            return new Observation($"runtime:{exception.ToDiagnostic().Code}", null, null, exception.ToString());
        }
        catch (RecursiveCteLimitExceededException exception)
        {
            return new Observation($"runtime:{exception.Code}", null, null, exception.ToString());
        }
        catch (QueryExecutionException exception)
        {
            return new Observation($"runtime:{exception.Envelope?.Code}", null, null, exception.ToString());
        }
    }

    private static CompilationOptions CreateOptions(string name, int recursiveLimit) =>
        new CompilationOptions(useCommonSubexpressionElimination: !name.EndsWith("no-cse", StringComparison.Ordinal))
            .WithRecursiveCteLimits(new RecursiveCteExecutionLimits(recursiveLimit))
            .WithLoopInvariantCodeMotion(!name.EndsWith("no-licm", StringComparison.Ordinal))
            .WithStabilityAwareScalarReuse(!name.EndsWith("no-reuse", StringComparison.Ordinal));

    private void CaptureInspection(string name, string sql, ISchemaProvider provider, CompilationOptions options)
    {
        if (name is not ("01-no-trailing-comma" or "02-original" or "03-original" or "02-typed-null"))
            return;

        var inspection = InstanceCreator.CompileForInspection(sql, Guid.NewGuid().ToString(), provider, LoggerResolver, options);
        TestContext.WriteLine($"INSPECTION {name}\nLOGICAL\n{inspection.LogicalPlanText}\nPHYSICAL\n{inspection.PhysicalPlanText}\nEXECUTION\n{inspection.ExecutionPlanText}\nCSHARP\n{inspection.GeneratedCSharpCode}\nWARNINGS\n{string.Join("\n", inspection.Warnings.Select(warning => warning.ToString()))}\nEND INSPECTION");
    }

    private static void AssertExpectedValues(string? expectedValues, Observation observation)
    {
        if (expectedValues == null)
            return;

        Assert.AreEqual(expectedValues, observation.Values, observation.ToString());
    }

    private static string FormatValue(object? value)
    {
        if (value == null)
            return "null";

        return Convert.ToString(value, CultureInfo.InvariantCulture)!;
    }

    private static object[] Case(string name, string sql, string outcome, string? values = null,
        int bytes = 0, int recursiveLimit = 1000) => [name, sql, outcome, values!, bytes, recursiveLimit];

    private static string InlineRange(int start, int end) =>
        "values { " + string.Join(", ", Enumerable.Range(start, end - start)
            .Select(value => $"(Value: {value.ToString(CultureInfo.InvariantCulture)})")) + " }";

    private sealed record Observation(string Outcome, string? Values, string? Types, string? Exception);
}
