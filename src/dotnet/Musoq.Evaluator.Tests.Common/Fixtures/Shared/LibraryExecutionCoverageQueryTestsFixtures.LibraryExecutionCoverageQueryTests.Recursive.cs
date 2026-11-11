using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionCoverageQueryTestsFixtures
{
    public const string RecursiveFrameQuery = """
        with recursive counter (Value) as (
            select 0 from values {(Seed: 1)} seed union all
            select c.Value + 1 from counter c where c.Value < 39)
        select Value, Sum(Value) over (order by Value rows between 2 preceding and current row) as Total,
            Avg(Value) over () as Average,
            Count(Value) over (partition by Value % 3) as Present
        from counter order by Value
        """;
}
