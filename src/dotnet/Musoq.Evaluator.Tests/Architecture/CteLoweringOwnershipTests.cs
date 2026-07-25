using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution.Lowering;
using Musoq.Evaluator.IR.Execution.Lowering.Coordinators;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class CteLoweringOwnershipTests
{
    [TestMethod]
    public void CtePlanLowerer_ShouldDependOnCteServiceContract()
    {
        var parameter = typeof(CtePlanLowerer)
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single()
            .GetParameters()
            .Single();

        Assert.AreEqual(typeof(ICteLoweringService), parameter.ParameterType);
        Assert.IsFalse(typeof(CteLoweringService).IsNested);
        Assert.AreEqual(
            typeof(ICteLoweringOperations),
            typeof(CteLoweringService).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Single().GetParameters().Single().ParameterType);
    }
}
