using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Interpreters;

namespace Musoq.Schema.Tests;

[TestClass]
public sealed class PartialCursorStateTests
{
    [TestMethod]
    public void LegacyConstructors_ShouldDefaultToByteAlignment()
    {
        IReadOnlySet<string> completed = new HashSet<string>();
        Assert.AreEqual(0, new PartialInterpretResult<int>(1, completed, 3).BitOffset);
        Assert.AreEqual(0, new PartialInterpretResult<int>(0, completed, 3, "Value", "Failure").BitOffset);
    }

    [TestMethod]
    [DataRow(-1)]
    [DataRow(8)]
    public void BitOffset_ShouldRejectValuesOutsideCurrentByte(int offset)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new PartialInterpretResult<int>(1, new HashSet<string>(), 0, offset));
    }

    [TestMethod]
    public void ReusedInterpreter_ShouldResetCursorAndCompletionAfterPartialFailure()
    {
        var interpreter = new BitsProbe();
        var first = interpreter.PartialInterpret([7]);
        Assert.IsFalse(first.IsSuccess);
        Assert.AreEqual(0, first.BytesConsumed);
        Assert.AreEqual(3, first.BitOffset);
        var second = interpreter.PartialInterpret([0]);
        Assert.IsTrue(second.IsSuccess);
        Assert.AreEqual(0, second.BytesConsumed);
        Assert.AreEqual(3, second.BitOffset);
        Assert.AreEqual(0, second.Result);
        Assert.IsTrue(second.CompletedFields.Contains("Value"));
        Assert.HasCount(0, first.CompletedFields);
    }

    private sealed class BitsProbe : BytesInterpreterBase<int>
    {
        public override string SchemaName => "Bits";
        public override int InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            var value = (int)ReadBits(data, 3);
            if (value != 0)
                throw ParseException.ForBytes(ParseErrorCode.ValidationFailed, SchemaName, "Value", ParsePosition, "Invalid bits");
            RecordParsedField("Value");
            return value;
        }
    }
}
