using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class CompilationOptionsTests
{
    [TestMethod]
    public void Constructor_Defaults_ShouldUseCloudCtePosture()
    {
        var options = new CompilationOptions();

        Assert.AreEqual(ParallelizationMode.Full, options.ParallelizationMode);
        Assert.IsTrue(options.UseHashJoin);
        Assert.IsTrue(options.UseSortMergeJoin);
        Assert.IsTrue(options.UseCommonSubexpressionElimination);
        Assert.IsTrue(options.UseConstantFolding);
        Assert.IsTrue(options.UsePrimitiveTypeValidation);
        Assert.IsTrue(options.UseCteParallelization);
        Assert.IsTrue(options.UseCteSidecarIndexes);
    }

    [TestMethod]
    public void WithInstrumentationMode_WhenTableResultMaterializationIsForced_ShouldPreserveFlag()
    {
        var options = new CompilationOptions()
            .WithTableResultMaterialization()
            .WithInstrumentationMode(QueryInstrumentationMode.Full);

        Assert.IsTrue(options.ForceTableResultMaterialization);
        Assert.AreEqual(QueryInstrumentationMode.Full, options.InstrumentationMode);
        Assert.IsTrue(options.UseCteParallelization);
        Assert.IsTrue(options.UseCteSidecarIndexes);
    }
}
