using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class JoinSemiAntiCrossJoinTests
{
    [TestMethod]
    public void SemiJoin_WhenRightSideHasDuplicateMatches_ShouldReturnEachLeftRowOnce()
    {
        var table = RunJoinQuery("select a.Name from #A.entities() a semi join #B.entities() b on a.Id = b.Id");
        var rows = table.Select(row => (string)row[0]).OrderBy(name => name).ToArray();

        CollectionAssert.AreEqual(new[] { "A1", "A3" }, rows);
    }

    [TestMethod]
    public void AntiJoin_WhenRightSideHasMatches_ShouldReturnOnlyUnmatchedLeftRows()
    {
        var table = RunJoinQuery("select a.Name from #A.entities() a anti join #B.entities() b on a.Id = b.Id");
        var rows = table.Select(row => (string)row[0]).ToArray();

        CollectionAssert.AreEqual(new[] { "A2" }, rows);
    }

    [TestMethod]
    public void SemiJoin_WhenEquiPredicateHasResidual_ShouldRespectResidualPredicate()
    {
        var query = "select a.Name from #A.entities() a semi join #B.entities() b on a.Id = b.Id and b.Population > 100";
        var table = RunJoinQuery(query);
        var rows = table.Select(row => (string)row[0]).ToArray();

        CollectionAssert.AreEqual(new[] { "A3" }, rows);
    }

    [TestMethod]
    public void AntiJoin_WhenEquiPredicateHasResidual_ShouldRespectResidualPredicate()
    {
        var query = "select a.Name from #A.entities() a anti join #B.entities() b on a.Id = b.Id and b.Population > 100";
        var table = RunJoinQuery(query);
        var rows = table.Select(row => (string)row[0]).OrderBy(name => name).ToArray();

        CollectionAssert.AreEqual(new[] { "A1", "A2" }, rows);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void SemiJoin_WhenPredicateIsNonEqui_ShouldUseNestedLoopSemantics(bool useSortMergeJoin)
    {
        var query = "select a.Name from #A.entities() a semi join #B.entities() b on a.Population > b.Population";
        var table = RunJoinQuery(query, new CompilationOptions(useSortMergeJoin: useSortMergeJoin));
        var rows = table.Select(row => (string)row[0]).OrderBy(name => name).ToArray();

        CollectionAssert.AreEqual(new[] { "A1", "A3" }, rows);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AntiJoin_WhenPredicateIsNonEqui_ShouldUseNestedLoopSemantics(bool useSortMergeJoin)
    {
        var query = "select a.Name from #A.entities() a anti join #B.entities() b on a.Population > b.Population";
        var table = RunJoinQuery(query, new CompilationOptions(useSortMergeJoin: useSortMergeJoin));
        var rows = table.Select(row => (string)row[0]).ToArray();

        CollectionAssert.AreEqual(new[] { "A2" }, rows);
    }
}