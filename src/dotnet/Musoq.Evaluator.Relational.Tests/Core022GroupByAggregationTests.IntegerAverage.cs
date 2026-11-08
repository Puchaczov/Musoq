using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

public sealed partial class Core022GroupByAggregationTests
{
    [TestMethod]
    public void CoupledIntegerAverage_ShouldReturnFractionalDecimalMean()
    {
        const string query = "table Items { Value: int }; couple system.range with table Items as CoupledRange; select avg(valueRow.Value) as Average from CoupledRange(1, 3) valueRow";
        using var compiled = InstanceCreator.CompileForExecution(
            query, 
            Guid.NewGuid().ToString(), 
            new SpecStressSystemRangeSchemaProvider(), 
            LoggerResolver, 
            TestCompilationOptions);
        using var result = TableMaterializationTestHelper.Materialize(compiled.Run());

        TableMaterializationTestHelper.AssertColumns(result, ("Average", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, new object?[] { 1.5m });
    }
}
