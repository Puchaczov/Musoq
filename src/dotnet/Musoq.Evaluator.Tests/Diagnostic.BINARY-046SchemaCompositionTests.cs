using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.Unknown;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticBinary046SchemaCompositionTests
{
    private static readonly CompilationOptions CompilationOptions =
        new(usePrimitiveTypeValidation: false);

    [TestMethod]
    public void BinarySchema_MissingBaseSchema_ShouldReportTheBaseName()
    {
        const string query =
            "binary Child extends Missing { Value: byte };" +
            "select 1 from #test.files();";

        var result = Analyze(query);
        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            result,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            "missing binary inheritance target");

        Assert.AreEqual(SpanOf(query, "Missing"), diagnostic.Span);
        StringAssert.Contains(diagnostic.Message, "Missing");
    }

    [TestMethod]
    public void BinarySchema_TextBaseSchema_ShouldBeRejected()
    {
        const string query =
            "text Base { Value: rest };" +
            "binary Child extends Base { Extra: byte };" +
            "select 1 from #test.files();";

        var result = Analyze(query);
        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            result,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            "binary schema extending text schema");

        Assert.AreEqual(SpanOf(query, "Base", query.IndexOf("extends", StringComparison.Ordinal)), diagnostic.Span);
        StringAssert.Contains(diagnostic.Message, "non-binary");
    }

    [TestMethod]
    public void BinarySchema_GenericReferenceWithWrongArity_ShouldReportSchemaTypeError()
    {
        const string query =
            "binary Item { Value: byte };" +
            "binary Pair<T, U> { First: T, Second: U };" +
            "binary Container { Value: Pair<Item> };" +
            "select 1 from #test.files();";

        var result = Analyze(query);
        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            result,
            DiagnosticCode.MQ4007_InvalidSchemaFieldType,
            "generic schema arity");

        Assert.AreEqual(SpanOf(query, "Pair<Item>", query.IndexOf("Value: Pair", StringComparison.Ordinal)), diagnostic.Span);
        StringAssert.Contains(diagnostic.Message, "2");
        StringAssert.Contains(diagnostic.Message, "1");
    }

    [TestMethod]
    public void BinarySchema_PrimitiveGenericArgument_ShouldBeRejected()
    {
        const string query =
            "binary Pair<T> { Value: T };" +
            "binary Container { Value: Pair<int> };" +
            "select 1 from #test.files();";

        var result = Analyze(query);
        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            result,
            DiagnosticCode.MQ4007_InvalidSchemaFieldType,
            "primitive generic schema argument");

        Assert.AreEqual(SpanOf(query, "Pair<int>", query.IndexOf("Value: Pair", StringComparison.Ordinal)), diagnostic.Span);
        StringAssert.Contains(diagnostic.Message, "primitive");
    }

    [TestMethod]
    public void BinarySchema_OpenGenericReference_ShouldBeRejected()
    {
        const string query =
            "binary Pair<T> { Value: T };" +
            "binary Container { Value: Pair };" +
            "select 1 from #test.files();";

        var result = Analyze(query);
        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            result,
            DiagnosticCode.MQ4007_InvalidSchemaFieldType,
            "open generic schema reference");

        Assert.AreEqual(SpanOf(query, "Pair", query.IndexOf("Value: Pair", StringComparison.Ordinal)), diagnostic.Span);
        StringAssert.Contains(diagnostic.Message, "instantiated");
    }

    [TestMethod]
    public void BinarySchema_UndefinedTextAsSchema_ShouldReportReferenceDiagnostic()
    {
        const string query =
            "binary Packet { Value: string[4] utf8 as MissingText };" +
            "select 1 from #test.files();";

        var result = Analyze(query);
        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            result,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            "undefined binary-text composition schema");

        Assert.AreEqual(SpanOf(query, "string[4] utf8 as MissingText"), diagnostic.Span);
        StringAssert.Contains(diagnostic.Message, "MissingText");
    }

    [TestMethod]
    public void BinarySchema_ChildOverride_ShouldRemainValidAfterDuplicateValidation()
    {
        const string query =
            "binary Base { Version: byte, Tag: byte };" +
            "binary Child extends Base { Version: short le, Extra: byte };" +
            "select 1 from #test.files();";

        var result = Analyze(query);

        DiagnosticContractTestAssertions.AssertNoErrors(result, "valid inherited field override");
    }

    [TestMethod]
    public void BinarySchema_DirectGeneratorMissingBase_ShouldFailClosed()
    {
        var registry = new SchemaRegistry();
        registry.Register(
            "Child",
            new BinarySchemaNode("Child", [new FieldDefinitionNode("Value", ByteType())], "Missing"));

        var exception = Assert.ThrowsExactly<ConstructionNotYetSupported>(
            () => new Musoq.Evaluator.Visitors.InterpreterCodeGenerator(registry).GenerateAll());

        Assert.AreEqual(DiagnosticCode.MQ4016_UnsupportedSchemaConstruction, exception.Code);
        StringAssert.Contains(exception.Message, "Missing");
    }

    [TestMethod]
    public void BinarySchema_DirectGeneratorNonBinaryBase_ShouldFailClosed()
    {
        var registry = new SchemaRegistry();
        registry.Register("Base", new TextSchemaNode("Base", []));
        registry.Register("Child", new BinarySchemaNode("Child", [], "Base"));

        var exception = Assert.ThrowsExactly<ConstructionNotYetSupported>(
            () => new Musoq.Evaluator.Visitors.InterpreterCodeGenerator(registry).GenerateAll());

        Assert.AreEqual(DiagnosticCode.MQ4016_UnsupportedSchemaConstruction, exception.Code);
        StringAssert.Contains(exception.Message, "non-binary");
    }

    [TestMethod]
    public void BinarySchema_DirectGeneratorCyclicInheritance_ShouldFailClosed()
    {
        var registry = new SchemaRegistry();
        registry.Register("A", new BinarySchemaNode("A", [], "B"));
        registry.Register("B", new BinarySchemaNode("B", [], "A"));

        var exception = Assert.ThrowsExactly<ConstructionNotYetSupported>(
            () => new Musoq.Evaluator.Visitors.InterpreterCodeGenerator(registry).GenerateAll());

        Assert.AreEqual(DiagnosticCode.MQ4016_UnsupportedSchemaConstruction, exception.Code);
        StringAssert.Contains(exception.Message, "cycle");
    }

    private static PrimitiveTypeNode ByteType()
    {
        return new PrimitiveTypeNode(PrimitiveTypeName.Byte, Endianness.NotApplicable);
    }

    private static QueryAnalysisResult Analyze(string query)
    {
        return new QueryAnalyzer(
                new UnknownSchemaProvider(Array.Empty<dynamic>()),
                compilationOptions: CompilationOptions)
            .Analyze(query);
    }

    private static TextSpan SpanOf(string source, string value, int start = 0)
    {
        var offset = source.IndexOf(value, start, StringComparison.Ordinal);
        Assert.IsTrue(offset >= 0, $"'{value}' was not found in the test query.");
        return new TextSpan(offset, value.Length);
    }
}
