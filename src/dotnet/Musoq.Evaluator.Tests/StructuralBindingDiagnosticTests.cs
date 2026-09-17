using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Musoq.Evaluator.Tests.Schema.EnvironmentVariable;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class StructuralBindingDiagnosticTests : EnvironmentVariablesTestBase
{
    [TestMethod]
    [DataRow(
        "param(patterns: (Id: string, id: string)[]); select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3060_UnsupportedScriptParameterType,
        "declared more than once",
        DisplayName = "duplicate structural declaration fields")]
    [DataRow(
        "param(options: (Enabled: bool) = (Unknown: true)); select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3061_InvalidScriptParameterDefault,
        "unexpected field",
        DisplayName = "unexpected field in parameter default")]
    [DataRow(
        "param(options: (Enabled: bool, Codes: int[]) = (Enabled: true)); select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3061_InvalidScriptParameterDefault,
        "missing required field 'Codes'",
        DisplayName = "missing required field in parameter default")]
    [DataRow(
        "param(options: (Enabled: bool) = array { true }); select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3061_InvalidScriptParameterDefault,
        "must be a named record",
        DisplayName = "record parameter receives array default")]
    [DataRow(
        "param(numbers: int[] = array { 'one' }); select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3061_InvalidScriptParameterDefault,
        "cannot be converted",
        DisplayName = "incompatible primitive array default")]
    [DataRow(
        "let values = array { (Id: 'todo'), (Id: 42) }; select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3065_InvalidScriptVariableInitializer,
        "incompatible",
        DisplayName = "incompatible inferred record field types")]
    [DataRow(
        "let values = array {}; select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3065_InvalidScriptVariableInitializer,
        "expected element type",
        DisplayName = "empty inferred array")]
    [DataRow(
        "let values = array { null, null }; select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3065_InvalidScriptVariableInitializer,
        "all-null",
        DisplayName = "all-null inferred array")]
    [DataRow(
        "let options: (Enabled: bool) = (Enabled: 1); select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3065_InvalidScriptVariableInitializer,
        "cannot be converted",
        DisplayName = "invalid explicit structural scalar conversion")]
    [DataRow(
        "let options: (Enabled: bool) = (Enabled: true, enabled: false); select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3065_InvalidScriptVariableInitializer,
        "Duplicate structural field",
        DisplayName = "case-only duplicate record fields")]
    [DataRow(
        "let options: (Enabled: bool) = (Enabled: true, Unknown: false); select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3065_InvalidScriptVariableInitializer,
        "unexpected field",
        DisplayName = "unexpected field in explicit structural let")]
    [DataRow(
        "let options: (Enabled: bool, Codes: int[]) = (Enabled: true); select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3065_InvalidScriptVariableInitializer,
        "missing required field 'Codes'",
        DisplayName = "missing required field in explicit structural let")]
    [DataRow(
        "let values = array { (Id: 1), 2 }; select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3065_InvalidScriptVariableInitializer,
        "cannot mix",
        DisplayName = "mixed record and scalar array elements")]
    [DataRow(
        "let values: int[][] = array { array { 1 }, (2) }; select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3065_InvalidScriptVariableInitializer,
        "must be an array",
        DisplayName = "nested array receives scalar element")]
    [DataRow(
        "let options: (Enabled: bool) = (Enabled: null); select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3065_InvalidScriptVariableInitializer,
        "null value",
        DisplayName = "null supplied to non-nullable structural field")]
    public void CompileForExecution_WhenStructuralBindingFails_ShouldUseSpecificEnvelope(
        string query,
        DiagnosticCode expectedCode,
        string messagePart)
    {
        var exception = Assert.Throws<Musoq.Converter.Exceptions.MusoqQueryException>(() =>
            Musoq.Converter.InstanceCreator.CompileForExecution(
                query,
                Guid.NewGuid().ToString(),
                new EnvironmentVariablesSchemaProvider(),
                LoggerResolver));

        AssertSingleError(exception, expectedCode, DiagnosticPhase.Bind, messagePart);
        AssertHasGuidance(exception);
    }

    [TestMethod]
    public void InferredRecordArray_WithCompatibleNumericFields_ShouldRemainValid()
    {
        var visitor = Analyze(
            "let values = array { (Id: 1), (Id: 2L) }; " +
            "select 1 from #EnvironmentVariables.All()");

        var definition = visitor.ScriptVariableDefinitions.Single();
        Assert.AreEqual("(Id: long)[]", definition.StructuralType!.ToCanonicalSql());
    }

    [TestMethod]
    public void ExplicitNullableRecordAndCollectionShapes_ShouldRemainValid()
    {
        var visitor = Analyze(
            "param(items: (Id: string)?[]? = null, options: (Enabled: bool) = (Enabled: true), numbers: int?[] = array { 1, null }); " +
            "select 1 from #EnvironmentVariables.All()");

        var definitions = visitor.ScriptParameterDefinitions.ToArray();
        Assert.HasCount(3, definitions);
        Assert.IsTrue(definitions.All(static definition => definition.Contract.IsStructured));
        Assert.AreEqual("(Id: string)?[]?", definitions[0].Contract.CanonicalTypeName);
        Assert.AreEqual("(Enabled: bool)", definitions[1].Contract.CanonicalTypeName);
        Assert.AreEqual("int?[]", definitions[2].Contract.CanonicalTypeName);
    }

    private static BuildMetadataAndInferTypesVisitor Analyze(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Musoq.Parser.Parser(lexer);
        var tree = parser.ComposeAll();
        var visitor = new BuildMetadataAndInferTypesVisitor(
            new EnvironmentVariablesSchemaProvider(),
            new System.Collections.Generic.Dictionary<string, string[]>(),
            new Mock<Microsoft.Extensions.Logging.ILogger<BuildMetadataAndInferTypesVisitor>>().Object);
        tree.Accept(new BuildMetadataAndInferTypesTraverseVisitor(visitor));
        return visitor;
    }
}
