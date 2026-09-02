using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void LoopInvariantStableApplySample_ShouldContainOuterAndMiddleEagerLocals()
    {
        var generated = ReadSample("Q248_LoopInvariantStableApplyProjection.cs").Content;

        StringAssert.Contains(generated, "int aValue =");
        Assert.IsTrue(
            generated.Contains("int abaValue =", StringComparison.Ordinal) ||
            generated.Contains("int abValue =", StringComparison.Ordinal),
            "The middle-loop stable value should be represented by an eager local.");
        StringAssert.Contains(generated, "c.Value");
        AssertNoLazyHoistingGuard(generated);
    }

    [TestMethod]
    public void LoopInvariantVolatileApplySample_ShouldKeepVolatileReadAtLeaf()
    {
        var generated = ReadSample("Q249_LoopInvariantVolatileApplyProjection.cs").Content;

        Assert.IsFalse(generated.Contains("var aVolatileValue =", StringComparison.Ordinal));
        StringAssert.Contains(generated, "a.VolatileValue");
        AssertNoLazyHoistingGuard(generated);
    }

    [TestMethod]
    public void LoopInvariantFunctionSample_ShouldHoistStableFunctionsButKeepVolatileFunctionAtLeaf()
    {
        var generated = ReadSample("Q250_LoopInvariantStableAndVolatileFunctions.cs").Content;

        Assert.IsTrue(generated.Contains("StableOf", StringComparison.Ordinal));
        Assert.IsTrue(generated.Contains("StablePair", StringComparison.Ordinal));
        Assert.IsTrue(generated.Contains("VolatileOf", StringComparison.Ordinal));
        AssertNoLazyHoistingGuard(generated);
    }

    [TestMethod]
    public void LoopInvariantEmptyApplySample_ShouldPlaceStableReadBeforeEmptyDescendant()
    {
        var generated = ReadSample("Q251_LoopInvariantEmptyApply.cs").Content;

        var localIndex = generated.IndexOf("var aValue =", StringComparison.Ordinal);
        if (localIndex < 0)
            localIndex = generated.IndexOf("int aValue =", StringComparison.Ordinal);

        Assert.IsGreaterThanOrEqualTo(0, localIndex);
        var loopIndex = generated.IndexOf("foreach", localIndex, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, loopIndex);
        Assert.IsLessThan(loopIndex, localIndex);
        StringAssert.Contains(generated, "b.VolatileValue");
        AssertNoLazyHoistingGuard(generated);
    }

    private static void AssertNoLazyHoistingGuard(string generated)
    {
        Assert.IsFalse(generated.Contains("??=", StringComparison.Ordinal));
        Assert.IsFalse(generated.Contains("initialized", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(generated.Contains("cache", StringComparison.OrdinalIgnoreCase));
    }
}
