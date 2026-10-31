namespace Musoq.Benchmarks.Scenarios;

public static partial class ScenarioCatalog
{
    private static IEnumerable<Scenario> Analytics() =>
    [
        new("B13_Apply", "Composition", "Expand fixed two-element collections with ordinality.",
            """
            select r.Id as Id, i.Value as Value, i.Ordinal as Position
            from bench.rows() r
            cross apply r.Items i with ordinality
            order by r.Id, Position
            """, "core/9"),
        new("B13_OuterApply", "Composition", "Preserve rows when a correlated input is empty.",
            """
            select r.Id as Id, d.Id as Match
            from bench.rows() r
            outer apply (select e.Id, e.Key
            from bench.empty() e
            where e.Key = r.Key) d
            order by r.Id
            """, "core/9.7"),
        new("B14_Group", "Analytics", "Composite nullable grouping, filtered/distinct aggregates and HAVING.",
            """
            select r.Key as Id, Sum(r.Amount) as Total, Count(distinct r.Optional) filter (where r.Id % 2 = 0) as Present
            from bench.rows() r
            group by r.Key
            having Sum(r.Amount) > 0
            order by Id
            """, "core/10"),
        new("B14_WholeAggregate", "Analytics", "Aggregate-only strategy over empty and nonempty contributions.",
            "select Count(*) as Count, Sum(r.Amount) as Total, Avg(r.Amount) as Average from bench.rows() r", "core/10.5"),
        new("B14_Skewed", "Analytics", "Hot grouping key receives at least ninety percent of input rows.",
            """
            select r.Key as Id, Count(*) as Count, Sum(r.Amount) as Total
            from bench.skewed() r
            group by r.Key
            order by Id
            """, "core/10"),
        new("B15_RankingOffset", "Analytics", "Ranking and nullable neighbor access.",
            """
            select r.Id as Id, RowNumber() over (order by r.Id) as Position, Lag(r.Amount) over (order by r.Id) as Previous
            from bench.rows() r
            order by r.Id
            """, "core/11.5"),
        new("B16_RowsFrame", "Analytics", "Bounded sliding positional aggregate and value window.",
            """
            select r.Id as Id, Sum(r.Amount) over (order by r.Id rows between 1 preceding and current row) as Total, FirstValue(r.Amount) over (order by r.Id rows between 1 preceding and current row) as First
            from bench.rows() r
            order by r.Id
            """, "core/11.12"),
        new("B16_RangeFrame", "Analytics", "Peer-aware bounded numeric range aggregation.",
            """
            select r.Id as Id, Sum(r.Amount) over (order by r.Key range between 1 preceding and current row) as Total
            from bench.rows() r
            order by r.Id
            """, "core/11.12"),
        new("B17_MultipleWindows", "Analytics", "Shared and distinct window orderings followed by QUALIFY.",
            """
            select r.Id as Id, RowNumber() over (partition by r.Key
            order by r.Id) as Position, Sum(r.Amount) over (order by r.Id) as Total
            from bench.rows() r
            qualify Position = 1
            order by Id
            """, "core/11.13"),
        new("B18_Pivot", "Analytics", "Static category aggregation into four output columns.",
            "pivot bench.rows() r on r.Key in (0 as K0, 1 as K1, 2 as K2, 3 as K3) using Sum(r.Amount) as Total", "core/10.11"),
        new("B18_Unpivot", "Analytics", "Two output rows per input with keep fields.",
            "unpivot bench.rows() r on Label in (r.Id as Original, r.Key as Grouped) using Value keep r.Id as Id order by Id, Label", "core/10.12"),
        new("B19_KeyedUnion", "Relational", "Keyed UNION retains the left representative.",
            """
            select r.Key as Id, r.Name as Name
            from bench.rows() r
            union (Id) select d.Key as Id, 'dimension' as Name
            from bench.lookup() d
            order by Id
            """, "core/12"),
        new("B19_Except", "Relational", "Difference preserves left multiplicity.",
            """
            select r.Key as Id
            from bench.rows() r except select d.Key as Id
            from bench.lookup() d
            order by Id
            """, "core/12.4"),
        new("B19_Intersect", "Relational", "Intersection preserves left multiplicity.",
            """
            select r.Key as Id
            from bench.rows() r intersect select d.Key as Id
            from bench.lookup() d
            order by Id
            """, "core/12.5"),
        new("B20_TopOffset", "Analytics", "Computed ordering with a bounded output slice.",
            "select r.Id as Id, r.Name as Name from bench.rows() r order by r.Amount desc skip 1 take 2", "core/13"),
        new("B21_CteReuse", "Composition", "Repeated references to a completed grouped CTE.",
            """
            with totals as (select r.Key as Id, Sum(r.Amount) as Total
            from bench.rows() r
            group by r.Key) select a.Id as Id, a.Total + b.Total as Total
            from totals a
            inner join totals b on a.Id = b.Id
            order by a.Id
            """, "core/14"),
        new("B22_Recursive", "Composition", "Bounded linear frontier traversal over input edges.",
            """
            with recursive walk as (select r.Id as Id
            from bench.rows() r
            where r.Id = 0
            union all select e.Id
            from walk w
            inner join bench.rows() e on e.Id = w.Id + 1) select Id
            from walk
            order by Id
            """, "core/14.9", InputUnit: "depth", RecursiveTerminationSteps: 0)
    ];
}
