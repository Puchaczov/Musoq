using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class CrossApplyUnusedAliasTests
{
    /// <summary>
    ///     Test CTE with cross apply followed by join.
    /// </summary>
    [TestMethod]
    public void Cte_CrossApply_FollowedByJoin_ShouldWork()
    {
        const string query = @"
            with cte as (
                select a.City as City, a.Country as Country
                from #schema.first() a
            )
            select c.City, t.Money, j.Month
            from cte c
            cross apply #schema.second(c.Country) t
            inner join #schema.third() j on c.Country = j.Country";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 }
        }.ToArray();

        var secondSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 1000, Month = "January" }
        }.ToArray();

        var thirdSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 5000, Month = "March" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource,
            thirdSource);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("c.City", typeof(string)),
            ("t.Money", typeof(decimal)),
            ("j.Month", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["City1", 1000m, "March"]);
    }

    /// <summary>
    ///     Test CTE with cross apply followed by left outer join.
    /// </summary>
    [TestMethod]
    public void Cte_CrossApply_FollowedByLeftOuterJoin_ShouldWork()
    {
        const string query = @"
            with cte as (
                select a.City as City, a.Country as Country
                from #schema.first() a
            )
            select c.City, t.Money, j.Month
            from cte c
            cross apply #schema.second(c.Country) t
            left outer join #schema.third() j on c.Country = j.Country";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 },
            new() { City = "City2", Country = "NoMatch", Population = 200 }
        }.ToArray();

        var secondSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 1000, Month = "January" },
            new() { Country = "NoMatch", Money = 2000, Month = "February" }
        }.ToArray();

        var thirdSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 5000, Month = "March" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource,
            thirdSource);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("c.City", typeof(string)),
            ("t.Money", typeof(decimal)),
            ("j.Month", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["City1", 1000m, "March"],
            ["City1", 2000m, "March"],
            ["City2", 1000m, null],
            ["City2", 2000m, null]);
    }

}
