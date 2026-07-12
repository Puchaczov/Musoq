using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class BuildItemsExecutionTargetTests
{
    [TestMethod]
    public void ExecutionTarget_WhenMissing_ShouldDefaultToCSharpClr()
    {
        var items = new BuildItems();

        Assert.AreEqual(ExecutionTargetIds.CSharpClr, items.ExecutionTarget);
    }

    [TestMethod]
    public void ExecutionTarget_WhenSet_ShouldRoundTripThroughBuildItems()
    {
        var items = new BuildItems
        {
            ExecutionTarget = TestExecutionTargetIds.TestOnlyNonClr
        };

        Assert.AreEqual(TestExecutionTargetIds.TestOnlyNonClr, items.ExecutionTarget);
    }

    [TestMethod]
    public void ExecutionTargetId_ShouldBeStringBackedValueObject()
    {
        Assert.IsFalse(typeof(ExecutionTargetId).IsEnum);
        Assert.AreEqual("CSharpClr", ExecutionTargetIds.CSharpClr.ToString());
        Assert.AreEqual(ExecutionTargetIds.CSharpClr, new ExecutionTargetId("CSharpClr"));
    }

    [TestMethod]
    public void ExecutionTarget_ShouldRemainInternalImplementationDetail()
    {
        Assert.IsFalse(typeof(ExecutionTargetId).IsPublic);

        var publicMethods = typeof(InstanceCreator)
            .GetMethods()
            .Where(static method => method.IsPublic)
            .ToArray();

        var exposed = publicMethods
            .Where(static method =>
                method.ReturnType == typeof(ExecutionTargetId) ||
                method.GetParameters().Any(static parameter => parameter.ParameterType == typeof(ExecutionTargetId)))
            .Select(static method => method.Name)
            .ToArray();

        Assert.IsEmpty(exposed, "Execution target selection must not be exposed publicly in this milestone.");
    }
}
