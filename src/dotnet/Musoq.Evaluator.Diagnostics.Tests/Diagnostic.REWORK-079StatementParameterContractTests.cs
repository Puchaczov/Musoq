using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.EnvironmentVariable;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticRework079StatementParameterContractTests : EnvironmentVariablesTestBase
{
    [TestMethod]
    [DataRow(
        "param(author: string); params(limit: int); select 1 from #EnvironmentVariables.All()",
        "params(limit: int)",
        DiagnosticCode.MQ3056_DuplicateScriptParameterBlock,
        "Only one parameter block is allowed")]
    [DataRow(
        "select 1 from #EnvironmentVariables.All(); param(author: string)",
        "param(author: string)",
        DiagnosticCode.MQ3057_ScriptParameterBlockAfterStatement,
        "must appear before all query statements")]
    [DataRow(
        "let value: int = 1; params(author: string); select $value from #EnvironmentVariables.All()",
        "params(author: string)",
        DiagnosticCode.MQ3057_ScriptParameterBlockAfterStatement,
        "must appear before all query statements")]
    [DataRow(
        "param(author: string, author: int); select 1 from #EnvironmentVariables.All()",
        "author: int",
        DiagnosticCode.MQ3058_DuplicateScriptParameterName,
        "declared more than once")]
    public void ParameterPlacementAndUniqueness_ShouldExposeExactBindEnvelopes(
        string query,
        string diagnosticText,
        DiagnosticCode expectedCode,
        string expectedMessage)
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            query,
            Guid.NewGuid().ToString(),
            new EnvironmentVariablesSchemaProvider(),
            LoggerResolver);
        var envelopes = result.ToEnvelopes().ToArray();

        Assert.HasCount(1, envelopes, string.Join(Environment.NewLine, envelopes.Select(static envelope => envelope.Message)));
        var envelope = envelopes.Single();
        Assert.AreEqual(expectedCode, envelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, envelope.Severity);
        Assert.AreEqual(DiagnosticPhase.Bind, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(query.IndexOf(diagnosticText, StringComparison.Ordinal), envelope.Offset);
        Assert.AreEqual(diagnosticText.Length, envelope.Length);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Snippet));
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Explanation));
        Assert.IsNotEmpty(envelope.SuggestedFixes);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.DocsReference));
        StringAssert.Contains(envelope.Message, expectedMessage);
    }
}
