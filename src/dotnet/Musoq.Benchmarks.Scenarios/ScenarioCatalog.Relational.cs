namespace Musoq.Benchmarks.Scenarios;

public static partial class ScenarioCatalog
{
    private static IEnumerable<Scenario> Relational() =>
    [
        new("B02_Scalar", "Expressions", "Arithmetic, widening and temporal/property reads.",
            "select r.Id as Id, (r.Amount * 2 + r.Id)::long as Value from bench.rows() r order by r.Id", "core/3,22"),
        new("B03_Guards", "Expressions", "Lazy CASE, coalesce and stable repeated scalars.",
            """
            select r.Id as Id, case when r.Id % 2 = 0 then r.Id * 2 else -1 end as Value, r.Optional ?? -2 as Fallback
            from bench.rows() r
            order by r.Id
            """, "core/7,19"),
        new("B04_Patterns", "Expressions", "LIKE and a constant Unicode-capable regular expression.",
            """
            select r.Id as Id
            from bench.rows() r
            where r.Name like '%em-_' and r.Name rlike r'item-[02]$'
            order by r.Id
            """, "core/7.6,7.7"),
        new("B05_Membership", "Expressions", "Membership with repeated and NULL literal keys.",
            "select r.Id as Id from bench.rows() r where r.Key in (0, 2, 2, null) order by r.Id", "core/7.9"),
        new("B06_Distinct", "Expressions", "Deduplicate nullable scalar values.",
            "select distinct r.Optional as Value from bench.rows() r order by Value", "core/5.5"),
        new("B07_EqualityJoin", "Relational", "Equal-sized unique-key join builds and probes N keys with bounded output.",
            """
            select r.Id as Id, d.Id as Match
            from bench.rows() r
            inner join bench.rows() d on r.Id = d.Id
            order by r.Id
            """, "core/8"),
        new("B07_DuplicateJoin", "Relational", "Bounded two-row dimension fan-out.",
            """
            select r.Id as Id, d.Id as Match
            from bench.rows() r
            inner join bench.twice() d on r.Key = d.Key
            order by r.Id
            """, "core/8"),
        new("B08_LeftJoin", "Relational", "Preserve unmatched left rows.",
            """
            select r.Id as Id, d.Id as Match
            from bench.rows() r
            left join bench.lookup() d on r.Key = d.Key
            order by r.Id
            """, "core/8.2"),
        new("B08_FullJoin", "Relational", "Full join with nullable output and presence classification.",
            """
            select r.Id as Id, d.Id as Match
            from bench.rows() r
            full outer join bench.reconcile() d on r.Key = d.Key
            order by r.Id
            """, "core/8.4"),
        new("B08_RightJoin", "Relational", "Preserve an unmatched right dimension row.",
            """
            select r.Id as Id, d.Id as Match
            from bench.rows() r right join bench.reconcile() d on r.Key = d.Key
            order by r.Id
            """, "core/8.3"),
        new("B08_SemiJoin", "Relational", "Existence without duplicate multiplication.",
            "select r.Id as Id from bench.rows() r semi join bench.twice() d on r.Key = d.Key order by r.Id", "core/8.5"),
        new("B08_AntiJoin", "Relational", "Nonmatching left keys.",
            "select r.Id as Id from bench.rows() r anti join bench.lookup() d on r.Key = d.Key order by r.Id", "core/8.6"),
        new("B08_ThetaJoin", "Relational", "Non-equality join against a fixed three-row lookup; at most two matches per input row.",
            """
            select r.Id as Id, d.Id as Match
            from bench.rows() r
            inner join bench.lookup() d on r.Key < d.Key
            order by r.Id, d.Id
            """, "core/8.9"),
        new("B08_CrossJoin", "Relational", "Cartesian expansion against a fixed three-row dimension.",
            """
            select r.Id as Id, d.Id as Match
            from bench.rows() r cross join bench.lookup() d
            order by r.Id, d.Id
            """, "core/8.7"),
        new("B09_AsOf", "Relational", "Nearest preceding observation with deterministic duplicate handling.",
            """
            select r.Id as Id, d.Id as Match
            from bench.rows() r asof
            left join bench.rows() d on r.Time >= d.Time tie break by d.Id desc
            order by r.Id
            """, "core/8.11"),
        new("B10_Exists", "Relational", "Equality-correlated existence lowered to a specialized strategy.",
            """
            select r.Id as Id
            from bench.rows() r
            where exists (select d.Id
            from bench.lookup() d
            where d.Key = r.Key)
            order by r.Id
            """, "core/7.10"),
        new("B10_CorrelatedFallback", "Relational", "Demanded shaped scalar correlation using per-binding execution.",
            """
            select r.Id as Id, (select d.Id
            from bench.lookup() d
            where d.Id <= r.Id
            order by d.Id desc
            take 1) as Match
            from bench.rows() r
            order by r.Id
            """, "core/7.11"),
        new("B11_ScalarAggregate", "Relational", "Correlated aggregate with NULL for absent groups.",
            """
            select r.Id as Id, (select Sum(d.Amount)
            from bench.lookup() d
            where d.Key = r.Key) as Total
            from bench.rows() r
            order by r.Id
            """, "core/7.11"),
        new("B12_Quantified", "Relational", "Quantified comparison in a projected Boolean context.",
            """
            select r.Id as Id, r.Id > all (select d.Id
            from bench.lookup() d) as Larger
            from bench.rows() r
            order by r.Id
            """, "core/7.12"),
        new("B24_Enums", "Contracts", "Enum flag matching and integral carrier projection.",
            """
            select r.Id as Id, EnumValue(r.State) as State
            from bench.rows() r
            where HasAnyFlags(r.State, 'Queued')
            order by r.Id
            """, "core/3.8,18.4")
    ];
}
