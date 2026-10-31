namespace Musoq.Benchmarks.Scenarios;

public static partial class ScenarioCatalog
{
    private static IEnumerable<Scenario> Composed() =>
    [
        new("X01_SalesReporting", "Composed", "Join, filter, aggregate, HAVING, rank, QUALIFY and page.",
            """
            with totals as (select r.Key as Id, Sum(r.Amount) as Total
            from bench.rows() r
            inner join bench.lookup() d on r.Key = d.Key
            where r.Amount >= 2
            group by r.Key
            having Sum(r.Amount) > 0) select Id, Total, RowNumber() over (order by Total desc) as Position
            from totals
            qualify Position <= 2
            order by Id
            take 2
            """, "core/8,10,11,13"),
        new("X02_JoinWindow", "Composed", "Duplicate-producing join into a peer-aware running window.",
            """
            select r.Id as Id, Sum(r.Amount) over (order by r.Id) as Total, Sum(r.Amount) over (order by r.Id range between 1 preceding and current row) as Sliding
            from bench.rows() r
            inner join bench.twice() d on r.Key = d.Key
            order by r.Id
            """, "core/8,11"),
        new("X03_Reconciliation", "Composed", "Full outer join, presence classification and discrepancy grouping.",
            """
            with classified as (select case when r is missing then 'right-only' when d is missing then 'missing' else 'matched' end as State, r.Optional ?? -1 as Fallback
            from bench.rows() r
            full outer join bench.reconcile() d on r.Key = d.Key) select State, Fallback, Count(*) as Count
            from classified
            group by State, Fallback
            order by State, Fallback
            """, "core/8.4,10,19"),
        new("X04_TemporalAnalytics", "Composed", "ASOF enrichment, tie breaking and multiple analytic windows.",
            """
            select r.Id as Id, Lag(d.Amount) over (order by r.Id) as Previous, Sum(d.Amount) over (order by r.Id) as Total
            from bench.rows() r asof
            left join bench.lookup() d on r.Time >= d.Time tie break by d.Id desc
            order by r.Id
            """, "core/8.11,11"),
        new("X05_CollectionPivot", "Composed", "Collection expansion with ordinality into static pivot aggregation.",
            """
            with expanded as (select r.Key as Id, i.Ordinal as Bucket, r.Amount as Amount
            from bench.rows() r
            outer apply r.Items i with ordinality
            left join bench.lookup() d on i.Value = d.Id) pivot expanded e on e.Bucket in (0 as Even, 1 as Odd) using Sum(e.Amount) as Total
            group by e.Id
            order by Id
            """, "core/8,9,10.11"),
        new("X06_Consolidation", "Composed", "Keyed CTE UNION followed by distinct and global pagination.",
            """
            with combined as (select r.Key as Id
            from bench.rows() r
            union (Id) select d.Key as Id
            from bench.lookup() d) select distinct Id
            from combined
            order by Id
            skip 1
            take 2
            """, "core/5.5,12,13,14"),
        new("X07_LogAnalytics", "Composed", "Tolerant text parsing, strict conversion, predicates, grouping and ranking.",
            Entry + """
            with logs as (select p.Level as Level, r.Amount as Amount
            from bench.rows() r
            outer apply TryParse<Entry>(r.Text) p
            where p.Number::int % 2 = 0 and p.Label rlike 'item') select Level, Sum(Amount) as Total, RowNumber() over (order by Level) as Position
            from logs
            group by Level
            qualify Position = 1
            order by Level
            """, "binary-text/5,7;core/7,10,11"),
        new("X08_PacketAnalytics", "Composed", "Exact nested binary, array expansion, text decoding and typed 64-bit lookup aggregation.",
            NestedPacket + Entry + """
            select t.Level as Level, Sum(p.Body.Value::long) as Total, Count(*) as Count
            from bench.rows() r
            cross apply Interpret<Packet>(r.Payload) p
            cross apply p.Tail b
            cross apply Parse<Entry>(r.Text) t
            inner join bench.lookup() d on r.Key = d.Key
            group by t.Level
            order by t.Level
            """, "binary-text/4,5,8;core/8,10"),
        new("X09_RecursiveChunks", "Composed", "Recursive offset/partial decoding stops after a malformed terminal chunk.",
            Header + """
            with recursive walk (Bytes, Off, Used, Error) as (select r.Payload, 0, 0, null
            from bench.stream() r
            union all select w.Bytes, w.Off + w.Used, p.BytesConsumed, p.ErrorField
            from walk w
            cross apply PartialInterpret<Header>(FromBase64(ToBase64(w.Bytes, w.Off + w.Used, Length(w.Bytes) - w.Off - w.Used))) p
            where w.Off + w.Used < Length(w.Bytes) and w.Error is null) select Off as Id, Used as Consumed, Error
            from walk
            order by Id, Consumed
            """, "core/14.9;binary-text/7", ValidatePrimitiveTypes: false, InputUnit: "chunks", RecursiveTerminationSteps: 2),
        new("X10_StructuredPipeline", "Composed", "Grouped CTE to closed record input, coupling, lookup join and running totals.",
            """
            table MetricsRow { Id: int, Amount: decimal }; couple bench.structured with table MetricsRow as Metrics; with keys as (select r.Key as Id, r.Key + 1d as Amount
            from bench.rows() r
            group by r.Key) select m.Id as Id, Sum(m.Amount) over (order by m.Id) as Total
            from Metrics(items: keys) m
            inner join bench.lookup() d on m.Id = d.Id
            order by m.Id
            """, "core/4.5,11,14;table-couple/10")
    ];
}
