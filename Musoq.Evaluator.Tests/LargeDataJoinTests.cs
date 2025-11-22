using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Multi;
using Musoq.Evaluator.Tests.Schema.Multi.First;
using Musoq.Evaluator.Tests.Schema.Multi.Second;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class LargeDataJoinTests : MultiSchemaTestBase
{
    [TestMethod]
    public void InnerJoin_LargeDataset_ShouldWorkCorrectly()
    {
        const int size = 10000;
        const string query = "select first.FirstItem, second.FirstItem from #schema.first() first inner join #schema.second() second on first.FirstItem = second.FirstItem";
        
        var first = Enumerable.Range(0, size).Select(i => new FirstEntity { FirstItem = i.ToString() }).ToArray();
        var second = Enumerable.Range(0, size).Select(i => new SecondEntity { FirstItem = i.ToString() }).ToArray();
        
        var vm = CreateAndRunVirtualMachine(query, first, second, new CompilationOptions(useHashJoin: true));
        
        var table = vm.Run();
        
        Assert.AreEqual(size, table.Count);
    }

    [TestMethod]
    public void LeftOuterJoin_LargeDataset_ShouldWorkCorrectly()
    {
        const int size = 10000;
        const string query = "select first.FirstItem, second.FirstItem from #schema.first() first left outer join #schema.second() second on first.FirstItem = second.FirstItem";
        
        var first = Enumerable.Range(0, size).Select(i => new FirstEntity { FirstItem = i.ToString() }).ToArray();
        var second = Enumerable.Range(size / 2, size).Select(i => new SecondEntity { FirstItem = i.ToString() }).ToArray();
        
        var vm = CreateAndRunVirtualMachine(query, first, second, new CompilationOptions(useHashJoin: true));
        
        var table = vm.Run();
        
        Assert.AreEqual(size, table.Count);
        
        var matchedRow = table.Single(r => (string)r[0] == (size - 1).ToString());
        Assert.AreEqual((size - 1).ToString(), matchedRow[1]);

        var unmatchedRow = table.Single(r => (string)r[0] == "0");
        Assert.IsNull(unmatchedRow[1]);
    }

    [TestMethod]
    public void RightOuterJoin_LargeDataset_ShouldWorkCorrectly()
    {
        const int size = 10000;
        const string query = "select first.FirstItem, second.FirstItem from #schema.first() first right outer join #schema.second() second on first.FirstItem = second.FirstItem";
        
        var first = Enumerable.Range(0, size).Select(i => new FirstEntity { FirstItem = i.ToString() }).ToArray();
        var second = Enumerable.Range(size / 2, size).Select(i => new SecondEntity { FirstItem = i.ToString() }).ToArray();
        
        var vm = CreateAndRunVirtualMachine(query, first, second, new CompilationOptions(useHashJoin: true));
        
        var table = vm.Run();
        
        Assert.AreEqual(size, table.Count);
        
        var matchedRow = table.Single(r => (string)r[1] == (size / 2).ToString());
        Assert.AreEqual((size / 2).ToString(), matchedRow[0]);

        var unmatchedRow = table.Single(r => (string)r[1] == (size + size / 2 - 1).ToString());
        Assert.IsNull(unmatchedRow[0]);
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
