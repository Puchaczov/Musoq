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

        var buildItems = CreateBuildItems<BasicEntity>(query);
        
        Assert.AreEqual(1, buildItems.UsedColumns.Count);
        
        var columns = 
            buildItems.UsedColumns
                .Where(f => f.Key.Alias == "a")
                .Select(f => f.Value).First();
        
        Assert.AreEqual(1, columns.Length);
        Assert.IsTrue(columns.Select(f => f.ColumnName).Contains("City"));
    }
    
    [TestMethod]
    public void WhenColumnsUsedAsSourceOfMethodAndUsedInWhere_ShouldPass()
    {
        var query = "select a.City from #A.entities() a where DoNothing(a.Population) = 400d";

        var buildItems = CreateBuildItems<BasicEntity>(query);
        
        Assert.AreEqual(1, buildItems.UsedColumns.Count);
        
        var columns = 
            buildItems.UsedColumns
                .Where(f => f.Key.Alias == "a")
                .Select(f => f.Value).First();
        
        Assert.AreEqual(2, columns.Length);
        Assert.IsTrue(columns.Select(f => f.ColumnName).Contains("City"));
        Assert.IsTrue(columns.Select(f => f.ColumnName).Contains("Population"));
    }
    
    [TestMethod]
    public void WhenColumnsUsedAsSourceOfMethodAndUsedInWhereAndUsedInSelect_ShouldPass()
    {
        var query = "select a.City, DoNothing(a.Population) from #A.entities() a where DoNothing(a.Population) = 400d";

        var buildItems = CreateBuildItems<BasicEntity>(query);
        
        Assert.AreEqual(1, buildItems.UsedColumns.Count);
        
        var columns = 
            buildItems.UsedColumns
                .Where(f => f.Key.Alias == "a")
                .Select(f => f.Value).First();
        
        Assert.AreEqual(2, columns.Length);
        Assert.IsTrue(columns.Select(f => f.ColumnName).Contains("City"));
        Assert.IsTrue(columns.Select(f => f.ColumnName).Contains("Population"));
    }
    
    [TestMethod]
    public void WhenColumnsUsedAsSourceOfMethodAndUsedInWhereAndUsedInSelectAndUsedInGroupBy_ShouldPass()
    {
        var query = "select a.City, DoNothing(a.Population) from #A.entities() a where DoNothing(a.Population) = 400d group by a.Month";

        var buildItems = CreateBuildItems<BasicEntity>(query);
        
        Assert.AreEqual(1, buildItems.UsedColumns.Count);
        
        var columns = 
            buildItems.UsedColumns
                .Where(f => f.Key.Alias == "a")
                .Select(f => f.Value).First();
        
        Assert.AreEqual(3, columns.Length);
        Assert.IsTrue(columns.Select(f => f.ColumnName).Contains("City"));
        Assert.IsTrue(columns.Select(f => f.ColumnName).Contains("Population"));
        Assert.IsTrue(columns.Select(f => f.ColumnName).Contains("Month"));
    }
    
    [TestMethod]
    public void WhenColumnsUsedAsSourceOfMethodAndUsedInWhereAndUsedInSelectAndUsedInGroupByWithHaving_ShouldPass()
    {
        var query = "select a.City from #A.entities() a group by a.Month having a.Population > 100d";

        var buildItems = CreateBuildItems<BasicEntity>(query);
        
        Assert.AreEqual(1, buildItems.UsedColumns.Count);
        
        var columns = 
            buildItems.UsedColumns
                .Where(f => f.Key.Alias == "a")
                .Select(f => f.Value).First();
        
        Assert.AreEqual(3, columns.Length);
        Assert.IsTrue(columns.Select(f => f.ColumnName).Contains("City"));
        Assert.IsTrue(columns.Select(f => f.ColumnName).Contains("Population"));
        Assert.IsTrue(columns.Select(f => f.ColumnName).Contains("Month"));
    }
    
    [TestMethod]
    public void WhenColumnsUsedInJoinQuery_ShouldPass()
    {
        var query = "select a.City, b.City from #A.entities() a inner join #B.entities() b on a.City = b.City";

        var buildItems = CreateBuildItems<BasicEntity>(query);
        
        Assert.AreEqual(2, buildItems.UsedColumns.Count);
        
        var columnsA = 
            buildItems.UsedColumns
                .Where(f => f.Key.Alias == "a")
                .Select(f => f.Value).First();
        
        Assert.AreEqual(1, columnsA.Length);
        Assert.IsTrue(columnsA.Select(f => f.ColumnName).Contains("City"));
        
        var columnsB = 
            buildItems.UsedColumns
                .Where(f => f.Key.Alias == "b")
                .Select(f => f.Value).First();
        
        Assert.AreEqual(1, columnsB.Length);
        Assert.IsTrue(columnsB.Select(f => f.ColumnName).Contains("City"));
    }
}