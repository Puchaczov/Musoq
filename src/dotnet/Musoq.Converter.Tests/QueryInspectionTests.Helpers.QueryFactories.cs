namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    private static string CreateWideValueTupleAggregateQuery()
    {
        return "select d.Dummy as Dummy, 1 as One, 2 as Two, 3 as Three, 4 as Four, 5 as Five, 6 as Six, 7 as Seven, 8 as Eight, Count(1) as Count from #system.dual() d group by d.Dummy, One, Two, Three, Four, Five, Six, Seven, Eight";
    }

    private static string CreateGroupedHavingQuery()
    {
        return "select d.Dummy as Dummy, Count(1) as Count from #system.dual() d group by d.Dummy having Count(1) = 1 and d.Dummy = 'single'";
    }

    private static string CreateGroupedAggregateOrderByQuery()
    {
        return "select d.Dummy as Dummy, Count(1) as Count from #system.dual() d group by d.Dummy order by Count(1) desc";
    }

    private static string CreateComputedGroupKeyQuery()
    {
        return "select d.Dummy + '!' as Dummy, Count(1) as Count from #system.dual() d group by d.Dummy + '!'";
    }

    private static string CreateCteBackedAsOfJoinQuery()
    {
        return "with rightCte as (select e.Dummy as Dummy from #system.dual() e) select d.Dummy, r.Dummy from #system.dual() d asof join rightCte r on d.Dummy >= r.Dummy";
    }

    private static string CreateDynamicAsOfJoinQuery()
    {
        return "select l.Name as LeftName, r.Name as RightName from #dynamic.all() l asof join #dynamic.all() r on l.Team = r.Team and l.Score >= r.Score";
    }

    private static string CreateDynamicCteBackedAsOfJoinQuery()
    {
        return "with rightCte as (select d.Team as Team, d.Name as Name, d.Score as Score from #dynamic.all() d) select l.Name as LeftName, r.Name as RightName from #dynamic.all() l asof join rightCte r on l.Team = r.Team and l.Score >= r.Score";
    }

    private static string CreateAggregateOverHashJoinQuery()
    {
        return "select d.Dummy as Dummy, Count(e.Dummy) as MatchCount from #system.dual() d inner join #system.dual() e on d.Dummy = e.Dummy group by d.Dummy";
    }

    private static string CreateCteBackedAggregateOverHashJoinQuery()
    {
        return "with leftCte as (select d.Dummy as Dummy from #system.dual() d), rightCte as (select e.Dummy as Dummy, 2 as Score from #system.dual() e) select l.Dummy as Dummy, Count(r.Score) as ScoreCount from leftCte l inner join rightCte r on l.Dummy = r.Dummy group by l.Dummy";
    }

    private static string CreateCteUnionAllQuery()
    {
        return "with l as (select d.Dummy as Dummy from #system.dual() d), r as (select e.Dummy as Dummy from #system.dual() e) select l.Dummy as Dummy from l union all (Dummy) select r.Dummy as Dummy from r";
    }

    private static string CreateIndependentCteJoinQuery()
    {
        return "with p as (select d.Dummy as Dummy from #system.dual() d), q as (select e.Dummy as Dummy from #system.dual() e) select p.Dummy, q.Dummy from p inner join q on p.Dummy = q.Dummy";
    }

    private static string CreateCteBackedInnerHashJoinQuery()
    {
        return "with leftCte as (select d.Dummy as Dummy from #system.dual() d), rightCte as (select e.Dummy as Dummy from #system.dual() e) select l.Dummy, r.Dummy from leftCte l inner join rightCte r on l.Dummy = r.Dummy";
    }

    private static string CreateCteBackedResidualOuterHashJoinQuery()
    {
        return "with leftCte as (select d.Dummy as Dummy from #system.dual() d), rightCte as (select e.Dummy as Dummy from #system.dual() e) select l.Dummy, r.Dummy from leftCte l left outer join rightCte r on l.Dummy = r.Dummy and r.Dummy = 'missing'";
    }

    private static string CreateResidualOuterHashJoinFeedingHashJoinQuery()
    {
        return "select d.Dummy, e.Dummy, f.Dummy from #system.dual() d left outer join #system.dual() e on d.Dummy = e.Dummy and e.Dummy = 'missing' inner join #system.dual() f on d.Dummy = f.Dummy";
    }

    private static string CreateCteBackedInnerNestedLoopJoinQuery()
    {
        return "with leftCte as (select d.Dummy + '!' as Dummy from #system.dual() d), rightCte as (select e.Dummy as Dummy from #system.dual() e) select l.Dummy, r.Dummy from leftCte l inner join rightCte r on l.Dummy != r.Dummy";
    }

    private static string CreateNestedUnionAllQuery()
    {
        return "select d.Dummy as Dummy from #system.dual() d union all (Dummy) select e.Dummy as Dummy from #system.dual() e union all (Dummy) select f.Dummy as Dummy from #system.dual() f";
    }

    private static string CreateFilteredUnionAllQuery()
    {
        return "select d.Dummy as Dummy from #system.dual() d where d.Dummy = 'single' union all (Dummy) select e.Dummy as Dummy from #system.dual() e where e.Dummy = 'missing'";
    }

    private static string CreateComputedUnionAllQuery()
    {
        return "select d.Dummy + '!' as Dummy from #system.dual() d union all (Dummy) select e.Dummy + '?' as Dummy from #system.dual() e";
    }

    private static string CreateAggregateUnionAllQuery()
    {
        return "select d.Dummy as Dummy, Count(1) as Count from #system.dual() d group by d.Dummy union all (Dummy) select e.Dummy as Dummy, Count(1) as Count from #system.dual() e group by e.Dummy";
    }

    private static string CreateRightSortedUnionAllQuery()
    {
        return "select 'b' as Dummy from #system.dual() d union all (Dummy) select 'a' as Dummy from #system.dual() e order by Dummy";
    }
}
