using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class MethodInvocationTests : BasicEntityTestBase
{
    [TestMethod]
    public void MethodInvocationOnAliasFinishedWithNumber_ShouldPass()
    {
        var query = "select population2.GetPopulation() from #A.entities() population2";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("KATOWICE", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(500m, table[0].Values[0]);
        Assert.AreEqual(400m, table[1].Values[0]);
        Assert.AreEqual(250m, table[2].Values[0]);
        Assert.AreEqual(250m, table[3].Values[0]);
        Assert.AreEqual(350m, table[4].Values[0]);
    }
    
    [TestMethod]
    public void WhenNullableBooleanFieldWithFullSyntaxApplied_ShouldPass()
    {
        var query = "select City from #A.entities() population2 where Contains(City, 'W') = true";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("KATOWICE", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(3, table.Count);
        
        Assert.AreEqual("WARSAW", table[0].Values[0]);
        Assert.AreEqual("CZESTOCHOWA", table[1].Values[0]);
        Assert.AreEqual("KATOWICE", table[2].Values[0]);
    }
    
    [TestMethod]
    public void WhenNullableBooleanFieldWithShortenedSyntaxApplied_ShouldPass()
    {
        var query = "select City from #A.entities() population2 where Contains(City, 'W')";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("KATOWICE", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(3, table.Count);
        
        Assert.AreEqual("WARSAW", table[0].Values[0]);
        Assert.AreEqual("CZESTOCHOWA", table[1].Values[0]);
        Assert.AreEqual("KATOWICE", table[2].Values[0]);
    }
    
    [TestMethod]
    public void WhenNullableBooleanFields_ForBinaryOperator_WithFullSyntaxApplied_ShouldPass()
    {
        var query = "select City from #A.entities() population2 where Contains(City, 'W') = true and Contains(City, 'A') = true";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("KATOWICE", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(3, table.Count);
        
        Assert.AreEqual("WARSAW", table[0].Values[0]);
        Assert.AreEqual("CZESTOCHOWA", table[1].Values[0]);
        Assert.AreEqual("KATOWICE", table[2].Values[0]);
    }
    
    [TestMethod]
    public void WhenNullableBooleanFields_ForBinaryOperator_WithShortenedSyntaxApplied_ShouldPass()
    {
        var query = "select City from #A.entities() population2 where Contains(City, 'W') and Contains(City, 'A')";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("KATOWICE", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(3, table.Count);
        
        Assert.AreEqual("WARSAW", table[0].Values[0]);
        Assert.AreEqual("CZESTOCHOWA", table[1].Values[0]);
        Assert.AreEqual("KATOWICE", table[2].Values[0]);
    }
    
    [TestMethod]
    public void WhenNullableBooleanFields_ForBinaryOperator_WithMixedSyntaxApplied_FirstExpression_ShouldPass()
    {
        var query = "select City from #A.entities() population2 where Contains(City, 'W') = true and Contains(City, 'A')";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("KATOWICE", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(3, table.Count);
        
        Assert.AreEqual("WARSAW", table[0].Values[0]);
        Assert.AreEqual("CZESTOCHOWA", table[1].Values[0]);
        Assert.AreEqual("KATOWICE", table[2].Values[0]);
    }
    
    [TestMethod]
    public void WhenNullableBooleanFields_ForBinaryOperator_WithMixedSyntaxApplied_SecondExpression_ShouldPass()
    {
        var query = "select City from #A.entities() population2 where Contains(City, 'W') and Contains(City, 'A') = true";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("KATOWICE", "POLAND", 250),
                    new BasicEntity("BERLIN", "GERMANY", 250),
                    new BasicEntity("MUNICH", "GERMANY", 350)
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(3, table.Count);
        
        Assert.AreEqual("WARSAW", table[0].Values[0]);
        Assert.AreEqual("CZESTOCHOWA", table[1].Values[0]);
        Assert.AreEqual("KATOWICE", table[2].Values[0]);
    }
    
    [TestMethod]
    public void WhenMethodCallDoesNotHaveAlias_ShouldThrows()
    {
        var query = "select Contains(first.City, 'W') from #A.entities() first inner join #B.entities() second on first.Country = second.Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", []
            },
            {
                "#B", []
            }
        };

        Assert.ThrowsException<AliasMissingException>(() => CreateAndRunVirtualMachine(query, sources));
    }

    [TestMethod]
    public void WhenContextsOfInnerCteShouldTurnContextOfResultingTable_ShouldPass()
    {
        var query = """
                    with first as (
                        select 
                            x.Name as Name
                        from #A.entities() x
                        cross apply x.JustReturnArrayOfString() b
                    )
                    select 
                        p.Value
                    from first b
                    inner join #A.entities() r2 on 1 = 1
                    cross apply r2.MethodArrayOfStrings(r2.TestMethodWithInjectEntityAndParameter(b.Name), r2.TestMethodWithInjectEntityAndParameter(b.Name)) p
                    """;

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>()
        {
            {
                "#A", [
                    new BasicEntity("TEST")
                ]
            }
        };
        
        var vm = CreateAndRunVirtualMachine(query, sources);
        
        var table = vm.Run();
        
        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("TEST", table[0].Values[0]);
        Assert.AreEqual("TEST", table[1].Values[0]);
        Assert.AreEqual("TEST", table[2].Values[0]);
        Assert.AreEqual("TEST", table[3].Values[0]);
    }
}