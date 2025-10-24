using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class RewriteWhereExpressionToPassItToDataSourceTests : BasicEntityTestBase
{
    [TestMethod]
    public void WhenWhereExpressionIsNotRewritten_ShouldPass()
    {
        var query = "select 1 from #A.entities() a where a.Population > 0";

        var buildItems = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(query);
        
        Assert.AreEqual(1, buildItems.UsedWhereNodes.Count);

        var firstWhereNodePair = buildItems.UsedWhereNodes.Where(f => f.Key.Alias == "a").ToArray();

        Assert.IsNotNull(firstWhereNodePair);
        Assert.AreEqual(1, firstWhereNodePair.Length);
        
        var firstWhereNode = firstWhereNodePair.First().Value;
        
        Assert.AreEqual("a.Population > 0", firstWhereNode.Expression.ToString());
    }
    
    [TestMethod]
    public void WhenLikeExpressionIsRewritten_ShouldPass()
    {
        var query = "select 1 from #A.entities() a where a.City like '%abc%'";

        var buildItems = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(query);
        
        Assert.AreEqual(1, buildItems.UsedWhereNodes.Count);

        var firstWhereNodePair = buildItems.UsedWhereNodes.Where(f => f.Key.Alias == "a").ToArray();

        Assert.IsNotNull(firstWhereNodePair);
        Assert.AreEqual(1, firstWhereNodePair.Length);
        
        var firstWhereNode = firstWhereNodePair.First().Value;
        
        Assert.AreEqual("1 = 1", firstWhereNode.Expression.ToString());
    }
    
    [TestMethod]
    public void WhenRLikeExpressionIsRewritten_ShouldPass()
    {
        var query = "select 1 from #A.entities() a where a.City rlike '%abc%'";

        var buildItems = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(query);
        
        Assert.AreEqual(1, buildItems.UsedWhereNodes.Count);

        var firstWhereNodePair = buildItems.UsedWhereNodes.Where(f => f.Key.Alias == "a").ToArray();

        Assert.IsNotNull(firstWhereNodePair);
        Assert.AreEqual(1, firstWhereNodePair.Length);
        
        var firstWhereNode = firstWhereNodePair.First().Value;
        
        Assert.AreEqual("1 = 1", firstWhereNode.Expression.ToString());
    }
    
    [TestMethod]
    public void WhenContainsExpressionIsNotRewritten_ShouldPass()
    {
        var query = "select 1 from #A.entities() a where a.City contains ('abc', 'def')";

        var buildItems = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(query);
        
        Assert.AreEqual(1, buildItems.UsedWhereNodes.Count);

        var firstWhereNodePair = buildItems.UsedWhereNodes.Where(f => f.Key.Alias == "a").ToArray();

        Assert.IsNotNull(firstWhereNodePair);
        Assert.AreEqual(1, firstWhereNodePair.Length);
        
        var firstWhereNode = firstWhereNodePair.First().Value;
        
        Assert.AreEqual("a.City contains ('abc', 'def')", firstWhereNode.Expression.ToString());
    }
    
    [TestMethod]
    public void WhenContainsExpressionIsRewritten_ShouldPass()
    {
        var query = "select 1 from #A.entities() a where a.City contains (DoNothing('abc'), 'def')";

        var buildItems = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(query);
        
        Assert.AreEqual(1, buildItems.UsedWhereNodes.Count);

        var firstWhereNodePair = buildItems.UsedWhereNodes.Where(f => f.Key.Alias == "a").ToArray();

        Assert.IsNotNull(firstWhereNodePair);
        Assert.AreEqual(1, firstWhereNodePair.Length);
        
        var firstWhereNode = firstWhereNodePair.First().Value;
        
        Assert.AreEqual("1 = 1", firstWhereNode.Expression.ToString());
    }
    
    [TestMethod]
    public void WhenWhereExpressionIsNotRewrittenAndUsesConditionOnTwoColumns_ShouldPass()
    {
        var query = "select 1 from #A.entities() a where a.Population > a.Population";

        var buildItems = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(query);
        
        Assert.AreEqual(1, buildItems.UsedWhereNodes.Count);

        var firstWhereNodePair = buildItems.UsedWhereNodes.Where(f => f.Key.Alias == "a").ToArray();

        Assert.IsNotNull(firstWhereNodePair);
        Assert.AreEqual(1, firstWhereNodePair.Length);
        
        var firstWhereNode = firstWhereNodePair.First().Value;
        
        Assert.AreEqual("a.Population > a.Population", firstWhereNode.Expression.ToString());
    }
    
    [TestMethod]
    public void WhenWhereExpressionUsesMustBeRewrittenForBothDataSources_ShouldPass()
    {
        var query = "select 1 from #A.entities() a inner join #B.entities() b on a.City = b.City where a.Population > 0";

        var buildItems = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(query);
        
        Assert.AreEqual(2, buildItems.UsedWhereNodes.Count);

        var firstWhereNodePair = buildItems.UsedWhereNodes.Where(f => f.Key.Alias == "a").ToArray();
        var secondWhereNodePair = buildItems.UsedWhereNodes.Where(f => f.Key.Alias == "b").ToArray();

        Assert.IsNotNull(firstWhereNodePair);
        Assert.IsNotNull(secondWhereNodePair);
        Assert.AreEqual(1, firstWhereNodePair.Length);
        Assert.AreEqual(1, secondWhereNodePair.Length);
        
        var firstWhereNode = firstWhereNodePair.First().Value;
        var secondWhereNode = secondWhereNodePair.First().Value;
        
        Assert.AreEqual("a.Population > 0", firstWhereNode.Expression.ToString());
        Assert.AreEqual("1 = 1", secondWhereNode.Expression.ToString());
    }
    
    [TestMethod]
    public void WhenWhereExpressionMustBeRewrittenDueToExchangingColumnsBetweenTwoSources_ShouldPass()
    {
        var query = "select 1 from #A.entities() a inner join #B.entities() b on a.City = b.City where a.Population > b.Population";

        var buildItems = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(query);
        
        Assert.AreEqual(2, buildItems.UsedWhereNodes.Count);

        var firstWhereNodePair = buildItems.UsedWhereNodes.Where(f => f.Key.Alias == "a").ToArray();
        var secondWhereNodePair = buildItems.UsedWhereNodes.Where(f => f.Key.Alias == "b").ToArray();

        Assert.IsNotNull(firstWhereNodePair);
        Assert.IsNotNull(secondWhereNodePair);
        Assert.AreEqual(1, firstWhereNodePair.Length);
        Assert.AreEqual(1, secondWhereNodePair.Length);
        
        var firstWhereNode = firstWhereNodePair.First().Value;
        var secondWhereNode = secondWhereNodePair.First().Value;
        
        Assert.AreEqual("1 = 1", firstWhereNode.Expression.ToString());
        Assert.AreEqual("1 = 1", secondWhereNode.Expression.ToString());
    }
    
    [TestMethod]
    public void WhenWhereExpressionMustBeRewrittenDueToExchangingColumnsBetweenThreeSources_ShouldPass()
    {
        var query = "select 1 from #A.entities() a inner join #B.entities() b on a.City = b.City inner join #C.entities() c on b.City = c.City where a.Population > b.Population and c.Population = 200d";

        var buildItems = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(query);
        
        Assert.AreEqual(3, buildItems.UsedWhereNodes.Count);
        
        var firstWhereNodePair = buildItems.UsedWhereNodes.Where(f => f.Key.Alias == "a").ToArray();
        var secondWhereNodePair = buildItems.UsedWhereNodes.Where(f => f.Key.Alias == "b").ToArray();
        var thirdWhereNodePair = buildItems.UsedWhereNodes.Where(f => f.Key.Alias == "c").ToArray();
        
        Assert.IsNotNull(firstWhereNodePair);
        Assert.IsNotNull(secondWhereNodePair);
        Assert.IsNotNull(thirdWhereNodePair);
        Assert.AreEqual(1, firstWhereNodePair.Length);
        Assert.AreEqual(1, secondWhereNodePair.Length);
        Assert.AreEqual(1, thirdWhereNodePair.Length);
        
        var firstWhereNode = firstWhereNodePair.First().Value;
        var secondWhereNode = secondWhereNodePair.First().Value;
        var thirdWhereNode = thirdWhereNodePair.First().Value;
        
        Assert.AreEqual("1 = 1 and 1 = 1", firstWhereNode.Expression.ToString());
        Assert.AreEqual("1 = 1 and 1 = 1", secondWhereNode.Expression.ToString());
        Assert.AreEqual("1 = 1 and c.Population = 200", thirdWhereNode.Expression.ToString());
    }
    
    [TestMethod]
    public void WhenCteUsedWithExchangeParametersBetweenSources_ShouldPass()
    {
        var query = @"
with a as (
    select City, Population from #A.entities() x where x.Population > 100d 
)
select 1 from a firstTable inner join #B.entities() b on firstTable.City = b.City inner join #C.entities() c on b.City = c.City where firstTable.Population > b.Population and c.Population = 200d";

        var buildItems = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(query);
        
        Assert.AreEqual(3, buildItems.UsedWhereNodes.Count);
        
        var firstWhereNodePair = buildItems.UsedWhereNodes.Where(f => f.Key.Alias == "x").ToArray();
        var secondWhereNodePair = buildItems.UsedWhereNodes.Where(f => f.Key.Alias == "b").ToArray();
        var thirdWhereNodePair = buildItems.UsedWhereNodes.Where(f => f.Key.Alias == "c").ToArray();
        
        Assert.IsNotNull(firstWhereNodePair);
        Assert.IsNotNull(secondWhereNodePair);
        Assert.IsNotNull(thirdWhereNodePair);
        Assert.AreEqual(1, firstWhereNodePair.Length);
        Assert.AreEqual(1, secondWhereNodePair.Length);
        Assert.AreEqual(1, thirdWhereNodePair.Length);
        
        var firstWhereNode = firstWhereNodePair.First().Value;
        var secondWhereNode = secondWhereNodePair.First().Value;
        var thirdWhereNode = thirdWhereNodePair.First().Value;
        
        Assert.AreEqual("x.Population > 100", firstWhereNode.Expression.ToString());
        Assert.AreEqual("1 = 1 and 1 = 1", secondWhereNode.Expression.ToString());
        Assert.AreEqual("1 = 1 and c.Population = 200", thirdWhereNode.Expression.ToString());
    }
}
