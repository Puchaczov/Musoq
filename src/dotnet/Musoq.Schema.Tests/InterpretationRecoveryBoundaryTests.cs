using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Interpreters;

namespace Musoq.Schema.Tests;

[TestClass]
public sealed class InterpretationRecoveryBoundaryTests
{
    [TestMethod]
    [DataRow("cancel")]
    [DataRow("provider")]
    [DataRow("arithmetic")]
    public void PartialResults_ShouldPropagateUnrelatedFaults(string failure)
    {
        var exception = CreateFailure(failure);
        var binary = new FailingBinary(exception);
        var text = new FailingText(exception);
        Assert.AreSame(exception, Assert.Throws<Exception>(() => binary.PartialInterpret(Array.Empty<byte>())));
        Assert.AreSame(exception, Assert.Throws<Exception>(() => text.PartialParse(string.Empty)));
    }

    [TestMethod]
    public void PartialResults_ShouldRecoverInterpretationFailures()
    {
        var exception = new ParseException("Invalid input");
        Assert.IsFalse(new FailingBinary(exception).PartialInterpret(Array.Empty<byte>()).IsSuccess);
        Assert.IsFalse(new FailingText(exception).PartialParse(string.Empty).IsSuccess);
    }

    [TestMethod]
    [DataRow("cancel")]
    [DataRow("provider")]
    [DataRow("arithmetic")]
    public void TryMethods_ShouldPropagateUnrelatedFaults(string failure)
    {
        var exception = CreateFailure(failure);
        Assert.AreSame(
            exception,
            Assert.Throws<Exception>(() => new FailingBinary(exception).TryInterpret(Array.Empty<byte>(), out _))
        );
        Assert.AreSame(
            exception,
            Assert.Throws<Exception>(() => new FailingText(exception).TryParse(string.Empty, out _))
        );
        Assert.AreSame(
            exception,
            Assert.Throws<Exception>(() => InterpretationStatus.TryParse(new FailingText(exception), string.Empty))
        );
        Assert.AreSame(
            exception,
            Assert.Throws<Exception>(() =>
                InterpretationStatus.TryParse(new LegacyFailingText(exception), string.Empty)
            )
        );
    }

    [TestMethod]
    public void TextStatusTryParse_ShouldRecoverLegacyParseException()
    {
        var interpreter = new LegacyFailingText(new ParseException("Invalid input"));

        Assert.IsNull(InterpretationStatus.TryParse(interpreter, string.Empty));
    }

    private static Exception CreateFailure(string failure) =>
        failure switch
        {
            "cancel" => new OperationCanceledException(),
            "provider" => new InvalidOperationException("Provider failed"),
            _ => new DivideByZeroException(),
        };

    private sealed class FailingBinary(Exception failure) : BytesInterpreterBase<object>
    {
        public override string SchemaName => "Failure";

        public override object InterpretAt(ReadOnlySpan<byte> data, int offset) => throw failure;
    }

    private sealed class FailingText(Exception failure) : TextInterpreterBase<object>
    {
        public override string SchemaName => "Failure";

        public override object ParseAt(ReadOnlySpan<char> data, int position) => throw failure;
    }

    private sealed class LegacyFailingText(Exception failure) : ITextInterpreter<object>
    {
        public string SchemaName => "Failure";

        public int CharsConsumed => 0;

        public object Parse(ReadOnlySpan<char> text) => ParseAt(text, 0);

        public object ParseAt(ReadOnlySpan<char> text, int position) => throw failure;
    }
}
