using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Visitors;
using Musoq.Evaluator.Tests.Schema.EnvironmentVariable;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Tokens;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class ScriptParameterDiagnosticTests : EnvironmentVariablesTestBase
{
    [TestMethod]
    [DataRow(
        "param(author: string); param(limit: int); select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3056_DuplicateScriptParameterBlock,
        "Only one parameter block",
        DisplayName = "duplicate parameter block")]
    [DataRow(
        "select 1 from #EnvironmentVariables.All(); param(author: string)",
        DiagnosticCode.MQ3057_ScriptParameterBlockAfterStatement,
        "must appear before",
        DisplayName = "parameter block after query")]
    [DataRow(
        "param(author: string, author: int); select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3058_DuplicateScriptParameterName,
        "declared more than once",
        DisplayName = "duplicate parameter name")]
    [DataRow(
        "param(author: string); select $missing from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3059_UndeclaredScriptParameter,
        "is not declared",
        DisplayName = "undeclared parameter reference")]
    [DataRow(
        "param(author: object); select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3060_UnsupportedScriptParameterType,
        "is not supported",
        DisplayName = "unsupported parameter type")]
    [DataRow(
        "param(ids: int[]?); select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3060_UnsupportedScriptParameterType,
        "nullable collection",
        DisplayName = "nullable collection parameter")]
    [DataRow(
        "param(ids: object[]); select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3060_UnsupportedScriptParameterType,
        "is not supported",
        DisplayName = "unsupported collection element type")]
    [DataRow(
        "param(limit: int = 'abc'); select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3061_InvalidScriptParameterDefault,
        "cannot be converted",
        DisplayName = "invalid parameter default")]
    [DataRow(
        "param(ids: int[] = null); select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3061_InvalidScriptParameterDefault,
        "cannot declare a default value",
        DisplayName = "collection parameter default")]
    [DataRow(
        "param(name: string = 'KEY_1'); select 1 from #EnvironmentVariables.All($name + '_2')",
        DiagnosticCode.MQ3062_InvalidScriptParameterSourceArgument,
        "must be passed directly",
        DisplayName = "nested parameter as source argument")]
    [DataRow(
        "param(name: string = 'KEY_1'); select 1 from #EnvironmentVariables.All($name::string)",
        DiagnosticCode.MQ3062_InvalidScriptParameterSourceArgument,
        "must be passed directly",
        DisplayName = "cast parameter as source argument")]
    [DataRow(
        "param(name: string = 'KEY_1'); select 1 from #EnvironmentVariables.All($name is null)",
        DiagnosticCode.MQ3062_InvalidScriptParameterSourceArgument,
        "must be passed directly",
        DisplayName = "null-check parameter as source argument")]
    [DataRow(
        "param(name: string = 'KEY_1'); select 1 from #EnvironmentVariables.All($name between 'A' and 'Z')",
        DiagnosticCode.MQ3062_InvalidScriptParameterSourceArgument,
        "must be passed directly",
        DisplayName = "between parameter as source argument")]
    [DataRow(
        "param(name: string = 'KEY_1'); select 1 from #EnvironmentVariables.All(name: $name + '_2') sourceAlias",
        DiagnosticCode.MQ3062_InvalidScriptParameterSourceArgument,
        "must be passed directly",
        DisplayName = "named aliased parameter as source argument")]
    [DataRow(
        "param(name: string); let name: string = 'KEY_1'; select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3063_DuplicateScriptSymbolName,
        "declared more than once",
        DisplayName = "let duplicates parameter name")]
    [DataRow(
        "let name: object = 'KEY_1'; select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3064_UnsupportedScriptVariableType,
        "is not supported",
        DisplayName = "unsupported let type")]
    [DataRow(
        "param(root: string = 'KEY'); let name: string = $root + '_1'; select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3065_InvalidScriptVariableInitializer,
        "runtime parameter",
        DisplayName = "let initializer uses parameter")]
    [DataRow(
        "let name: int = 'abc'; select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3065_InvalidScriptVariableInitializer,
        "cannot be converted",
        DisplayName = "invalid let conversion")]
    [DataRow(
        "let name: string = $later; let later: string = 'KEY_1'; select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3066_ScriptVariableUsedBeforeDeclaration,
        "before it is declared",
        DisplayName = "let forward reference")]
    public void CompileForExecution_WhenScriptParameterBindingFails_ShouldUseSpecificEnvelope(
        string query,
        DiagnosticCode expectedCode,
        string messagePart)
    {
        var exception = CompileFails(query);

        AssertSingleError(exception, expectedCode, DiagnosticPhase.Bind, messagePart);
        AssertHasGuidance(exception);
        StringAssert.Contains(exception.FormatText(), "Core Spec");
        StringAssert.Contains(exception.FormatJson(), $"MQ{(int)expectedCode}");
    }

    [TestMethod]
    public void ValidateSchemaArguments_WhenParameterIsNestedInAnyRegisteredExpression_ShouldReportMq3062()
    {
        var parameter = new ParameterReferenceNode("name", typeof(string));
        var nestedExpressions = new Node[]
        {
            new CastNode(parameter, "string"),
            new ArrayIndexNode(new ParameterReferenceNode("names", typeof(string[])), new IntegerNode("0", string.Empty)),
            new IsNullNode(parameter, false),
            new BetweenNode(parameter, new StringNode("A"), new StringNode("Z")),
            new AccessMethodNode(
                new FunctionToken("ToString", TextSpan.Empty),
                new ArgsListNode([parameter]),
                null,
                false),
            new AddNode(parameter, new StringNode("_suffix"))
        };
        var diagnostics = new List<DiagnosticCode>();
        var binder = new ScriptParameterMetadataBinder(
            (code, _, _) =>
            {
                diagnostics.Add(code);
                return true;
            },
            _ => { });
        var source = new SchemaFromNode(
            "schema",
            "method",
            ArgsListNode.Empty,
            "source",
            typeof(object),
            0);

        foreach (var expression in nestedExpressions)
            binder.ValidateSchemaArguments(new ArgsListNode([expression]), source);

        CollectionAssert.AreEqual(
            Enumerable.Repeat(DiagnosticCode.MQ3062_InvalidScriptParameterSourceArgument, nestedExpressions.Length).ToArray(),
            diagnostics);
    }

    [TestMethod]
    [DataRow(
        "param(string author) select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ2031_InvalidScriptParameterDeclaration,
        "param(author: string)",
        DisplayName = "C#-style parameter")]
    [DataRow(
        "param([string]$author) select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ2032_UnsupportedScriptParameterSyntax,
        "PowerShell-style",
        DisplayName = "PowerShell-style parameter")]
    [DataRow(
        "def query(author: str = 'x') select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ2032_UnsupportedScriptParameterSyntax,
        "Python-style",
        DisplayName = "Python-style parameter")]
    [DataRow(
        "declare @author string; select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ2032_UnsupportedScriptParameterSyntax,
        "SQL variable",
        DisplayName = "SQL variable declaration")]
    [DataRow(
        "param(limit: int = $other) select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ2031_InvalidScriptParameterDeclaration,
        "primitive constants or null",
        DisplayName = "non-constant default")]
    [DataRow(
        "let string topic = 'important'; select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ2033_InvalidScriptVariableDeclaration,
        "let topic: string",
        DisplayName = "C#-style let declaration")]
    public void CompileForExecution_WhenScriptParameterSyntaxIsBorrowedFromOtherLanguages_ShouldUseSpecificEnvelope(
        string query,
        DiagnosticCode expectedCode,
        string messagePart)
    {
        var exception = CompileFails(query);

        AssertSingleError(exception, expectedCode, DiagnosticPhase.Parse, messagePart);
        AssertHasGuidance(exception);
    }

    [TestMethod]
    public void CompileWithDiagnostics_WhenParameterDefaultIsInvalid_ShouldReturnEnvelopeMetadata()
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            "param(limit: int = 'abc') select 1 from #EnvironmentVariables.All()",
            Guid.NewGuid().ToString(),
            new EnvironmentVariablesSchemaProvider(),
            LoggerResolver);

        var envelope = result.ToEnvelopes().Single();

        Assert.AreEqual(DiagnosticCode.MQ3061_InvalidScriptParameterDefault, envelope.Code);
        Assert.AreEqual(DiagnosticPhase.Bind, envelope.Phase);
        Assert.IsNotNull(envelope.Snippet);
        Assert.IsNotNull(envelope.Explanation);
        Assert.IsGreaterThan(0, envelope.SuggestedFixes.Count);
        StringAssert.Contains(envelope.DocsReference, "Script Parameters");
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
