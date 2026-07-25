using System.Collections.Generic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

internal static partial class RecursiveCteUnsupportedCaseCatalog
{
    private static IReadOnlyList<RecursiveCteUnsupportedCase> CreateHardeningCases()
    {
        return
        [
            Unsupported("HavingRecursiveMember",
                MemberWith("select c.Value + 1 from counter c group by c.Value having c.Value < 3"),
                DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator, "Value", "HAVING"),
            Unsupported("WindowRecursiveMember",
                MemberWith("select RowNumber() over ranked from counter c " +
                           "window ranked as (order by c.Value)"),
                DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator, "Value", "WINDOW"),
            Unsupported("QualifyRecursiveMember",
                MemberWith("select c.Value + 1 from counter c " +
                           "qualify RowNumber() over (order by c.Value) = 1"),
                DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator, "RowNumber", "QUALIFY"),
            Unsupported("SkipRecursiveMember",
                MemberWith("select c.Value + 1 from counter c skip 1"),
                DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator, "1", "pagination"),
            Unsupported("TakeRecursiveMember",
                MemberWith("select c.Value + 1 from counter c take 1"),
                DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator, "1", "pagination"),
            Unsupported("RightOuterJoinRecursiveMember",
                JoinMember("right outer join"),
                DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator, "counter c", "OuterRight join"),
            Unsupported("FullOuterJoinRecursiveMember",
                JoinMember("full outer join"),
                DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator, "counter c", "OuterFull join"),
            Unsupported("SemiJoinRecursiveMember",
                JoinMember("semi join"),
                DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator, "counter c", "LeftSemi join"),
            Unsupported("AntiJoinRecursiveMember",
                JoinMember("anti join"),
                DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator, "counter c", "LeftAntiSemi join"),
            Unsupported("AsOfJoinRecursiveMember",
                "with recursive counter (Value) as (" + Anchor + " union all " +
                "select c.Value + 1 from counter c asof join values {{ Value: 1 }} seed " +
                "on c.Value >= seed.Value) select Value from counter",
                DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator, "counter c", "AsOf join"),
            Unsupported("UnpivotRecursiveMember",
                "with recursive counter (Q1) as (" + Anchor + " union all " +
                "unpivot counter c on Name in (c.Q1 as Q1) using Value) select Q1 from counter",
                DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator, "counter c", "UNPIVOT"),
            Unsupported("AggregateRecursiveMember",
                MemberWith("select Count(c.Value) from counter c"),
                DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator, "Count", "aggregation"),
            Unsupported("DistinctAggregateRecursiveMember",
                MemberWith("select Count(distinct c.Value) from counter c"),
                DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator, "Count", "aggregation"),
            Unsupported("AnchorReferencePrecedesDistinct",
                "with recursive counter (Value) as (select c.Value from counter c union all " +
                "select distinct c.Value + 1 from counter c) select Value from counter",
                DiagnosticCode.MQ3074_InvalidRecursiveCteReference, "counter c", "anchor"),
            Unsupported("MultipleReferencesPrecedeOrdering",
                "with recursive counter (Value) as (" + Anchor + " union all " +
                "select a.Value + b.Value from counter a inner join counter b on a.Value = b.Value " +
                "order by a.Value) select Value from counter",
                DiagnosticCode.MQ3074_InvalidRecursiveCteReference, "counter a", "exactly once"),
            Unsupported("UnionAllKeysPrecedeGrouping",
                "with recursive counter (Value) as (" + Anchor + " union all (Value) " +
                "select c.Value + 1 from counter c group by c.Value) select Value from counter",
                DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator, "union all", "UNION ALL (keys)"),
            Unsupported("DistinctPrecedesTypeMismatch",
                "with recursive counter (Value) as (" + Anchor + " union all " +
                "select distinct (c.Value + 1)::Decimal from counter c) select Value from counter",
                DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator, "Value + 1", "DISTINCT"),
            Unsupported("OrderingPrecedesColumnCountMismatch",
                "with recursive counter (Value) as (" + Anchor + " union all " +
                "select c.Value + 1, c.Value from counter c order by c.Value) select Value from counter",
                DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator, "Value", "ORDER BY"),
            Unsupported("RecursiveColumnListCountMismatch",
                "with recursive counter (Value, Extra) as (" + Anchor + " union all " + Member +
                ") select Value from counter",
                DiagnosticCode.MQ3077_CteColumnListCountMismatch, "Value", "declares 2 column name"),
            Unsupported("RecursiveDuplicateColumnName",
                "with recursive counter (Value, Value) as (" +
                "select seed.Value, seed.Value from values {{ Value: 1 }} seed union all " +
                "select c.Value + 1, c.Value + 1 from counter c where c.Value < 3) " +
                "select Value from counter",
                DiagnosticCode.MQ3078_DuplicateCteColumnName, "Value", "duplicate column name"),
            Unsupported("RecursiveCaseOnlyDuplicateColumnName",
                "with recursive counter (Value, value) as (" +
                "select seed.Value, seed.Value from values {{ Value: 1 }} seed union all " +
                "select c.Value + 1, c.Value + 1 from counter c where c.Value < 3) " +
                "select Value from counter",
                DiagnosticCode.MQ3078_DuplicateCteColumnName, "value", "duplicate column name"),
            Unsupported("OrdinaryColumnListCountInsideRecursiveWith",
                "with recursive seed (Value, Extra) as (select 1 from values {{ Seed: 1 }} s) " +
                "select 1 from values {{ Seed: 1 }} live",
                DiagnosticCode.MQ3077_CteColumnListCountMismatch, "Value", "declares 2 column name"),
            Unsupported("OrdinaryDuplicateColumnInsideRecursiveWith",
                "with recursive seed (Value, Value) as (select 1, 2 from values {{ Seed: 1 }} s) " +
                "select 1 from values {{ Seed: 1 }} live",
                DiagnosticCode.MQ3078_DuplicateCteColumnName, "Value", "duplicate column name"),
            Unsupported("OrdinaryCaseOnlyDuplicateColumnName",
                "with recursive seed (Value, value) as (select 1, 2 from values {{ Seed: 1 }} s) " +
                "select 1 from values {{ Seed: 1 }} live",
                DiagnosticCode.MQ3078_DuplicateCteColumnName, "value", "duplicate column name"),
            Unsupported("UnknownRecursiveUnionKey",
                "with recursive counter (Value) as (" + Anchor + " union (Missing) " + Member +
                ") select Value from counter",
                DiagnosticCode.MQ3001_UnknownColumn, "union", "Unknown column 'Missing'"),
            Unsupported("RecursiveKeyMatchesOnlyUnderlyingExpression",
                "with recursive counter (Id) as (" + Anchor + " union (Value) " +
                "select c.Id + 1 from counter c where c.Id < 3) select Id from counter",
                DiagnosticCode.MQ3001_UnknownColumn, "union", "Unknown column 'Value'"),
            Unsupported("ForwardReferenceInsideRecursiveMember",
                "with recursive counter (Value) as (" + Anchor + " union all " +
                "select n.Value from counter c inner join next n on n.Value = c.Value), " +
                "next (Value) as (" + Anchor + ") select Value from counter",
                DiagnosticCode.MQ3074_InvalidRecursiveCteReference, "next n", "forward CTE 'next'"),
            Unsupported("ThreeDefinitionMutualRecursion",
                "with recursive first (Value) as (select t.Value from third t), " +
                "second (Value) as (select f.Value from first f), " +
                "third (Value) as (select s.Value from second s) select Value from first",
                DiagnosticCode.MQ3074_InvalidRecursiveCteReference, "third t", "forward CTE 'third'"),
            Unsupported("ScalarSubquerySelfReference",
                "with recursive counter (Value) as (" + Anchor + " union all " +
                "select (select c.Value from counter c) from values {{ Seed: 1 }} seed) " +
                "select Value from counter",
                DiagnosticCode.MQ3074_InvalidRecursiveCteReference, "counter c", "nested query"),
            Unsupported("ApplyAndSecondSelfReference",
                "with recursive counter (Value) as (" + Anchor + " union all " +
                "select a.Value + b.Value from counter a cross apply counter b) select Value from counter",
                DiagnosticCode.MQ3074_InvalidRecursiveCteReference, "counter a", "exactly once"),
            Unsupported("NestedChainedUnionAll",
                "with recursive counter (Value) as (" + Anchor + " union all " + Member +
                " union all select c.Value + 2 from counter c) select Value from counter",
                DiagnosticCode.MQ3073_InvalidRecursiveCteShape, "counter", "exactly one anchor"),
            Unsupported("PivotRecursiveMember",
                "with recursive counter (Value) as (" + Anchor + " union all " +
                "pivot counter c on Value in (1 as One) using Count(Value) as Total) " +
                "select Value from counter",
                DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator, "Count", "aggregation"),
            Unsupported("SearchClauseParserRecovery",
                "with recursive counter (Value) as (" + Anchor + " union all " + Member +
                ") search depth first by Value set Ordinal select Value from counter",
                DiagnosticCode.MQ2030_UnsupportedSyntax, "search", "Cannot recognize", parserRecovery: true),
            Unsupported("CycleClauseParserRecovery",
                "with recursive counter (Value) as (" + Anchor + " union all " + Member +
                ") cycle Value set IsCycle select Value from counter",
                DiagnosticCode.MQ2030_UnsupportedSyntax, "cycle", "Cannot recognize", parserRecovery: true),
            Unsupported("MissingClosingParenthesisRecovery",
                "with recursive counter (Value) as (" + Anchor + " union all " + Member +
                " select Value from counter",
                DiagnosticCode.MQ2001_UnexpectedToken, "select", "Expected token is RightParenthesis", parserRecovery: true)
        ];
    }

    private static RecursiveCteUnsupportedCase Unsupported(
        string name,
        string query,
        DiagnosticCode code,
        string spanFragment,
        string messageFragment,
        bool parserRecovery = false)
    {
        return new RecursiveCteUnsupportedCase(
            name,
            query,
            code,
            spanFragment,
            messageFragment,
            parserRecovery
                ? [new RecursiveCteExpectedDiagnostic(code, spanFragment, messageFragment)]
                : null);
    }

    private static string MemberWith(string member)
    {
        return "with recursive counter (Value) as (" + Anchor + " union all " + member +
               ") select Value from counter";
    }

    private static string JoinMember(string join)
    {
        return "with recursive counter (Value) as (" + Anchor + " union all " +
               "select c.Value + 1 from counter c " + join + " values {{ Value: 1 }} seed " +
               "on c.Value = seed.Value) select Value from counter";
    }
}
