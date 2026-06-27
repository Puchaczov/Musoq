using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class GenericMethodIdentityTests
{
    [TestMethod]
    public void AccessMethodNode_WhenGeneric_ShouldIncludeTypeParameterInIdentityAndRendering()
    {
        var payload = new IdentifierNode("payload");
        var header = new AccessMethodNode(
            new GenericFunctionToken("Interpret", "Header", TextSpan.Empty),
            new ArgsListNode([payload]),
            null,
            false);
        var packet = new AccessMethodNode(
            new GenericFunctionToken("Interpret", "Packet", TextSpan.Empty),
            new ArgsListNode([payload]),
            null,
            false);

        Assert.AreEqual("Interpret<Header>(payload)", header.ToString());
        Assert.Contains("Interpret<Header>", header.Id);
        Assert.Contains("Interpret<Packet>", packet.Id);
        Assert.AreNotEqual(header.Id, packet.Id);
    }

    [TestMethod]
    public void AccessMethodNode_WhenNonGeneric_ShouldKeepExistingRendering()
    {
        var method = new AccessMethodNode(
            new FunctionToken("Length", TextSpan.Empty),
            ArgsListNode.Empty,
            null,
            false);

        Assert.AreEqual("Length()", method.ToString());
        Assert.IsFalse(method.Id.Contains("<", System.StringComparison.Ordinal));
    }

    [TestMethod]
    public void AliasedFromNode_WhenGeneric_ShouldIncludeTypeParameterInIdentityAndRendering()
    {
        var payload = new IdentifierNode("payload");
        var header = new AliasedFromNode(
            "Interpret",
            new ArgsListNode([payload]),
            "h",
            typeof(object),
            0,
            "Header");
        var packet = new AliasedFromNode(
            "Interpret",
            new ArgsListNode([payload]),
            "h",
            typeof(object),
            0,
            "Packet");

        Assert.AreEqual("Interpret<Header>(payload) as h", header.ToString());
        Assert.AreEqual("Interpret<Header>-h", header.Id);
        Assert.AreEqual("Interpret<Packet>-h", packet.Id);
        Assert.AreNotEqual(header.Id, packet.Id);
    }

    [TestMethod]
    public void AliasedFromNode_WhenNonGeneric_ShouldKeepExistingRendering()
    {
        var source = new AliasedFromNode(
            "Entities",
            ArgsListNode.Empty,
            "e",
            typeof(object),
            0);

        Assert.AreEqual("Entities() as e", source.ToString());
        Assert.AreEqual("Entities-e", source.Id);
    }
}