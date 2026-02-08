using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class UsedColumnsTests : BasicEntityTestBase
{
    [TestMethod]
    public void WhenColumnsUsedAsSourceOfMethod_ShouldPass()
    {
        var query = "select DoNothing(a.City) from #A.entities() a";

        var buildItems = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(query);

        Assert.HasCount(1, buildItems.UsedColumns);

        var columns =
            buildItems.UsedColumns
                .Where(f => f.Key.Alias == "a")
                .Select(f => f.Value).First();

        Assert.HasCount(1, columns);
        Assert.IsTrue(columns.Select(f => f.ColumnName).Contains("City"));
    }

    [TestMethod]
    public void WhenCteWithSameAliasExist2_ShouldPass()
    {
        const string query = @"
with q1 as (
    select a.City as City from #A.entities() a where a.City = 'Warsaw'
), q2 as (
    select a.Population as Population from #A.entities() a where a.Population = 200d
) select City from q1";

        var buildItems = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(query);

        Assert.HasCount(2, buildItems.UsedColumns);

        var columns =
            buildItems.UsedColumns
                .Where(f => f.Key.Alias == "a")
                .Select(f => f.Value).ElementAt(0);

        Assert.HasCount(1, columns);
        Assert.IsTrue(columns.Select(f => f.ColumnName).Contains("City"));

        columns =
            buildItems.UsedColumns
                .Where(f => f.Key.Alias == "a")
                .Select(f => f.Value).ElementAt(1);

        Assert.HasCount(1, columns);
    }

    [TestMethod]
    public void WhenCteWithSameAliasExist_ShouldPass()
    {
        const string query = @"
with q1 as (
    select a.City as City from #A.entities('1') a
), q2 as (
    select a.Population as Population from #A.entities('2') a
) select City from q1";

        var buildItems = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(query);

        Assert.HasCount(2, buildItems.UsedColumns);

        var columns =
            buildItems.UsedColumns
                .Where(f => f.Key.Alias == "a")
                .Select(f => f.Value).ElementAt(0);

        Assert.HasCount(1, columns);

        columns =
            buildItems.UsedColumns
                .Where(f => f.Key.Alias == "a")
                .Select(f => f.Value).ElementAt(1);

        Assert.HasCount(1, columns);

        Assert.IsTrue(columns.Select(f => f.ColumnName).Contains("Population"));
    }

    [TestMethod]
    public void WhenColumnsUsedAsSourceOfMethodAndUsedInWhere_ShouldPass()
    {
        var query = "select a.City from #A.entities() a where DoNothing(a.Population) = 400d";

        var buildItems = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(query);

        Assert.HasCount(1, buildItems.UsedColumns);

        var columns =
            buildItems.UsedColumns
                .Where(f => f.Key.Alias == "a")
                .Select(f => f.Value).First();

        Assert.HasCount(2, columns);
        Assert.IsTrue(columns.Select(f => f.ColumnName).Contains("City"));
        Assert.IsTrue(columns.Select(f => f.ColumnName).Contains("Population"));
    }

    [TestMethod]
    public void WhenColumnsUsedAsSourceOfMethodAndUsedInWhereAndUsedInSelect_ShouldPass()
    {
        var query = "select a.City, DoNothing(a.Population) from #A.entities() a where DoNothing(a.Population) = 400d";

        var buildItems = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(query);

        Assert.HasCount(1, buildItems.UsedColumns);

        var columns =
            buildItems.UsedColumns
                .Where(f => f.Key.Alias == "a")
                .Select(f => f.Value).First();

        Assert.HasCount(2, columns);
        Assert.IsTrue(columns.Select(f => f.ColumnName).Contains("City"));
        Assert.IsTrue(columns.Select(f => f.ColumnName).Contains("Population"));
    }

    [TestMethod]
    public void WhenColumnsUsedAsSourceOfMethodAndUsedInWhereAndUsedInSelectAndUsedInGroupBy_ShouldPass()
    {
        var query =
            "select a.City, DoNothing(a.Population) from #A.entities() a where DoNothing(a.Population) = 400d group by a.Month, a.City, a.Population";

        var buildItems = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(query);

        Assert.HasCount(1, buildItems.UsedColumns);

        var columns =
            buildItems.UsedColumns
                .Where(f => f.Key.Alias == "a")
                .Select(f => f.Value).First();

        Assert.HasCount(3, columns);
        Assert.IsTrue(columns.Select(f => f.ColumnName).Contains("City"));
        Assert.IsTrue(columns.Select(f => f.ColumnName).Contains("Population"));
        Assert.IsTrue(columns.Select(f => f.ColumnName).Contains("Month"));
    }

    [TestMethod]
    public void WhenColumnsUsedAsSourceOfMethodAndUsedInWhereAndUsedInSelectAndUsedInGroupByWithHaving_ShouldPass()
    {
        var query = "select a.City from #A.entities() a group by a.Month, a.City having a.Population > 100d";

        var buildItems = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(query);

        Assert.HasCount(1, buildItems.UsedColumns);

        var columns =
            buildItems.UsedColumns
                .Where(f => f.Key.Alias == "a")
                .Select(f => f.Value).First();

        Assert.HasCount(3, columns);
        Assert.IsTrue(columns.Select(f => f.ColumnName).Contains("City"));
        Assert.IsTrue(columns.Select(f => f.ColumnName).Contains("Population"));
        Assert.IsTrue(columns.Select(f => f.ColumnName).Contains("Month"));
    }

    [TestMethod]
    public void WhenColumnsUsedInJoinQuery_ShouldPass()
    {
        var query = "select a.City, b.City from #A.entities() a inner join #B.entities() b on a.City = b.City";

        var buildItems = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(query);

        Assert.HasCount(2, buildItems.UsedColumns);

        var columnsA =
            buildItems.UsedColumns
                .Where(f => f.Key.Alias == "a")
                .Select(f => f.Value).First();

        Assert.HasCount(1, columnsA);
        Assert.IsTrue(columnsA.Select(f => f.ColumnName).Contains("City"));

        var columnsB =
            buildItems.UsedColumns
                .Where(f => f.Key.Alias == "b")
                .Select(f => f.Value).First();

        Assert.HasCount(1, columnsB);
        Assert.IsTrue(columnsB.Select(f => f.ColumnName).Contains("City"));
    }

    [TestMethod]
    public void WhenGroupByAndOrderByUsed_ShouldPass()
    {
        var query =
            "select 1 from #A.entities() a inner join #B.entities() b on a.Population = b.Population group by a.City";

        var buildItems = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(query);

        Assert.HasCount(2, buildItems.UsedColumns);

        var columnsA =
            buildItems.UsedColumns
                .Where(f => f.Key.Alias == "a")
                .Select(f => f.Value).First();

        Assert.HasCount(2, columnsA);

        var columnsB =
            buildItems.UsedColumns
                .Where(f => f.Key.Alias == "b")
                .Select(f => f.Value).First();

        Assert.HasCount(1, columnsB);
    }
}
