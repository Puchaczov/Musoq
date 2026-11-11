using Microsoft.CodeAnalysis;
using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution.Lowering.Coordinators;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class StatementLoweringOwnershipTests
{
    [TestMethod]
    public void PlanLowerers_ShouldDependOnDomainServices()
    {
        AssertServiceParameter<PipelinePlanLowerer, IPipelineLoweringService>();
        AssertServiceParameter<MultiStatementPlanLowerer, IMultiStatementLoweringService>();
        AssertServiceParameter<DescPlanLowerer, IDescLoweringService>();
    }

    [TestMethod]
    public void DomainServices_ShouldBeTopLevelAndUseTypedOperations()
    {
        Assert.IsFalse(CompilerContractSymbols.For(typeof(PipelineLoweringService)).IsNested);
        Assert.IsFalse(CompilerContractSymbols.For(typeof(MultiStatementLoweringService)).IsNested);
        Assert.IsFalse(CompilerContractSymbols.For(typeof(DescLoweringService)).IsNested);
        Assert.IsTrue(SymbolEqualityComparer.Default.Equals(CompilerContractSymbols.For(typeof(IPipelineLoweringOperations)), SingleParameter(CompilerContractSymbols.For(typeof(PipelineLoweringService)))));
        Assert.IsTrue(SymbolEqualityComparer.Default.Equals(CompilerContractSymbols.For(typeof(IMultiStatementLoweringOperations)), SingleParameter(CompilerContractSymbols.For(typeof(MultiStatementLoweringService)))));
        Assert.IsTrue(SymbolEqualityComparer.Default.Equals(CompilerContractSymbols.For(typeof(IDescLoweringOperations)), SingleParameter(CompilerContractSymbols.For(typeof(DescLoweringService)))));
    }

    private static void AssertServiceParameter<TLowerer, TService>()
    {
        var parameter = CompilerContractSymbols.For(typeof(TLowerer)).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single().GetParameters().Single();
        Assert.IsTrue(SymbolEqualityComparer.Default.Equals(CompilerContractSymbols.For(typeof(TService)), parameter.ParameterType));
    }

    private static ITypeSymbol SingleParameter(ITypeSymbol type) => type.GetConstructors(
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Single().GetParameters().Single().ParameterType;
}
