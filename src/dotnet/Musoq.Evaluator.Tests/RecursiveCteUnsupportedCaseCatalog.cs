using System.Collections.Generic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public sealed record RecursiveCteExpectedDiagnostic(
    DiagnosticCode Code,
    string SpanFragment,
    string MessageFragment);

public sealed record RecursiveCteUnsupportedCase(
    string Name,
    string Query,
    DiagnosticCode DiagnosticCode,
    string SpanFragment,
    string MessageFragment,
    IReadOnlyList<RecursiveCteExpectedDiagnostic>? ExpectedDiagnostics = null)
{
    public bool ParserRecovery => ExpectedDiagnostics is not null;
}

internal static partial class RecursiveCteUnsupportedCaseCatalog
{
    public static IReadOnlyList<RecursiveCteUnsupportedCase> Cases { get; } =
    [
        .. CreateFocusedCases(),
        .. CreateHardeningCases()
    ];

    private static IReadOnlyList<RecursiveCteUnsupportedCase> CreateFocusedCases() =>
    [
        Case(
            "MissingRecursiveKeyword",
            "with counter (Value) as (" + Anchor + " union all " + Member + ") select Value from counter",
            DiagnosticCode.MQ3072_RecursiveCteRequiresKeyword,
            "counter",
            "requires WITH RECURSIVE"),
        Case(
            "MissingKeywordPrecedesUnsupportedMember",
            "with counter (Value) as (" + Anchor +
            " union all select distinct c.Value + 1 from counter c) select Value from counter",
            DiagnosticCode.MQ3072_RecursiveCteRequiresKeyword,
            "counter c",
            "requires WITH RECURSIVE"),
        Case(
            "NoTopLevelUnion",
            "with recursive counter (Value) as (select c.Value from counter c) select Value from counter",
            DiagnosticCode.MQ3073_InvalidRecursiveCteShape,
            "counter",
            "top-level UNION"),
        Case(
            "SelfReferenceInAnchor",
            "with recursive counter (Value) as (select c.Value from counter c union all " + Member +
            ") select Value from counter",
            DiagnosticCode.MQ3074_InvalidRecursiveCteReference,
            "counter c",
            "anchor"),
        Case(
            "MultipleSelfReferences",
            "with recursive counter (Value) as (" + Anchor +
            " union all select a.Value + b.Value from counter a inner join counter b on a.Value = b.Value" +
            ") select Value from counter",
            DiagnosticCode.MQ3074_InvalidRecursiveCteReference,
            "counter a",
            "exactly once"),
        Case(
            "NestedSelfReference",
            "with recursive counter (Value) as (" + Anchor +
            " union all select seed.Value from values {{ Value: 1 }} seed " +
            "where exists (select c.Value from counter c)) select Value from counter",
            DiagnosticCode.MQ3074_InvalidRecursiveCteReference,
            "counter c",
            "nested query"),
        Case(
            "ForwardReference",
            "with recursive first (Value) as (select s.Value from second s), " +
            "second (Value) as (" + Anchor + ") select Value from first",
            DiagnosticCode.MQ3074_InvalidRecursiveCteReference,
            "second s",
            "forward CTE 'second'"),
        Case(
            "MutualRecursion",
            "with recursive first (Value) as (select s.Value from second s), " +
            "second (Value) as (select f.Value from first f) select Value from first",
            DiagnosticCode.MQ3074_InvalidRecursiveCteReference,
            "second s",
            "forward CTE 'second'"),
        Case(
            "UnionAllWithKeys",
            "with recursive counter (Value) as (" + Anchor + " union all (Value) " + Member +
            ") select Value from counter",
            DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator,
            "union all",
            "UNION ALL (keys)"),
        Case(
            "DistinctRecursiveMember",
            "with recursive counter (Value) as (" + Anchor +
            " union all select distinct c.Value + 1 from counter c where c.Value < 3" +
            ") select Value from counter",
            DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator,
            "Value + 1",
            "DISTINCT"),
        Case(
            "GroupedRecursiveMember",
            "with recursive counter (Value) as (" + Anchor +
            " union all select c.Value + 1 from counter c group by c.Value" +
            ") select Value from counter",
            DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator,
            "Value",
            "GROUP BY"),
        Case(
            "OrderedRecursiveMember",
            "with recursive counter (Value) as (" + Anchor + " union all " + Member +
            " order by c.Value) select Value from counter",
            DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator,
            "Value",
            "ORDER BY"),
        Case(
            "OuterJoinRecursiveMember",
            "with recursive counter (Value) as (" + Anchor +
            " union all select c.Value + 1 from counter c left outer join values {{ Value: 1 }} seed " +
            "on c.Value = seed.Value) select Value from counter",
            DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator,
            "counter c",
            "OuterLeft join"),
        Case(
            "NestedSetOperation",
            "with recursive counter (Value) as (" + Anchor + " union all " + Member +
            " union all select seed.Value from values {{ Value: 4 }} seed) select Value from counter",
            DiagnosticCode.MQ3073_InvalidRecursiveCteShape,
            "counter",
            "exactly one anchor"),
        Case(
            "RecursiveOutputColumnCountMismatch",
            "with recursive counter (Value) as (" + Anchor +
            " union all select c.Value + 1, c.Value from counter c where c.Value < 3" +
            ") select Value from counter",
            DiagnosticCode.MQ3076_RecursiveCteOutputMismatch,
            "Value + 1",
            "anchor projects 1 column(s), but its recursive member projects 2"),
        Case(
            "RecursiveOutputTypeMismatch",
            "with recursive counter (Value) as (" + Anchor +
            " union all select (c.Value + 1)::Decimal from counter c where c.Value < 3" +
            ") select Value from counter",
            DiagnosticCode.MQ3076_RecursiveCteOutputMismatch,
            "Decimal",
            "anchor type 'Int32'")
    ];

    private const string Anchor = "select seed.Value from values {{ Value: 1 }} seed";

    private const string Member = "select c.Value + 1 from counter c where c.Value < 3";

    private static RecursiveCteUnsupportedCase Case(
        string name,
        string query,
        DiagnosticCode code,
        string spanFragment,
        string messageFragment)
    {
        return new RecursiveCteUnsupportedCase(name, query, code, spanFragment, messageFragment);
    }
}
