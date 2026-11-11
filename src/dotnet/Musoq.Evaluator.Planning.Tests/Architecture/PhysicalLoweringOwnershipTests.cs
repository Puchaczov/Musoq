using System;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class PhysicalLoweringOwnershipTests
{
    [TestMethod]
    public void Facade_ShouldOwnTheOnlyImplementationCompositionBoundary()
    {
        var field = CompilerContractSymbols
            .For(typeof(PhysicalLoweringFacade))
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single();

        Assert.AreEqual("_implementation", field.MetadataName);
        Assert.IsTrue(
            SymbolEqualityComparer.Default.Equals(
                CompilerContractSymbols.For(typeof(PhysicalLoweringImplementation)),
                field.FieldType
            )
        );
        Assert.IsFalse(CompilerContractSymbols.For(typeof(PhysicalLoweringImplementation)).IsNested);
    }

    [TestMethod]
    public void LegacyKernelTypeAndFiles_ShouldBeGone()
    {
        Assert.IsNull(
            CompilerContractSymbols
                .For(typeof(PhysicalLoweringImplementation))
                .Assembly.GetType("Musoq.Evaluator.IR.Execution.PhysicalLoweringKernel")
        );

        var implementationTypes = CompilerContractSymbols
            .For(typeof(PhysicalLoweringImplementation))
            .Assembly.GetTypes()
            .Where(static type => type.MetadataName.Contains("PhysicalLoweringKernel", StringComparison.Ordinal))
            .ToArray();

        Assert.IsEmpty(implementationTypes);
    }
}
