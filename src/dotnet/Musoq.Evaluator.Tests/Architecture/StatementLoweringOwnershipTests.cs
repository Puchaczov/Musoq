using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution.Lowering;
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
        Assert.IsFalse(typeof(PipelineLoweringService).IsNested);
        Assert.IsFalse(typeof(MultiStatementLoweringService).IsNested);
        Assert.IsFalse(typeof(DescLoweringService).IsNested);
        Assert.AreEqual(typeof(IPipelineLoweringOperations), SingleParameter(typeof(PipelineLoweringService)));
        Assert.AreEqual(typeof(IMultiStatementLoweringOperations), SingleParameter(typeof(MultiStatementLoweringService)));
        Assert.AreEqual(typeof(IDescLoweringOperations), SingleParameter(typeof(DescLoweringService)));
    }

    private static void AssertServiceParameter<TLowerer, TService>()
    {
        var parameter = typeof(TLowerer).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single().GetParameters().Single();
        Assert.AreEqual(typeof(TService), parameter.ParameterType);
    }

    private static Type SingleParameter(Type type) => type.GetConstructors(
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Single().GetParameters().Single().ParameterType;
}
