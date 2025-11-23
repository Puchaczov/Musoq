using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Multi;
using Musoq.Evaluator.Tests.Schema.Multi.First;
using Musoq.Evaluator.Tests.Schema.Multi.Second;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class HashJoinIntegrationTests : MultiSchemaTestBase
{
    [TestMethod]
    public void LeftOuterJoin_WithHashJoinEnabled_ShouldUseHashJoin()
    {
        const string query = "select first.FirstItem, second.FirstItem from #schema.first() first left outer join #schema.second() second on first.FirstItem = second.FirstItem order by first.FirstItem";
        
        var first = new[] { new FirstEntity { FirstItem = "1" }, new FirstEntity { FirstItem = "2" } };
        var second = new[] { new SecondEntity { FirstItem = "1" }, new SecondEntity { FirstItem = "3" } };
        
        var vm = CreateAndRunVirtualMachine(query, first, second, new CompilationOptions(useHashJoin: true));
        
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Count);
        
        Assert.AreEqual("1", table[0][0]);
        Assert.AreEqual("1", table[0][1]);
        
        Assert.AreEqual("2", table[1][0]);
        Assert.IsNull(table[1][1]);
    }

    [TestMethod]
    public void RightOuterJoin_WithHashJoinEnabled_ShouldUseHashJoin()
    {
        const string query = "select first.FirstItem, second.FirstItem from #schema.first() first right outer join #schema.second() second on first.FirstItem = second.FirstItem order by second.FirstItem";
        
        var first = new[] { new FirstEntity { FirstItem = "1" }, new FirstEntity { FirstItem = "2" } };
        var second = new[] { new SecondEntity { FirstItem = "1" }, new SecondEntity { FirstItem = "3" } };
        
        var vm = CreateAndRunVirtualMachine(query, first, second, new CompilationOptions(useHashJoin: true));
        
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Count);
        
        Assert.AreEqual("1", table[0][0]);
        Assert.AreEqual("1", table[0][1]);
        
        Assert.IsNull(table[1][0]);
        Assert.AreEqual("3", table[1][1]);
    }

    [TestMethod]
    public void InnerJoin_WithHashJoinDisabled_ShouldUseNestedLoop()
    {
        const string query = "select first.FirstItem, second.FirstItem from #schema.first() first inner join #schema.second() second on first.FirstItem = second.FirstItem";
        
        var first = new[] { new FirstEntity { FirstItem = "1" }, new FirstEntity { FirstItem = "2" } };
        var second = new[] { new SecondEntity { FirstItem = "1" }, new SecondEntity { FirstItem = "3" } };
        
        var vm = CreateAndRunVirtualMachine(query, first, second, new CompilationOptions(useHashJoin: false));
        
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("1", table[0][0]);
        Assert.AreEqual("1", table[0][1]);
    }

    protected CompiledQuery CreateAndRunVirtualMachine(
        string script,
        FirstEntity[] first,
        SecondEntity[] second,
        CompilationOptions options)
    {
        var schema = new MultiSchema(new Dictionary<string, (ISchemaTable SchemaTable, RowSource RowSource)>()
        {
            {"first", (new FirstEntityTable(), new MultiRowSource<FirstEntity>(first, FirstEntity.TestNameToIndexMap, FirstEntity.TestIndexToObjectAccessMap))},
            {"second", (new SecondEntityTable(), new MultiRowSource<SecondEntity>(second, SecondEntity.TestNameToIndexMap, SecondEntity.TestIndexToObjectAccessMap))}
        });
        
        return InstanceCreator.CompileForExecution(
            script, 
            Guid.NewGuid().ToString(), 
            new MultiSchemaProvider(new Dictionary<string, ISchema>()
            {
                {"#schema", schema}
            }),
            LoggerResolver,
            options);
    }
}
