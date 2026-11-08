using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionCoverageQueryTestsFixtures
{
    public const string SetAggregateWindowQuery = """
        with doubled as (
            select Partition, Value from #schema.first()
            union all select Partition, Value from #schema.first() where Position % 2 = 0),
        totals as (
            select Partition, Sum(Value) as Total, Count(Value) as Present,
                Sum(distinct Value) as UniqueTotal from doubled group by Partition)
        select Partition, Total, Present, UniqueTotal, Avg(Total) over () as AverageTotal
        from totals
        """;

}
