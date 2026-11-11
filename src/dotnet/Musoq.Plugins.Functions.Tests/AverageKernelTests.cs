using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public sealed class AverageKernelTests
{
    [TestMethod]
    public void ConcreteIntegralAverageOperations_ShouldPreserveAverageAndCount()
    {
        var state = new AverageKernel<int, decimal>.State();
        AverageKernel<int, decimal>.Set(ref state, 0);
        AverageKernel<int, decimal>.Set(ref state, 50);
        AverageKernel<int, decimal>.Set(ref state, 100);

        Assert.AreEqual(50m, AverageKernel<int, decimal>.Get(in state));
        Assert.AreEqual(3L, state.Count);
    }
}
