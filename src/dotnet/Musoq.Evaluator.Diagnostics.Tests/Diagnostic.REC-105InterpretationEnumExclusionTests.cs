using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticREC105InterpretationEnumExclusionTests
{
    public static IEnumerable<object[]> DirectEnumAnnotationCases()
    {
        yield return ["REC-105-I01", "binary Packet { Status: JobStatus }", DiagnosticCode.MQ2030_UnsupportedSyntax];
        yield return ["REC-105-I02", "text Packet { Status: JobStatus }", DiagnosticCode.MQ4003_UndefinedSchemaReference];
        yield return ["REC-105-I03", "binary Packet { Status: jobstatus }", DiagnosticCode.MQ2030_UnsupportedSyntax];
        yield return ["REC-105-I04", "text Packet { Status: jobstatus }", DiagnosticCode.MQ4003_UndefinedSchemaReference];
        yield return ["REC-105-I05", "binary Packet { Status: enum JobStatus }", DiagnosticCode.MQ2001_UnexpectedToken];
        yield return ["REC-105-I06", "text Packet { Status: enum JobStatus }", DiagnosticCode.MQ2001_UnexpectedToken];
        yield return ["REC-105-I07", "binary Packet { Access: flags FileAccess }", DiagnosticCode.MQ2001_UnexpectedToken];
        yield return ["REC-105-I08", "text Packet { Access: flags FileAccess }", DiagnosticCode.MQ2001_UnexpectedToken];
    }

    [TestMethod]
    [DynamicData(nameof(DirectEnumAnnotationCases))]
    public void DirectEnumAnnotations_ShouldRemainInterpretationSchemaReferencesOrSyntaxErrors(
        string caseId,
        string schema,
        DiagnosticCode expectedCode)
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(caseId));

        if (expectedCode == DiagnosticCode.MQ2001_UnexpectedToken)
        {
            var syntax = Assert.ThrowsExactly<SyntaxException>(() => ParseSchema(schema));
            Assert.AreEqual(expectedCode, syntax.Code, caseId);
            return;
        }

        var node = ParseSchema(schema);
        var registry = new SchemaRegistry();
        var visitor = new SchemaDefinitionVisitor(registry);
        var exception = Assert.ThrowsExactly<QuerySyntaxException>(() =>
            node.Accept(visitor));

        Assert.AreEqual(expectedCode, exception.Code, caseId);
        Assert.IsTrue(exception.Span.HasValue, caseId);
        Assert.IsFalse(registry.TryGetSchema("JobStatus", out _), caseId);
    }

    [TestMethod]
    public void InterpretationEnumBoundary_Control_ShouldNotCreateALogicalEnumDescriptor()
    {
        var binary = (BinarySchemaNode)ParseSchema("binary Packet { Status: JobStatus }");
        var binaryField = (FieldDefinitionNode)binary.Fields[0];
        Assert.IsInstanceOfType<SchemaReferenceTypeNode>(binaryField.TypeAnnotation);

        var text = (TextSchemaNode)ParseSchema("text Packet { Status: JobStatus }");
        Assert.AreEqual(TextFieldType.SchemaReference, text.Fields[0].FieldType);
        Assert.AreEqual("JobStatus", text.Fields[0].PrimaryValue);
    }

    [TestMethod]
    public void InterpretationEnumBoundary_Control_ShouldKeepTableEnumSyntaxSeparate()
    {
        var lexer = new Lexer(
            "enum JobStatus : short { Running = 20s }; table Jobs { Status: JobStatus };",
            true);
        var parser = new Musoq.Parser.Parser(lexer);
        var root = parser.ComposeAll();

        Assert.IsNotNull(root);
        StringAssert.Contains(root.ToString(), "JobStatus");
    }

    [TestMethod]
    public void DirectEnumAnnotationMatrix_ShouldContainEightRegisteredCases()
    {
        var cases = new List<object[]>(DirectEnumAnnotationCases());
        Assert.HasCount(8, cases);
        Assert.AreEqual(8, cases.Select(static values => (string)values[0])
            .Distinct(StringComparer.Ordinal).Count());
        Assert.HasCount(4, cases.Where(static values => ((DiagnosticCode)values[2]) == DiagnosticCode.MQ2001_UnexpectedToken));
        Assert.HasCount(4, cases.Where(static values => ((string)values[1]).StartsWith("binary", StringComparison.Ordinal)));
        Assert.HasCount(4, cases.Where(static values => ((string)values[1]).StartsWith("text", StringComparison.Ordinal)));
    }

    private static Node ParseSchema(string schema)
    {
        var lexer = new Lexer(schema, true);
        return new SchemaParser(lexer).ParseSchema();
    }
}
