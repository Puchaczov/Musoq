using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;

namespace Musoq.Evaluator.Tests;

public sealed partial class RuntimeV2MaintainabilityBudgetTests
{
    [TestMethod]
    public void CountDistinctOverloads_ShouldResolveTypedKernelsWithMergeSupport()
    {
        var overloads = typeof(LibraryBase)
            .GetMethods()
            .Where(method => method.Name == nameof(LibraryBase.CountDistinct))
            .ToArray();

        Assert.HasCount(15, overloads);
        foreach (var overload in overloads)
        {
            var descriptor = AggregateKernelDescriptor.Create(overload);

            Assert.AreEqual(typeof(long), descriptor.ResultType, overload.ToString());
            Assert.AreEqual(1, descriptor.InputShape.ArgumentTypes.Count, overload.ToString());
            Assert.AreEqual(overload.GetParameters()[0].ParameterType, descriptor.InputShape.ArgumentTypes[0], overload.ToString());
            Assert.IsTrue(descriptor.SupportsMerge, overload.ToString());
            Assert.IsNotNull(descriptor.MergeMethod, overload.ToString());
            Assert.AreEqual(descriptor.KernelType, descriptor.MergeMethod!.DeclaringType, overload.ToString());
        }
    }
}
