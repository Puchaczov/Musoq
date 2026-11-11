using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionAsOfQueryTestsFixtures
{
    public const string CompositeQuery =
        "select a.Position as LeftPosition, b.Position as RightPosition "
        + "from #schema.first() a asof left join #schema.first() b "
        + "on a.Partition = b.Partition and a.Label = b.Label and a.Key >= b.Key "
        + "tie break by b.Position desc order by LeftPosition";

    public static AsOfInput[] CreateRows() =>
        [
            new(1, 10, "A", 10m),
            new(2, 10, "A", 5m),
            new(3, null, "A", 7m),
            new(4, 10, "B", 8m),
            new(5, 10, "a", 10m),
            new(6, 10, "A", 4m),
            new(7, 10, "A", null),
        ];

    public sealed record AsOfInput(int Position, int? Partition, string Label, decimal? Key);
}
