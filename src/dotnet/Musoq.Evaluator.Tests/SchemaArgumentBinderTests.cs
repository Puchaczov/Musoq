using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;
using Musoq.Schema.Reflection;
using SchemaConstructorInfo = Musoq.Schema.Reflection.ConstructorInfo;

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
    public void BindStaticArguments_ShouldStopAtFirstDynamicArgument()
    {
        var args = new ArgsListNode(
        [
            new IdentifierNode("rowValue", typeof(string)),
            new StringNode("static")
        ]);

        var values = SchemaArgumentBinder.BindStaticArguments(args);

        CollectionAssert.AreEqual(Array.Empty<object?>(), values);
    }

    [TestMethod]
    public void BindStaticArguments_ShouldKeepOnlyTheMaterializablePrefix()
    {
        var args = new ArgsListNode(
        [
            new StringNode("first"),
            new IdentifierNode("rowValue", typeof(string)),
            new IntegerNode("3")
        ]);

        var values = SchemaArgumentBinder.BindStaticArguments(args);

        CollectionAssert.AreEqual(new object?[] { "first" }, values);
    }

    [TestMethod]
    public void BoundInvocation_ShouldNotShiftStaticValuesAfterDynamicSlot()
    {
        var args = new ArgsListNode(
        [
            new IdentifierNode("rowValue", typeof(string)),
            new IntegerNode("2")
        ]);
        var constructor = typeof(BoundSourceTable)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Single(candidate => candidate.GetParameters().Length == 2);
        var method = new SchemaMethodInfo(
            "source",
            new SchemaConstructorInfo(
                constructor,
                false,
                ("first", typeof(string)),
                ("second", typeof(int))));
        var signature = SchemaSourceSignature.Create(method);
        var invocation = new BoundSchemaInvocation(
            signature,
            [
                new BoundSchemaArgument(0, 0, null),
                new BoundSchemaArgument(1, 1, null)
            ],
            usesNamedArguments: true);

        var values = SchemaArgumentBinder.BindStaticArguments(args, invocation: invocation);

        CollectionAssert.AreEqual(Array.Empty<object?>(), values);
    }

    private sealed class BoundSourceTable
    {
        public BoundSourceTable(string first, int second)
        {
            _ = (first, second);
        }
    }
}
