using System.Text.Json;
using Musoq.Evaluator.KaitaiStruct.Tests.Infrastructure;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.KaitaiStruct.Tests.Tests;

[TestClass]
public sealed class ParsingFailureComparerTests
{
    [TestMethod]
    public void Wrong_diagnostic_does_not_satisfy_a_failure_case()
    {
        using var contract = JsonDocument.Parse("{\"code\":\"ValidationFailed\",\"field\":\"Value\"}");
        Assert.Throws<AssertFailedException>(() => ParsingFailureAssertion.Compare(() =>
            throw ParseException.ForBytes(ParseErrorCode.InsufficientData, "Record", "Value", 0, "Truncated"), contract.RootElement, "wrong-code"));
    }

    [TestMethod]
    public void Unrelated_exception_does_not_satisfy_a_failure_case()
    {
        using var contract = JsonDocument.Parse("{\"code\":\"ValidationFailed\",\"field\":\"Value\"}");
        Assert.Throws<AssertFailedException>(() => ParsingFailureAssertion.Compare(() =>
            throw new InvalidOperationException("Unexpected runtime failure"), contract.RootElement, "wrong-type"));
    }
}
