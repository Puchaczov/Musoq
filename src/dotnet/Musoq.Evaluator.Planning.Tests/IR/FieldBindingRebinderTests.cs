using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class FieldBindingRebinderTests
{
    [TestMethod]
    public void Rebind_ShouldReplaceOnlyCarrierAccessStrategy()
    {
        var field = new FieldBinding(
            "Value",
            "source.Value",
            1,
            typeof(string),
            FieldNullability.Nullable,
            new PositionalAccess(1),
            readModifiers: new Dictionary<string, string> { ["encoding"] = "utf-8" }) with
        {
            GeneratedTypeName = "SourceRow",
            GeneratedMemberTypeNames = new Dictionary<string, string> { ["Value"] = "string" }
        };

        var rebound = FieldBindingRebinder.Rebind(field, new GeneratedFieldAccess("Value"));

        Assert.IsInstanceOfType(rebound.AccessStrategy, typeof(GeneratedFieldAccess));
        Assert.AreEqual(field.Name, rebound.Name);
        Assert.AreEqual(field.QualifiedName, rebound.QualifiedName);
        Assert.AreEqual(field.OutputIndex, rebound.OutputIndex);
        Assert.AreEqual(field.Type, rebound.Type);
        Assert.AreEqual(field.Nullability, rebound.Nullability);
        Assert.AreEqual(field.PublicType, rebound.PublicType);
        Assert.AreEqual(field.GeneratedTypeName, rebound.GeneratedTypeName);
        Assert.AreSame(field.GeneratedMemberTypeNames, rebound.GeneratedMemberTypeNames);
        Assert.AreSame(field.ReadModifiers, rebound.ReadModifiers);
    }
}
