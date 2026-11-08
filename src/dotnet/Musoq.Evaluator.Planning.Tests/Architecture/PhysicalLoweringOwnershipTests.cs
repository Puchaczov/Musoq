using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class PhysicalLoweringOwnershipTests
{
    [TestMethod]
    public void Facade_ShouldOwnTheOnlyImplementationCompositionBoundary()
    {
        var field = typeof(PhysicalLoweringFacade).GetFields(
                BindingFlags.Instance | BindingFlags.NonPublic)
            .Single();

        Assert.AreEqual("_implementation", field.Name);
        Assert.AreEqual(typeof(PhysicalLoweringImplementation), field.FieldType);
        Assert.IsFalse(typeof(PhysicalLoweringImplementation).IsNested);
    }

    [TestMethod]
    public void LegacyKernelTypeAndFiles_ShouldBeGone()
    {
        Assert.IsNull(typeof(PhysicalLoweringImplementation).Assembly.GetType(
            "Musoq.Evaluator.IR.Execution.PhysicalLoweringKernel"));

        var implementationTypes = typeof(PhysicalLoweringImplementation).Assembly
            .GetTypes()
            .Where(static type => type.Name.Contains("PhysicalLoweringKernel", StringComparison.Ordinal))
            .ToArray();

        Assert.IsEmpty(implementationTypes);
    }
}
