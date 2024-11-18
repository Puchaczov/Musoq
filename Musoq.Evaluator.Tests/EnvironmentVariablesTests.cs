using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.EnvironmentVariable;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class EnvironmentVariablesTests : EnvironmentVariablesTestBase
{
    [TestMethod]
    public void WhenDescEnvironmentVariables_ShouldListAllColumns()
    {
        var query = "desc #EnvironmentVariables.All()";
        var sources = new Dictionary<uint, IEnumerable<EnvironmentVariableEntity>>
        {
            {
                0,
                Array.Empty<EnvironmentVariableEntity>()
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        
        var table = vm.Run();
        
        Assert.AreEqual(6, table.Count);
        Assert.AreEqual("Key", table[0][0]);
        Assert.AreEqual("Key.Chars", table[1][0]);
        Assert.AreEqual("Key.Length", table[2][0]);
        Assert.AreEqual("Value", table[3][0]);
        Assert.AreEqual("Value.Chars", table[4][0]);
        Assert.AreEqual("Value.Length", table[5][0]);
    }
    
    [TestMethod]
    public void WhenPassedEnvironmentVariables_ShouldListThemAll()
    {
        var query = "select Key, Value from #EnvironmentVariables.All()";
        
        var sources = new Dictionary<uint, IEnumerable<EnvironmentVariableEntity>>
        {
            {
                0,
                [
                    new ("KEY_1", "VALUE_1"),
                    new ("KEY_2", "VALUE_2")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("KEY_1", table[0][0]);
        Assert.AreEqual("VALUE_1", table[0][1]);
        Assert.AreEqual("KEY_2", table[1][0]);
        Assert.AreEqual("VALUE_2", table[1][1]);
    }

    [TestMethod]
    public void WhenPassedEnvironmentVariables_JoinedDataSources_ShouldListThemAll()
    {
        var query = "select e1.Key, e1.Value, e2.Value from #EnvironmentVariables.All() e1 inner join #EnvironmentVariables.All() e2 on e1.Key = e2.Key";
        var sources = new Dictionary<uint, IEnumerable<EnvironmentVariableEntity>>
        {
            {
                0,
                [
                    new("KEY_1", "VALUE_1"),
                    new("KEY_2", "VALUE_2"),
                ]
            },
            {
                1,
                [
                    new("KEY_1", "VALUE_3"),
                    new("KEY_2", "VALUE_4")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Count);
        
        Assert.AreEqual("KEY_1", table[0][0]);
        Assert.AreEqual("VALUE_1", table[0][1]);
        Assert.AreEqual("VALUE_3", table[0][2]);
        
        Assert.AreEqual("KEY_2", table[1][0]);
        Assert.AreEqual("VALUE_2", table[1][1]);
        Assert.AreEqual("VALUE_4", table[1][2]);
    }
    
    [TestMethod]
    public void WhenPassedEnvironmentVariables_UnionDataSources_ShouldListThemAll()
    {
        var query = "select Key, Value from #EnvironmentVariables.All() union all (Key) select Key, Value from #EnvironmentVariables.All()";
        var sources = new Dictionary<uint, IEnumerable<EnvironmentVariableEntity>>
        {
            {
                0,
                [
                    new("KEY_1", "VALUE_1"),
                    new("KEY_2", "VALUE_2")
                ]
            },
            {
                1,
                [
                    new("KEY_3", "VALUE_3"),
                    new("KEY_4", "VALUE_4")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(4, table.Count);
        
        Assert.AreEqual("KEY_1", table[0][0]);
        Assert.AreEqual("VALUE_1", table[0][1]);
        
        Assert.AreEqual("KEY_2", table[1][0]);
        Assert.AreEqual("VALUE_2", table[1][1]);
            
        Assert.AreEqual("KEY_3", table[2][0]);
        Assert.AreEqual("VALUE_3", table[2][1]);
        
        Assert.AreEqual("KEY_4", table[3][0]);
        Assert.AreEqual("VALUE_4", table[3][1]);
    }

    [TestMethod]
    public void WhenPreviouslyCteExpressionSaw_ShouldPass()
    {
        const string query = @"
with p as ( 
    select 1 from #A.entities()
)
select Key, Value from #EnvironmentVariables.All()";
        
        var basicEntitiesSource = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                Array.Empty<BasicEntity>()
            }
        };
        
        var environmentVariablesEntitiesSource = new Dictionary<string, IEnumerable<EnvironmentVariableEntity>>
        {
            {
                "#EnvironmentVariables",
                [
                    new("KEY_1", "VALUE_1"),
                    new("KEY_2", "VALUE_2")
                ]
            }
        };
        
        var environmentVariablesSource = new Dictionary<uint, IEnumerable<EnvironmentVariableEntity>>
        {
            {
                0,
                Array.Empty<EnvironmentVariableEntity>()
            },
            {
                1,
                [
                    new("KEY_1", "VALUE_1"),
                    new("KEY_2", "VALUE_2")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(
            query, 
            basicEntitiesSource,
            environmentVariablesEntitiesSource,
            environmentVariablesSource);
        
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("KEY_1", table[0][0]);
        Assert.AreEqual("VALUE_1", table[0][1]);
        Assert.AreEqual("KEY_2", table[1][0]);
        Assert.AreEqual("VALUE_2", table[1][1]);
    }
}