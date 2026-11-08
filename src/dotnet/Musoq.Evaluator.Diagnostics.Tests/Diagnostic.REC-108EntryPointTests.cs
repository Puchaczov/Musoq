using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticREC108EntryPointTests : Schema.NegativeTests.NegativeTestsBase
{
    [TestMethod]
    public void SingleQueryCompilation_ShouldRejectTwoExecutableStatementsWithMQ2036()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("select 1 from #test.single(); select 2 from #test.single()"));

        AssertErrorEnvelope(exception, DiagnosticCode.MQ2036_MultipleExecutableStatements, DiagnosticPhase.Parse);
        AssertHasGuidance(exception);
        StringAssert.Contains(exception.PrimaryEnvelope.Message, "Multiple executable statements");
    }

    [TestMethod]
    public void SingleQueryCompilation_ShouldKeepAdjacentPastedStatementAsParseError()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("select 1 from #test.single() select 2 from #test.single()"));

        AssertErrorEnvelope(exception, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse);
        AssertHasGuidance(exception);
    }

    [TestMethod]
    public void SingleQueryCompilation_ShouldAllowOneExecutableStatementAfterDeclaration()
    {
        var compiled = CompileQuery("enum State : int { Ready = 1 }; select 1 from #test.single()");
        var table = compiled.Run(TokenSource.Token);

        Assert.HasCount(1, table);
    }
}
