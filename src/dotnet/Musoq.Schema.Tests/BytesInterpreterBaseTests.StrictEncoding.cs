using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Interpreters;

namespace Musoq.Schema.Tests;

public partial class BytesInterpreterBaseTests
{
    [TestMethod]
    public void InvalidUtf8PreservesByteOffsetAndCursor()
    {
        var reader = new TestBytesInterpreter();
        reader.SetPosition(1);
        var failure = Assert.Throws<ParseException>(() => reader.TestReadString([0, 65, 0xff], 2, Encoding.UTF8));

        Assert.AreEqual(ParseErrorCode.EncodingError, failure.ErrorCode);
        Assert.AreEqual(2, failure.Position);
        Assert.AreEqual(1, reader.GetPosition());
    }

    [TestMethod]
    public void CustomUtf8EncodingRetainsItsCloneBehaviorAndFallback()
    {
        var encoding = new CountingUtf8();
        var fallback = encoding.DecoderFallback;
        var reader = new TestBytesInterpreter();

        Assert.AreEqual("a", reader.TestReadString("ab"u8, 1, encoding));
        Assert.AreEqual("b", reader.TestReadString("ab"u8, 1, encoding));
        Assert.AreEqual(2, encoding.Clones);
        Assert.AreSame(fallback, encoding.DecoderFallback);
    }

    private sealed class CountingUtf8 : UTF8Encoding
    {
        internal int Clones { get; private set; }

        public override object Clone()
        {
            Clones++;
            return base.Clone();
        }
    }
}
