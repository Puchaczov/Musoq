using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed partial class LibraryExecutionPerformanceQueryTests : GenericEntityTestBase
{
    [TestMethod]
    public void GroupedNumericExecution_ShouldConsumeExpectedTypedResultsAndEmitNoBoxes()
    {
        const string sql = "select Partition, Sum(Amount) as Total, Avg(Integral) as Average, " +
            "Count(Amount) as Present from #schema.first() group by Partition order by Partition";
        using var query = InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString(),
            LibraryExecutionFixture.CreateProvider(), LoggerResolver, TestCompilationOptions);
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Partition", typeof(string)),
            ("Total", typeof(decimal?)), ("Average", typeof(decimal?)), ("Present", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["A", 4m, 1.5m, 2L], ["B", 2m, 1m, 2L]);
        AssertNoExecutionBoxes(query);
    }

    [TestMethod]
    public void CustomKernelExecution_ShouldConsumeExpectedResultsAndEmitNoBoxes()
    {
        const string sql = "select Partition, PositiveTotal(Amount) as Positive, ScaledTotal(Amount, 2) as Scaled " +
            "from #schema.first() group by Partition order by Partition";
        using var query = InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString(),
            LibraryExecutionFixture.CreateProvider(), LoggerResolver, TestCompilationOptions);
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Partition", typeof(string)),
            ("Positive", typeof(decimal?)), ("Scaled", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["A", 4m, 8m], ["B", 5m, 4m]);
        AssertNoExecutionBoxes(query);
    }

    private static void AssertNoExecutionBoxes(CompiledQuery query)
    {
        var boxed = GeneratedExecutionIlInspection.GetGeneratedComputeMethods(query)
            .Where(GeneratedExecutionIlInspection.ContainsBoxOpcode)
            .Select(static method => method.DeclaringType + "." + method.Name).ToArray();
        Assert.IsEmpty(boxed, "Generated numeric and custom execution must not box values: " + string.Join(", ", boxed));
    }
}
