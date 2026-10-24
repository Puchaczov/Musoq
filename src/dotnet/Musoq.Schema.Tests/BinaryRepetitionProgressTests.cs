using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Interpreters;

namespace Musoq.Schema.Tests;

[TestClass]
public sealed class BinaryRepetitionProgressTests
{
    [TestMethod]
    [DataRow(0, 2, 0, 3)]
    [DataRow(0, 7, 1, 0)]
    public void Progress_ShouldAcceptAdvancementInBytesOrBits(int startByte, int startBit, int endByte, int endBit) =>
        new Probe().Validate(startByte, startBit, endByte, endBit);

    [TestMethod]
    [DataRow(1, 0, 0, 7)]
    [DataRow(0, 3, 0, 2)]
    [DataRow(0, 3, 0, 3)]
    public void Progress_ShouldRejectStationaryOrBackwardCursor(int startByte, int startBit, int endByte, int endBit)
    {
        var failure = Assert.ThrowsExactly<ParseException>(() => new Probe().Validate(startByte, startBit, endByte, endBit));
        Assert.AreEqual(ParseErrorCode.MaxIterationsExceeded, failure.ErrorCode);
    }

    private sealed class Probe : BytesInterpreterBase<int>
    {
        public override string SchemaName => "Progress";
        public override int InterpretAt(ReadOnlySpan<byte> data, int offset) => 0;
        internal void Validate(int startByte, int startBit, int endByte, int endBit)
        {
            ParsePosition = endByte;
            BitOffset = endBit;
            EnsureRepeatMadeProgress("Items", startByte, startBit);
        }
    }
}
