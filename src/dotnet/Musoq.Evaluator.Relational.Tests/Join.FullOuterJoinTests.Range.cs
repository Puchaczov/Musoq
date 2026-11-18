using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class JoinFullOuterJoinTests
{
    [TestMethod]
    [DataRow("<")]
    [DataRow("<=")]
    [DataRow(">")]
    [DataRow(">=")]
    public void IndexedFullOuterRangePreservesOccurrenceOrderAndResiduals(string comparison)
    {
        var query =
            $"select a.Name, b.Name from #A.entities() a full outer join #B.entities() b on a.NullableValue {comparison} b.NullableValue and b.Id <> a.Id";
        BasicEntity[] left =
        [
            new("left") { Id = 1, NullableValue = 2 },
            new("same key different residual") { Id = 2, NullableValue = 2 },
            new("null") { NullableValue = null },
            new("another null") { NullableValue = null },
            new("revisited") { Id = 1, NullableValue = 2 },
        ];
        BasicEntity repeated = new("duplicate") { NullableValue = 3 };
        BasicEntity[] right =
        [
            new("unknown") { NullableValue = null },
            repeated,
            new("one") { NullableValue = 1 },
            new("rejected") { Id = 1, NullableValue = 4 },
            new("equal") { NullableValue = 2 },
            repeated,
        ];
        var indexed = new CompilationOptions(useHashJoin: false, useSortMergeJoin: true);
        Assert.Contains("PhysicalSortMergeJoin [FullOuter]", Inspect(query, indexed).PhysicalPlanText);
        foreach (
            var sources in new[]
            {
                CreateSources(left, right),
                CreateSources([], right),
                CreateSources(left, []),
                CreateSources([], []),
            }
        )
        {
            var expected = Run(query, sources, new CompilationOptions(useHashJoin: false, useSortMergeJoin: false));
            var actual = Run(query, sources, indexed);
            CollectionAssert.AreEqual(expected.ToArray(), actual.ToArray());
        }
    }
}
