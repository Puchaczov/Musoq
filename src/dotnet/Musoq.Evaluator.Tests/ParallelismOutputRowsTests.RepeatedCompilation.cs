using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class ParallelismOutputRowsTests
{
    [TestMethod]
    public void StressTest_MultipleIterations_WithParallelization_ShouldAlwaysReturnSameRowCount()
    {
        const int rowCount = 2000;
        const int iterations = 10;
        const string query = "select Name, Id from #A.Entities()";

        var entities = CreateBasicEntitiesWithIds(rowCount);
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", entities }
        };
        var schemaProvider = new BasicSchemaProvider<BasicEntity>(sources);
        var options = new CompilationOptions(ParallelizationMode.Full);

        for (var i = 0; i < iterations; i++)
        {
            var vm = InstanceCreator.CompileForExecution(
                query,
                "ParallelismStressRows",
                schemaProvider,
                LoggerResolver,
                options);
            var table = TableMaterializationTestHelper.Materialize(vm.Run());

            Assert.AreEqual(rowCount, table.Count, $"Iteration {i}: Expected {rowCount} rows but got {table.Count}");
        }
    }

    [TestMethod]
    public void StressTest_MultipleIterations_WithFilter_ShouldAlwaysReturnConsistentResults()
    {
        const int rowCount = 2000;
        const int iterations = 10;
        const string query = "select Name, Id from #A.Entities() where Id % 5 = 0";

        var entities = CreateBasicEntitiesWithIds(rowCount);
        var expectedCount = entities.Count(e => e.Id % 5 == 0);
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", entities }
        };
        var schemaProvider = new BasicSchemaProvider<BasicEntity>(sources);
        var options = new CompilationOptions(ParallelizationMode.Full);

        for (var i = 0; i < iterations; i++)
        {
            var vm = InstanceCreator.CompileForExecution(
                query,
                "ParallelismStressFilter",
                schemaProvider,
                LoggerResolver,
                options);
            var table = TableMaterializationTestHelper.Materialize(vm.Run());

            Assert.AreEqual(expectedCount, table.Count,
                $"Iteration {i}: Expected {expectedCount} rows but got {table.Count}");
        }
    }
}
