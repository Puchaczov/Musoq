using Microsoft.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution.Lowering.Coordinators;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class CteLoweringOwnershipTests
{
    [TestMethod]
    public void CtePlanLowerer_ShouldDependOnCteServiceContract()
    {
        var parameter = CompilerContractSymbols.For(typeof(CtePlanLowerer))
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single()
            .GetParameters()
            .Single();

        Assert.IsTrue(SymbolEqualityComparer.Default.Equals(CompilerContractSymbols.For(typeof(ICteLoweringService)), parameter.ParameterType));
        Assert.IsFalse(CompilerContractSymbols.For(typeof(CteLoweringService)).IsNested);
        Assert.IsTrue(SymbolEqualityComparer.Default.Equals(CompilerContractSymbols.For(typeof(ICteLoweringOperations)), CompilerContractSymbols.For(typeof(CteLoweringService)).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Single().GetParameters().Single().ParameterType));
    }
}
