using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class JoinSemiAntiCrossJoinTests
{
    [TestMethod]
    public void SemiJoin_WhenRightSideHasDuplicateMatches_ShouldReturnEachLeftRowOnce()
    {
        var table = RunJoinQuery("select a.Name from #A.entities() a semi join #B.entities() b on a.Id = b.Id");
        TableMaterializationTestHelper.AssertColumns(table, ("a.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["A1"], ["A3"]);
    }

    [TestMethod]
    public void AntiJoin_WhenRightSideHasMatches_ShouldReturnOnlyUnmatchedLeftRows()
    {
        var table = RunJoinQuery("select a.Name from #A.entities() a anti join #B.entities() b on a.Id = b.Id");
        TableMaterializationTestHelper.AssertColumns(table, ("a.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["A2"]);
    }

    [TestMethod]
    public void SemiJoin_WhenEquiPredicateHasResidual_ShouldRespectResidualPredicate()
    {
        var query = "select a.Name from #A.entities() a semi join #B.entities() b on a.Id = b.Id and b.Population > 100";
        var table = RunJoinQuery(query);
        TableMaterializationTestHelper.AssertColumns(table, ("a.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["A3"]);
    }

    [TestMethod]
    public void AntiJoin_WhenEquiPredicateHasResidual_ShouldRespectResidualPredicate()
    {
        var query = "select a.Name from #A.entities() a anti join #B.entities() b on a.Id = b.Id and b.Population > 100";
        var table = RunJoinQuery(query);
        TableMaterializationTestHelper.AssertColumns(table, ("a.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["A1"], ["A2"]);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void SemiJoin_WhenPredicateIsNonEqui_ShouldUseNestedLoopSemantics(bool useSortMergeJoin)
    {
        var query = "select a.Name from #A.entities() a semi join #B.entities() b on a.Population > b.Population";
        var table = RunJoinQuery(query, new CompilationOptions(useSortMergeJoin: useSortMergeJoin));
        TableMaterializationTestHelper.AssertColumns(table, ("a.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["A1"], ["A3"]);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AntiJoin_WhenPredicateIsNonEqui_ShouldUseNestedLoopSemantics(bool useSortMergeJoin)
    {
        var query = "select a.Name from #A.entities() a anti join #B.entities() b on a.Population > b.Population";
        var table = RunJoinQuery(query, new CompilationOptions(useSortMergeJoin: useSortMergeJoin));
        TableMaterializationTestHelper.AssertColumns(table, ("a.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["A2"]);
    }
}
