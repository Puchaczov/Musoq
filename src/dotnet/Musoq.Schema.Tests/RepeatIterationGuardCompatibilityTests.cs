using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Interpreters;

namespace Musoq.Schema.Tests;

[TestClass]
public sealed class RepeatIterationGuardCompatibilityTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void LegacyTwoArgumentGuard_ShouldKeepItsHistoricalBoundary(bool text)
    {
        Action<int> guard = text ? new TextProbe().Legacy : new BytesProbe().Legacy;
        guard(9_999);
        var failure = Assert.Throws<ParseException>(() => guard(10_000));

        Assert.AreEqual(ParseErrorCode.MaxIterationsExceeded, failure.ErrorCode);
        Assert.AreEqual("Records", failure.FieldName);
        Assert.AreEqual(0, failure.Position);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void ConfiguredGuard_ShouldUseItsSuppliedBoundary(bool text)
    {
        Action<int, int> guard = text ? new TextProbe().Configured : new BytesProbe().Configured;
        guard(1, 2);
        var failure = Assert.Throws<ParseException>(() => guard(2, 2));

        Assert.AreEqual(ParseErrorCode.MaxIterationsExceeded, failure.ErrorCode);
        StringAssert.Contains(failure.Details, "maximum of 2 iterations");
    }

    private sealed class BytesProbe : BytesInterpreterBase<int>
    {
        public override string SchemaName => "BytesProbe";
        public override int InterpretAt(ReadOnlySpan<byte> data, int offset) => 0;
        public void Legacy(int iteration) => EnsureRepeatIteration("Records", iteration);
        public void Configured(int iteration, int maximum) => EnsureRepeatIteration("Records", iteration, maximum);
    }

    private sealed class TextProbe : TextInterpreterBase<string>
    {
        public override string SchemaName => "TextProbe";
        public override string ParseAt(ReadOnlySpan<char> text, int position) => string.Empty;
        public void Legacy(int iteration) => EnsureRepeatIteration("Records", iteration);
        public void Configured(int iteration, int maximum) => EnsureRepeatIteration("Records", iteration, maximum);
    }
}
