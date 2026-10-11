using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Diagnostics;

namespace Musoq.Schema.Tests;

[TestClass]
public sealed class DataSourceInputDiagnosticContractTests
{
    [TestMethod]
    public void Location_PreservesLineColumnAndZeroBasedOffsets()
    {
        var location = new DataSourceInputLocation(
            line: 3,
            column: 7,
            byteOffset: 23,
            characterOffset: 19,
            bytePositionInLine: 5);

        Assert.AreEqual(3L, location.Line);
        Assert.AreEqual(7L, location.Column);
        Assert.AreEqual(23L, location.ByteOffset);
        Assert.AreEqual(19L, location.CharacterOffset);
        Assert.AreEqual(5L, location.BytePositionInLine);
    }

    [TestMethod]
    public void Location_RejectsInvalidOneBasedAndZeroBasedValues()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new DataSourceInputLocation(line: 0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new DataSourceInputLocation(column: -1));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new DataSourceInputLocation(byteOffset: -1));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new DataSourceInputLocation(characterOffset: -1));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new DataSourceInputLocation(bytePositionInLine: -1));
    }

    [TestMethod]
    public void InputException_UsesFixedSafeMessagesAndPreservesInnerException()
    {
        var inner = new InvalidDataException("private path and record payload");
        var location = new DataSourceInputLocation(byteOffset: 23);
        var exception = new DataSourceInputException(
            DataSourceInputFailureReason.UnterminatedQuotedField,
            location,
            inner);

        Assert.AreEqual("The final quoted field is not terminated.", exception.Message);
        Assert.AreEqual(DataSourceInputFailureReason.UnterminatedQuotedField, exception.Reason);
        Assert.AreSame(location, exception.Location);
        Assert.AreSame(inner, exception.InnerException);
        Assert.DoesNotContain("private path", exception.Message);
        Assert.DoesNotContain("record payload", exception.Message);
    }

    [TestMethod]
    public void InputException_RejectsUnknownReason()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new DataSourceInputException((DataSourceInputFailureReason)int.MaxValue));
    }
}
