using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Interpreters;

namespace Musoq.Schema.Tests;

public partial class BytesInterpreterBaseTests
{
    #region AlignToBits Tests

    [TestMethod]
    public void AlignToBits_ByteAlign_ResetsOffset()
    {
        var interpreter = new TestBytesInterpreter();
        interpreter.SetBitOffset(5);
        interpreter.TestAlignToBits(new byte[10], 8);
        Assert.AreEqual(0, interpreter.GetBitOffset());
        Assert.AreEqual(1, interpreter.GetPosition());
    }

    [TestMethod]
    public void AlignToBits_AlreadyAligned_NoChange()
    {
        var interpreter = new TestBytesInterpreter();
        interpreter.SetBitOffset(0);
        interpreter.TestAlignToBits(new byte[10], 8);
        Assert.AreEqual(0, interpreter.GetPosition());
    }

    [TestMethod]
    public void AlignToBits_16BitAlign_AlignsPosition()
    {
        var interpreter = new TestBytesInterpreter();
        interpreter.SetPosition(1);
        interpreter.TestAlignToBits(new byte[10], 16);
        Assert.AreEqual(2, interpreter.GetPosition());
    }

    [TestMethod]
    public void AlignToBits_32BitAlign_AlignsPosition()
    {
        var interpreter = new TestBytesInterpreter();
        interpreter.SetPosition(1);
        interpreter.TestAlignToBits(new byte[10], 32);
        Assert.AreEqual(4, interpreter.GetPosition());
    }

    [TestMethod]
    public void AlignToBits_ZeroBits_ThrowsInvalidSizeParseException()
    {
        var interpreter = new TestBytesInterpreter();
        var exception = Assert.Throws<ParseException>(() =>
            interpreter.TestAlignToBits(new byte[10], 0));
        Assert.AreEqual(ParseErrorCode.InvalidSize, exception.ErrorCode);
    }

    [TestMethod]
    public void AlignToBits_NegativeBits_ThrowsInvalidSizeParseException()
    {
        var interpreter = new TestBytesInterpreter();
        var exception = Assert.Throws<ParseException>(() =>
            interpreter.TestAlignToBits(new byte[10], -1));
        Assert.AreEqual(ParseErrorCode.InvalidSize, exception.ErrorCode);
    }

    [TestMethod]
    public void AlignToBits_ArbitraryPositiveBoundary_AlignsAbsoluteBitPosition()
    {
        var interpreter = new TestBytesInterpreter();
        interpreter.SetPosition(1);
        interpreter.TestAlignToBits(new byte[10], 65);
        Assert.AreEqual(8, interpreter.GetPosition());
        Assert.AreEqual(1, interpreter.GetBitOffset());
    }

    #endregion
    #region Validate Tests

    [TestMethod]
    public void Validate_ConditionTrue_NoException()
    {
        var interpreter = new TestBytesInterpreter();
        interpreter.TestValidate(true, "field", "should not throw");
    }

    [TestMethod]
    public void Validate_ConditionFalse_ThrowsParseException()
    {
        var interpreter = new TestBytesInterpreter();
        var ex = Assert.Throws<ParseException>(() =>
            interpreter.TestValidate(false, "testField", "validation failed"));
        Assert.AreEqual("testField", ex.FieldName);
        Assert.Contains("validation failed", ex.Message);
    }

    #endregion

    #region Interface Tests

    [TestMethod]
    public void Interpret_FromSpan_Works()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.Interpret(new byte[] { 0x42 });
        Assert.AreEqual(0x42, result.Value);
    }

    [TestMethod]
    public void Interpret_FromArray_Works()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.Interpret(new byte[] { 0x42 });
        Assert.AreEqual(0x42, result.Value);
    }

    [TestMethod]
    public void TryInterpret_Success_ReturnsTrue()
    {
        var interpreter = new TestBytesInterpreter();
        var success = interpreter.TryInterpret([0x42], out var result);
        Assert.IsTrue(success);
        Assert.IsNotNull(result);
        Assert.AreEqual(0x42, result.Value);
    }

    [TestMethod]
    public void TryInterpret_Empty_ReturnsTrue()
    {
        var interpreter = new TestBytesInterpreter();
        var success = interpreter.TryInterpret([], out var result);
        Assert.IsTrue(success);
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Value);
    }

    [TestMethod]
    public void BytesConsumed_AfterInterpret_ReturnsCorrectValue()
    {
        var interpreter = new TestBytesInterpreter();
        interpreter.Interpret(new byte[] { 0x42 });
        Assert.AreEqual(1, interpreter.BytesConsumed);
    }

    [TestMethod]
    public void PartialInterpret_Success_ReturnsResult()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.PartialInterpret(new byte[] { 0x42 });
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Result);
        Assert.AreEqual(0x42, result.Result!.Value);
        Assert.IsNull(result.ErrorMessage);
        Assert.AreEqual("Test", result.ParsedFields["Name"]);
    }

    [TestMethod]
    public void PartialInterpret_FromArray_ReturnsResult()
    {
        var interpreter = new TestBytesInterpreter();
        var result = interpreter.PartialInterpret(new byte[] { 0x42 });
        Assert.IsTrue(result.IsSuccess);
    }

    #endregion

    #region EnsureBytes Tests

    [TestMethod]
    public void EnsureBytes_Enough_NoException()
    {
        var interpreter = new TestBytesInterpreter();
        interpreter.TestEnsureBytes([0x01, 0x02, 0x03], 2);
    }

    [TestMethod]
    public void EnsureBytes_NotEnough_ThrowsParseException()
    {
        var interpreter = new TestBytesInterpreter();
        Assert.Throws<ParseException>(() =>
            interpreter.TestEnsureBytes([0x01], 10));
    }

    #endregion

    #region SeekTo Tests

    [TestMethod]
    public void SeekTo_ValidPosition_SetsPosition()
    {
        var interpreter = new TestBytesInterpreter();
        interpreter.TestSeekTo(5);
        Assert.AreEqual(5, interpreter.GetPosition());
    }

    [TestMethod]
    public void SeekTo_ResetsBitOffset()
    {
        var interpreter = new TestBytesInterpreter();
        interpreter.SetBitOffset(4);
        interpreter.TestSeekTo(5);
        Assert.AreEqual(0, interpreter.GetBitOffset());
    }

    #endregion
}
