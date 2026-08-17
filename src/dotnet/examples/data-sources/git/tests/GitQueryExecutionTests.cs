namespace Musoq.Examples.DataSources.Git.Tests;

[TestClass]
public sealed class GitQueryExecutionTests : GitExampleTestBase
{
    [TestMethod]
    public void Query_WhenSelectingCommits_ShouldReturnDeterministicRows()
    {
        var table = Run("select ShortSha, AuthorName, Subject from #git.commits() order by AuthoredAt");

        Assert.AreEqual(5, table.Count);
        Assert.AreEqual("d4e5f60", table[0][0]);
        Assert.AreEqual("Cara Docs", table[0][1]);
        Assert.AreEqual("Document query samples", table[0][2]);
        Assert.AreEqual("e5f6012", table[4][0]);
        Assert.AreEqual("Bob Evaluator", table[4][1]);
    }

    [TestMethod]
    public void Query_WhenRepositoryParameterIsProvided_ShouldScopeRows()
    {
        var table = Run("select Repository, Count(1) from #git.commits('musoq') group by Repository");

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("musoq", table[0][0]);
        Assert.AreEqual(3, Convert.ToInt32(table[0][1]));
    }

    [TestMethod]
    public void Query_WhenFilteringAndGrouping_ShouldExecute()
    {
        var table = Run(
            "select AuthorName, Count(1) as Commits from #git.commits() where Branch = 'main' group by AuthorName order by AuthorName");

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("Alice Runtime", table[0][0]);
        Assert.AreEqual(1, Convert.ToInt32(table[0][1]));
        Assert.AreEqual("Bob Evaluator", table[1][0]);
        Assert.AreEqual(2, Convert.ToInt32(table[1][1]));
        Assert.AreEqual("Cara Docs", table[2][0]);
        Assert.AreEqual(1, Convert.ToInt32(table[2][1]));
    }

    [TestMethod]
    public void Query_WhenPredicateOrderSkipTakeArePushedDown_ShouldMatchExpectedRows()
    {
        var table = Run(
            "select ShortSha, Subject from #git.commits() where AuthorName = 'Bob Evaluator' order by AuthoredAt desc skip 0 take 1");

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("e5f6012", table[0][0]);
        Assert.AreEqual("Refresh runtime docs", table[0][1]);
    }

    [TestMethod]
    public void Query_WhenRuntimeRepositorySettingIsProvided_ShouldScopeRows()
    {
        var table = Run(
            "select Repository, Count(1) from #git.commits() group by Repository",
            options: CreateOptionsWithRepository("docs"));

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("docs", table[0][0]);
        Assert.AreEqual(2, Convert.ToInt32(table[0][1]));
    }

    [TestMethod]
    public void Query_WhenRepositoryArgumentAndRuntimeSettingAreProvided_ShouldPreferArgument()
    {
        var table = Run(
            "select Repository, Count(1) from #git.commits('musoq') group by Repository",
            options: CreateOptionsWithRepository("docs"));

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("musoq", table[0][0]);
        Assert.AreEqual(3, Convert.ToInt32(table[0][1]));
    }

    [TestMethod]
    public void Query_WhenStatsColumnsAreNotRequired_ShouldNotLoadStats()
    {
        var store = new TrackingGitHistoryStore();

        var table = Run(
            "select Subject from #git.commits('musoq') order by Subject",
            new GitSchemaProvider(store));

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual(0, store.StatsLoadCount);
    }

    [TestMethod]
    public void Query_WhenStatsColumnsAreRequired_ShouldLoadStatsLazily()
    {
        var store = new TrackingGitHistoryStore();

        var table = Run(
            "select Additions from #git.commits('musoq') order by AuthoredAt",
            new GitSchemaProvider(store));

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual(240, table[0][0]);
        Assert.AreEqual(3, store.StatsLoadCount);
    }

    [TestMethod]
    public void Query_WhenStatsPredicateIsResidual_ShouldLoadStatsDuringExecution()
    {
        var store = new TrackingGitHistoryStore();

        var table = Run(
            "select Subject from #git.commits() where Additions > 100 order by AuthoredAt",
            new GitSchemaProvider(store));

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("Add runtime planner", table[0][0]);
        Assert.AreEqual(5, store.StatsLoadCount);
    }

    [TestMethod]
    public void Query_WhenCheapAndStatsPredicatesAreMixed_ShouldApplyResidualTakeAfterStatsFilter()
    {
        var store = new TrackingGitHistoryStore();

        var table = Run(
            "select Subject from #git.commits() " +
            "where AuthorName = 'Bob Evaluator' and Additions > 100 " +
            "order by AuthoredAt asc take 1",
            new GitSchemaProvider(store));

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Refresh runtime docs", table[0][0]);
        Assert.AreEqual(2, store.StatsLoadCount);
    }

    [TestMethod]
    public void ReadmeSampleQueries_ShouldExecute()
    {
        string[] queries =
        [
            "select ShortSha, AuthoredAt, AuthorName, Subject from #git.commits() order by AuthoredAt desc take 5",
            "select ShortSha, Branch, Subject from #git.commits('musoq') where IsMerge = false order by AuthoredAt",
            "select AuthorName, Count(1) as Commits from #git.commits() group by AuthorName order by Commits desc",
            "select Repository, Sum(Additions) as Added, Sum(Deletions) as Deleted, Sum(Churn) as Churn from #git.commits() group by Repository order by Churn desc"
        ];

        foreach (var query in queries)
        {
            var table = Run(query);
            Assert.IsTrue(table.Count > 0, query);
        }
    }
}
