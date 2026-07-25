using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class NamedArgumentAstTests
{
    [TestMethod]
    public void ArgsListNode_NamedArguments_PreservesLabelsInIdentityAndText()
    {
        var value = new IntegerNode(4, new TextSpan(19, 1));
        var names = new ArgumentName?[]
        {
            new ArgumentName("someArgument", new TextSpan(5, 12)),
            null
        };

        var node = new ArgsListNode([value, new IntegerNode(2)], names, default);

        Assert.IsTrue(node.HasNamedArguments);
        Assert.AreEqual("someArgument: 4, 2", node.ToString());
        Assert.AreEqual("(someArgument: 4, 2)", node.ToStringWithBrackets());
        Assert.AreEqual(new TextSpan(5, 15), node.Span);
        StringAssert.Contains(node.Id, "someArgument:");
        Assert.AreEqual("someArgument", node.ArgumentNames[0]!.Value.Name);
        Assert.IsNull(node.ArgumentNames[1]);
    }

    [TestMethod]
    public void ArgsListNode_LegacyConstructor_RemainsEntirelyPositional()
    {
        var node = new ArgsListNode([new IntegerNode(1), new IntegerNode(2)]);

        Assert.IsFalse(node.HasNamedArguments);
        Assert.IsTrue(node.ArgumentNames.All(static name => name is null));
        Assert.AreEqual("1, 2", node.ToString());
    }

    [TestMethod]
    public void ArgsListNode_MismatchedNameMetadata_IsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new ArgsListNode([new IntegerNode(1)], [new ArgumentName("value", default), null], default));
    }
}
