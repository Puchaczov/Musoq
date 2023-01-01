using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.EnvironmentVariable;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class EnvironmentVariablesTests : EnvironmentVariablesTestBase
{
    [TestMethod]
    public void WhenPassedEnvironmentVariables_ShouldListThemAll()
    {
        var query = "select Key, Value from #EnvironmentVariables.All()";
        var sources = new Dictionary<string, IEnumerable<EnvironmentVariableEntity>>
        {
            {
                "#EnvironmentVariables",
                Array.Empty<EnvironmentVariableEntity>()
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources, new Dictionary<uint, IReadOnlyDictionary<string, string>>()
        {
            {
                0, new Dictionary<string, string>()
                {
                    {"KEY_1", "VALUE_1"},
                    {"KEY_2", "VALUE_2"}
                }
            }
        });
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
        var sources = new Dictionary<string, IEnumerable<EnvironmentVariableEntity>>
        {
            {
                "#EnvironmentVariables",
                Array.Empty<EnvironmentVariableEntity>()
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources, new Dictionary<uint, IReadOnlyDictionary<string, string>>()
        {
            {
                0, new Dictionary<string, string>()
                {
                    {"KEY_1", "VALUE_1"},
                    {"KEY_2", "VALUE_2"}
                }
            },
            {
                1, new Dictionary<string, string>()
                {
                    {"KEY_1", "VALUE_3"},
                    {"KEY_2", "VALUE_4"}
                }
            }
        });
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
        var sources = new Dictionary<string, IEnumerable<EnvironmentVariableEntity>>
        {
            {
                "#EnvironmentVariables",
                Array.Empty<EnvironmentVariableEntity>()
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources, new Dictionary<uint, IReadOnlyDictionary<string, string>>()
        {
            {
                0, new Dictionary<string, string>()
                {
                    {"KEY_1", "VALUE_1"},
                    {"KEY_2", "VALUE_2"}
                }
            },
            {
                1, new Dictionary<string, string>()
                {
                    {"KEY_3", "VALUE_3"},
                    {"KEY_4", "VALUE_4"}
                }
            }
        });
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
}