using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Parser.Tokens;
using Musoq.Schema.Interpreters;
using BinaryParseException = Musoq.Schema.Interpreters.ParseException;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticBinary043RepetitionDiscardTests : BinaryInterpretationTestBase
{
    [TestMethod]
    public void BinaryRepeatUntil_LastElementReference_ShouldStopAfterTerminator()
    {
        var registry = new SchemaRegistry();
        registry.Register("Record", new BinarySchemaNode("Record",
        [
            new FieldDefinitionNode("Type", ByteType()),
            new FieldDefinitionNode("Value", ByteType())
        ]));

        var lastRecord = new ArrayIndexNode(
            new IdentifierNode("Records"),
            new HyphenNode(new IntegerNode(0, new TextSpan(0, 0)), new IntegerNode(1, new TextSpan(0, 0))));
        var condition = new EqualityNode(
            new DotNode(lastRecord, new IdentifierNode("Type"), "Type"),
            new IntegerNode(0, new TextSpan(0, 0)));
        var repeat = new RepeatUntilTypeNode(new SchemaReferenceTypeNode("Record"), condition, "Records");
        registry.Register("Stream", new BinarySchemaNode("Stream", [new FieldDefinitionNode("Records", repeat)]));

        var interpreter = CompileInterpreter(registry, "Stream");
        var result = InvokeInterpret(interpreter, [1, 10, 2, 20, 0, 99, 7]);
        var records = (Array)GetPropertyValue<Array>(result, "Records");

        Assert.HasCount(3, records);
        Assert.AreEqual((byte)1, GetPropertyValue<byte>(records.GetValue(0)!, "Type"));
        Assert.AreEqual((byte)20, GetPropertyValue<byte>(records.GetValue(1)!, "Value"));
        Assert.AreEqual((byte)99, GetPropertyValue<byte>(records.GetValue(2)!, "Value"));
    }

    [TestMethod]
    public void BinaryRepeatUntil_ConditionIsDoWhile_ShouldAttemptAnElementOnEmptyInput()
    {
        var repeat = new RepeatUntilTypeNode(ByteType(), new BooleanNode(true), "Values");
        var interpreter = CreateAndCompileInterpreter("DoWhile", new FieldDefinitionNode("Values", repeat));

        var wrapper = Assert.ThrowsExactly<TargetInvocationException>(
            () => InvokeInterpret(interpreter, []));
        var exception = AssertParseException(wrapper);

        Assert.AreEqual(ParseErrorCode.InsufficientData, exception.ErrorCode);
        Assert.AreEqual("DoWhile", exception.SchemaName);
        Assert.AreEqual(0, exception.Position);
        Assert.AreEqual("ISE0001", exception.FormattedErrorCode);
    }

    [TestMethod]
    public void BinaryRepeatUntilEof_ByteArrayElements_ShouldBeZeroOrMoreAndConsumeBoundedElements()
    {
        var repeat = RepeatUntilTypeNode.EndOfInput(
            new ByteArrayTypeNode(new IntegerNode(2, new TextSpan(0, 0))),
            "Chunks");
        var interpreter = CreateAndCompileInterpreter("ChunkStream", new FieldDefinitionNode("Chunks", repeat));

        var empty = InvokeInterpret(interpreter, []);
        Assert.IsEmpty(GetPropertyValue<byte[][]>(empty, "Chunks"));

        var result = InvokeInterpret(interpreter, [1, 2, 3, 4]);
        var chunks = GetPropertyValue<byte[][]>(result, "Chunks");
        Assert.HasCount(2, chunks);
        CollectionAssert.AreEqual(new byte[] { 1, 2 }, chunks[0]);
        CollectionAssert.AreEqual(new byte[] { 3, 4 }, chunks[1]);
    }

    [TestMethod]
    public void BinaryRepeatUntil_ConditionLimit_ShouldRaiseFieldSpecificIse0009()
    {
        var repeat = new RepeatUntilTypeNode(ByteType(), new BooleanNode(false), "Values");
        var interpreter = CreateAndCompileInterpreter("ConditionLimit", new FieldDefinitionNode("Values", repeat));

        var wrapper = Assert.ThrowsExactly<TargetInvocationException>(
            () => InvokeInterpret(interpreter, new byte[10_001]));
        var exception = AssertParseException(wrapper);

        Assert.AreEqual(ParseErrorCode.MaxIterationsExceeded, exception.ErrorCode);
        Assert.AreEqual("ConditionLimit", exception.SchemaName);
        Assert.AreEqual("Values", exception.FieldName);
        Assert.AreEqual(10_000, exception.Position);
        Assert.AreEqual("ISE0009", exception.FormattedErrorCode);
        StringAssert.Contains(exception.Details, "maximum of 10000 iterations");
    }

    [TestMethod]
    public void BinaryRepeatUntilEof_IterationLimit_ShouldRaiseFieldSpecificIse0009()
    {
        var repeat = RepeatUntilTypeNode.EndOfInput(ByteType(), "Values");
        var interpreter = CreateAndCompileInterpreter("EofLimit", new FieldDefinitionNode("Values", repeat));

        var wrapper = Assert.ThrowsExactly<TargetInvocationException>(
            () => InvokeInterpret(interpreter, new byte[10_001]));
        var exception = AssertParseException(wrapper);

        Assert.AreEqual(ParseErrorCode.MaxIterationsExceeded, exception.ErrorCode);
        Assert.AreEqual("EofLimit", exception.SchemaName);
        Assert.AreEqual("Values", exception.FieldName);
        Assert.AreEqual(10_000, exception.Position);
        Assert.AreEqual("ISE0009", exception.FormattedErrorCode);
    }

    [TestMethod]
    public void BinaryRepeatUntilEof_ZeroLengthElement_ShouldRaiseProgressIse0009()
    {
        var repeat = RepeatUntilTypeNode.EndOfInput(
            new ByteArrayTypeNode(new IntegerNode(0, new TextSpan(0, 0))),
            "Chunks");
        var interpreter = CreateAndCompileInterpreter("ZeroProgress", new FieldDefinitionNode("Chunks", repeat));

        var wrapper = Assert.ThrowsExactly<TargetInvocationException>(
            () => InvokeInterpret(interpreter, [0xAA]));
        var exception = AssertParseException(wrapper);

        Assert.AreEqual(ParseErrorCode.MaxIterationsExceeded, exception.ErrorCode);
        Assert.AreEqual("ZeroProgress", exception.SchemaName);
        Assert.AreEqual("Chunks", exception.FieldName);
        Assert.AreEqual(0, exception.Position);
        Assert.AreEqual("ISE0009", exception.FormattedErrorCode);
        StringAssert.Contains(exception.Details, "made no progress");
    }

    [TestMethod]
    public void BinaryDiscard_ConditionalRepeat_ShouldAdvanceOnlyWhenConditionIsTrueAndRemainUnexposed()
    {
        var lastDiscard = new ArrayIndexNode(
            new IdentifierNode("_"),
            new HyphenNode(new IntegerNode(0, new TextSpan(0, 0)), new IntegerNode(1, new TextSpan(0, 0))));
        var repeatCondition = new EqualityNode(lastDiscard, new IntegerNode(0, new TextSpan(0, 0)));
        var repeat = new RepeatUntilTypeNode(ByteType(), repeatCondition, "_");
        var when = new EqualityNode(new IdentifierNode("Flag"), new IntegerNode(1, new TextSpan(0, 0)));
        var interpreter = CreateAndCompileInterpreter(
            "ConditionalDiscard",
            new FieldDefinitionNode("Flag", ByteType()),
            new FieldDefinitionNode("_", repeat, null, null, when),
            new FieldDefinitionNode("Tail", ByteType()));

        var parsed = InvokeInterpret(interpreter, [1, 0xAA, 0x00, 0xBB]);
        Assert.AreEqual((byte)0xBB, GetPropertyValue<byte>(parsed, "Tail"));
        Assert.IsNull(parsed.GetType().GetProperty("_"));

        var skipped = InvokeInterpret(interpreter, [0, 0xCC]);
        Assert.AreEqual((byte)0xCC, GetPropertyValue<byte>(skipped, "Tail"));
    }

    private static PrimitiveTypeNode ByteType()
    {
        return new PrimitiveTypeNode(PrimitiveTypeName.Byte, Endianness.NotApplicable);
    }

    private static BinaryParseException AssertParseException(TargetInvocationException wrapper)
    {
        Assert.IsNotNull(wrapper.InnerException);
        Assert.IsInstanceOfType<BinaryParseException>(wrapper.InnerException);
        return (BinaryParseException)wrapper.InnerException;
    }
}
