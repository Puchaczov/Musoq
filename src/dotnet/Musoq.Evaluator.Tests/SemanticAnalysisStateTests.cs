using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class SemanticAnalysisStateTests
{
    [TestMethod]
    public void Constructor_WhenMultipleStatesAreCreated_ShouldKeepIndependentMutableBags()
    {
        var first = new SemanticAnalysisState();
        var second = new SemanticAnalysisState();

        first.SourceBinding.Identifier = "first";
        first.ResultShape.GeneratedAliases.Add("alias");
        first.MethodResolution.Methods.Push("method");
        first.Diagnostics.NullSuspiciousTypes.Add(typeof(string));
        first.Query.SetOperatorFieldTypes["set"] = [typeof(int)];

        Assert.AreEqual("first", first.SourceBinding.Identifier);
        Assert.AreEqual(string.Empty, second.SourceBinding.Identifier);
        Assert.HasCount(1, first.ResultShape.GeneratedAliases);
        Assert.IsEmpty(second.ResultShape.GeneratedAliases);
        Assert.AreEqual(1, first.MethodResolution.Methods.Count);
        Assert.AreEqual(0, second.MethodResolution.Methods.Count);
        Assert.HasCount(1, first.Diagnostics.NullSuspiciousTypes);
        Assert.IsEmpty(second.Diagnostics.NullSuspiciousTypes);
        Assert.IsTrue(first.Query.SetOperatorFieldTypes.ContainsKey("set"));
        Assert.IsFalse(second.Query.SetOperatorFieldTypes.ContainsKey("set"));
    }
}
