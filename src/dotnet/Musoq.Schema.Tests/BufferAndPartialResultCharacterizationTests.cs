using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;
using Musoq.Schema.Interpreters;

namespace Musoq.Schema.Tests;

[TestClass]
public sealed class BufferAndPartialResultCharacterizationTests
{
    [TestMethod]
    public void LosslessConversion_ResultEscapingInterpretation_ShouldOwnTheEntireRequestedRange()
    {
        byte[] input = [10, 20, 30, 40];
        var library = new LibraryBase();
        var snapshot = library.FromBase64(library.ToBase64(input, 1, 3));

        input[2] = 99;

        CollectionAssert.AreEqual(new byte[] { 20, 30, 40 }, snapshot!);
    }

    [TestMethod]
    public void LegacyInterpreter_ParsedArray_ShouldRemainIndependentOfInputMutation()
    {
        byte[] input = [1, 2, 3];
        var parsed = new LegacyPacketInterpreter().Interpret(input);

        input[1] = 99;

        CollectionAssert.AreEqual(new byte[] { 2, 3 }, parsed.Tail);
    }

    [TestMethod]
    public void PartialResult_CompletedFieldsAndPayload_ShouldSurviveLaterInterpreterUse()
    {
        var interpreter = new LegacyPacketInterpreter();
        var retained = interpreter.PartialInterpret([7]);

        _ = interpreter.PartialInterpret([8, 9, 10]);

        Assert.AreEqual((byte)7, retained.ParsedFields!.Value);
        CollectionAssert.AreEquivalent(new[] { "Value" }, new List<string>(retained.CompletedFields));
    }

    [TestMethod]
    public void PartialFailure_ShouldRetainItsOriginalCursorAndDiagnostic()
    {
        var result = new LegacyPacketInterpreter().PartialInterpret([7]);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(1, result.BytesConsumed);
        Assert.AreEqual(0, result.BitOffset);
        Assert.AreEqual("Tail", result.ErrorField);
        Assert.Contains("ISE0001", result.ErrorMessage!);
    }

    [TestMethod]
    public void PublicPartialResultConstructor_ShouldPreserveCallerSuppliedCompletionMetadata()
    {
        IReadOnlySet<string> completed = new HashSet<string>(StringComparer.Ordinal) { "Value" };
        var result = new PartialInterpretResult<int>(3, completed, 1);

        Assert.AreSame(completed, result.CompletedFields);
        Assert.AreEqual(3, result.Result);
    }

    [TestMethod]
    public void LegacyTextInterpreter_TolerantAndStrictCalls_ShouldRetainTheirFailureContract()
    {
        var interpreter = new LegacyLiteralInterpreter();

        var success = interpreter.TryParse("NO".AsSpan(), out var value);
        var failure = Assert.ThrowsExactly<ParseException>(() => interpreter.Parse("NO"));

        Assert.IsFalse(success);
        Assert.IsNull(value);
        Assert.AreEqual(ParseErrorCode.LiteralMismatch, failure.ErrorCode);
        Assert.AreEqual("Prefix", failure.FieldName);
        Assert.AreEqual(0, failure.Position);
    }

    private sealed class DecodedPacket
    {
        public byte Value { get; set; }
        public byte[] Tail { get; set; } = [];
    }

    private sealed class LegacyPacketInterpreter : BytesInterpreterBase<DecodedPacket>
    {
        public override string SchemaName => "LegacyPacket";

        public override DecodedPacket InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            var parsed = new DecodedPacket();
            RecordParsedValue(parsed);
            SetCurrentField("Value");
            parsed.Value = ReadByte(data);
            RecordParsedField("Value");
            SetCurrentField("Tail");
            parsed.Tail = ReadBytes(data, 2);
            RecordParsedField("Tail");
            return parsed;
        }
    }

    private sealed class LegacyLiteralInterpreter : TextInterpreterBase<string>
    {
        public override string SchemaName => "LegacyLiteral";

        public override string ParseAt(ReadOnlySpan<char> text, int offset)
        {
            ParsePosition = offset;
            return ExpectLiteral(text, "OK", fieldName: "Prefix");
        }
    }
}
