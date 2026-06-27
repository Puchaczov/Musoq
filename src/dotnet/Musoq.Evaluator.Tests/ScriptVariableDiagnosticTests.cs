using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.EnvironmentVariable;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class ScriptVariableDiagnosticTests : EnvironmentVariablesTestBase
{
    [TestMethod]
    [DataRow(
        "let key: string = 'KEY_1'; select $kay from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3059_UndeclaredScriptParameter,
        "is not declared",
        DisplayName = "mistyped script variable name")]
    [DataRow(
        "let key: string = 'KEY_1'; let key: string = 'KEY_2'; select $key from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3063_DuplicateScriptSymbolName,
        "declared more than once",
        DisplayName = "duplicate let name")]
    [DataRow(
        "let key: object = 'KEY_1'; select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3064_UnsupportedScriptVariableType,
        "is not supported",
        DisplayName = "unsupported let type")]
    [DataRow(
        "let limit: int = 'many'; select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3065_InvalidScriptVariableInitializer,
        "cannot be converted",
        DisplayName = "initializer value has wrong type")]
    [DataRow(
        "let ratio: int = 10 / 0; select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3065_InvalidScriptVariableInitializer,
        "divide by zero",
        DisplayName = "initializer divides by zero")]
    [DataRow(
        "param(root: string = 'KEY'); let key: string = $root + '_1'; select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3065_InvalidScriptVariableInitializer,
        "runtime parameter",
        DisplayName = "initializer uses runtime parameter")]
    [DataRow(
        "let key: string = $later; let later: string = 'KEY_1'; select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3066_ScriptVariableUsedBeforeDeclaration,
        "before it is declared",
        DisplayName = "initializer references later variable")]
    public void CompileForExecution_WhenScriptVariableDeclarationOrReferenceFails_ShouldUseSpecificEnvelope(
        string query,
        DiagnosticCode expectedCode,
        string messagePart)
    {
        var exception = CompileFails(query);

        AssertSingleError(exception, expectedCode, DiagnosticPhase.Bind, messagePart);
        AssertHasGuidance(exception);
        StringAssert.Contains(exception.FormatText(), "Try:");
        StringAssert.Contains(exception.FormatJson(), $"MQ{(int)expectedCode}");
    }

    [TestMethod]
    public void CompileWithDiagnostics_WhenScriptVariableNameIsMistyped_ShouldReturnActionableEnvelope()
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            "let key: string = 'KEY_1'; select $kay from #EnvironmentVariables.All()",
            Guid.NewGuid().ToString(),
            new EnvironmentVariablesSchemaProvider(),
            LoggerResolver);

        var envelope = result.ToEnvelopes().Single();

        Assert.AreEqual(DiagnosticCode.MQ3059_UndeclaredScriptParameter, envelope.Code);
        Assert.AreEqual(DiagnosticPhase.Bind, envelope.Phase);
        Assert.IsNotNull(envelope.Snippet);
        Assert.IsNotNull(envelope.Explanation);
        Assert.IsGreaterThan(0, envelope.SuggestedFixes.Count);
    }

    private MusoqQueryException CompileFails(string query)
    {
        return Assert.Throws<MusoqQueryException>(() =>
            InstanceCreator.CompileForExecution(
                query,
                Guid.NewGuid().ToString(),
                new EnvironmentVariablesSchemaProvider(),
                LoggerResolver));
    }
}