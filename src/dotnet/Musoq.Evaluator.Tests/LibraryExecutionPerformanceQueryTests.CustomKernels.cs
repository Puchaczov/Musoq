using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionPerformanceQueryTests
{
    [TestMethod]
    public void MultipleArgumentKernel_ShouldExecuteTypedQuery()
    {
        using var query = InstanceCreator.CompileForExecution(
            "select ScaledTotal(Amount, 2) as Total from #schema.first()", Guid.NewGuid().ToString(),
            LibraryExecutionFixture.CreateProvider(), LoggerResolver, TestCompilationOptions);
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Total", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [12m]);
        AssertNoExecutionBoxes(query);
    }
}
