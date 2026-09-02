using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

[TestClass]
[DoNotParallelize]
public sealed class GeneratedCodeSamplesLoopInvariantExecutionTests
{
    [TestMethod]
    public void StableApplyProjection_ShouldReturnAllRowsWithStableValues()
    {
        using var query = Compile("Q248_LoopInvariantStableApplyProjection.cs");
        using var table = query.Run();

        Assert.AreEqual(24, table.Count);
        Assert.AreEqual(10, table[0][0]);
        Assert.AreEqual(101, table[0][1]);
        Assert.AreEqual(1011, table[0][2]);
    }

    [TestMethod]
    public void VolatileApplyProjection_ShouldPreserveResultParity()
    {
        using var query = Compile("Q249_LoopInvariantVolatileApplyProjection.cs");
        using var table = query.Run();

        Assert.AreEqual(24, table.Count);
        Assert.IsTrue(table.Rows.All(row => row[0] is int));
        Assert.AreEqual(100, table[0][0]);
    }

    [TestMethod]
    public void StableAndVolatileFunctions_ShouldProduceExpectedValues()
    {
        using var query = Compile("Q250_LoopInvariantStableAndVolatileFunctions.cs");
        using var table = query.Run();

        Assert.AreEqual(6, table.Count);
        Assert.AreEqual(11, table[0][0]);
        Assert.AreEqual(111, table[0][1]);
        Assert.AreEqual(103, table[0][2]);
    }

    [TestMethod]
    public void EmptyApply_ShouldReturnNoRowsWithoutChangingTheSchema()
    {
        using var query = Compile("Q251_LoopInvariantEmptyApply.cs");
        using var table = query.Run();

        Assert.AreEqual(0, table.Count);
        Assert.AreEqual(2, table.Columns.Count());
    }

    private static CompiledQuery Compile(string fileName)
    {
        var sample = GeneratedCodeSamplesCatalog.GetByFileName(fileName);
        return InstanceCreator.CompileForExecution(
            sample.Query,
            $"GeneratedSample_{fileName.Replace('.', '_')}",
            sample.CreateSchemaProvider(),
            new Components.TestsLoggerResolver(),
            sample.CompilationOptions);
    }
}
