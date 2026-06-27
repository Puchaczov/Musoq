using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class SchemaArgumentBinderTests
{
    [TestMethod]
    public void BindStaticArguments_ShouldReturnLiteralValuesInOrder()
    {
        var args = new ArgsListNode(
        [
            new IntegerNode("1", string.Empty),
            new DecimalNode("2.5"),
            new BooleanNode(true),
            new BooleanNode(false),
            new StringNode("text"),
            new WordNode("word"),
            new HexIntegerNode("0x10"),
            new BinaryIntegerNode("0b10"),
            new OctalIntegerNode("0o10")
        ]);

        var values = SchemaArgumentBinder.BindStaticArguments(args);

        CollectionAssert.AreEqual(
            new object[] { 1, 2.5m, true, false, "text", "word", 16L, 2L, 8L },
            values);
    }

    [TestMethod]
    public void BindStaticArguments_ShouldSkipDynamicArguments()
    {
        var args = new ArgsListNode(
        [
            new IdentifierNode("rowValue", typeof(string)),
            new StringNode("static")
        ]);

        var values = SchemaArgumentBinder.BindStaticArguments(args);

        CollectionAssert.AreEqual(new object[] { "static" }, values);
    }
}
