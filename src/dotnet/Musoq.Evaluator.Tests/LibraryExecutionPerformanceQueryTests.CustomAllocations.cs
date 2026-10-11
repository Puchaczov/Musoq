using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionPerformanceQueryTests
{
    [TestMethod]
    public void MultipleArgumentKernel_ShouldExecuteTypedQueryAndAllocateNoInvocationArrays()
    {
        using var query = InstanceCreator.CompileForExecution(
            "select ScaledTotal(Amount, 2) as Total from #schema.first()", Guid.NewGuid().ToString(),
            LibraryExecutionFixture.CreateProvider(), LoggerResolver, TestCompilationOptions);
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Total", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [12m]);
        AssertNoExecutionBoxes(query);
        var measured = AllocationTestThread.Run(MeasureScaledKernel);
        Assert.AreEqual(50000m, measured.Checksum);
        Assert.AreEqual(0L, measured.Allocated);
    }

    private static (long Allocated, decimal Checksum) MeasureScaledKernel()
    {
        _ = ConsumeScaledKernel(10000);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var checksum = ConsumeScaledKernel(100000);
        return (GC.GetAllocatedBytesForCurrentThread() - before, checksum);
    }

    private static decimal ConsumeScaledKernel(int iterations)
    {
        var state = default(LibraryExecutionLibrary.ScaledTotalKernel.State);
        var checksum = 0m;
        for (var iteration = 0; iteration < iterations; iteration++)
        {
            LibraryExecutionLibrary.ScaledTotalKernel.Reset(ref state);
            LibraryExecutionLibrary.ScaledTotalKernel.Set(ref state, 1.25m, 2);
            LibraryExecutionLibrary.ScaledTotalKernel.Set(ref state, null, 2);
            LibraryExecutionLibrary.ScaledTotalKernel.Set(ref state, -1m, 2);
            checksum += LibraryExecutionLibrary.ScaledTotalKernel.Get(in state)!.Value;
        }
        return checksum;
    }
}
