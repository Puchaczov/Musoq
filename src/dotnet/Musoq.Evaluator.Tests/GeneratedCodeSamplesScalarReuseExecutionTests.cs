using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

[TestClass]
[DoNotParallelize]
public sealed class GeneratedCodeSamplesScalarReuseExecutionTests
{
    [TestMethod]
    public void VolatileFilterProjection_ShouldPreserveRowsAndValues()
    {
        using var query = Compile("Q253_VolatileFilterProjectionReuse.cs");
        using var table = query.Run();

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Rows.All(row => row[0] is int));
        Assert.AreEqual(100, table[0][0]);
    }

    [TestMethod]
    public void VolatileWindowInputs_ShouldPreserveRowNumberAndValues()
    {
        using var query = Compile("Q255_VolatileWindowInputs.cs");
        using var table = query.Run();

        Assert.AreEqual(2, table.Count);
        Assert.IsTrue(table.Rows.All(row => row[0] is int));
        Assert.AreEqual(1L, table[0][1]);
    }

    [TestMethod]
    public void GuardedStableApply_ShouldReturnEveryMatchingChild()
    {
        using var query = Compile("Q258_GuardedStableApplyPredicate.cs");
        using var table = query.Run();

        Assert.AreEqual(6, table.Count);
        Assert.AreEqual(10, table[0][0]);
        Assert.AreEqual(101, table[0][1]);
    }

    [TestMethod]
    public void GuardedVolatileOuterApply_ShouldPreserveOuterRows()
    {
        using var query = Compile("Q259_GuardedVolatileOuterApplyPredicate.cs");
        using var table = query.Run();

        Assert.AreEqual(6, table.Count);
        Assert.IsTrue(table.Rows.All(row => row[0] is int));
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
