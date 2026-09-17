using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Build;
using Musoq.Evaluator.Tests.Schema.Unknown;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Schema.Interpreters;
using RuntimeParseException = Musoq.Schema.Interpreters.ParseException;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Reproduces the two same-domain documentation conflicts assigned to REC-140.
///     The approved documentation-only resolutions are tested alongside the retained
///     negative boundaries so implementation semantics are not silently broadened.
/// </summary>
[TestClass]
public sealed class DiagnosticREC140DocumentationConflictTests
{
    private static readonly CompilationOptions CompilationOptions =
        new(usePrimitiveTypeValidation: false);

    [TestMethod]
    public void UntilExample_ShouldProveDelimiterIsConsumedBeforeFollowingLiteral()
    {
        const string schemaText =
            "text Example { Key: until ':', _: literal ': ', Value: until '\\n' }";

        var interpreter = CompileTextInterpreter(schemaText, "Example");

        var doubleDelimiter = InvokeParse(interpreter, "key:: value\n");
        Assert.AreEqual("key", GetPropertyValue<string>(doubleDelimiter, "Key"));
        Assert.AreEqual("value", GetPropertyValue<string>(doubleDelimiter, "Value"));

        var singleDelimiterFailure = AssertParseFailure(interpreter, "key: value\n");
        Assert.AreEqual(ParseErrorCode.LiteralMismatch, singleDelimiterFailure.ErrorCode);
        Assert.AreEqual("_", singleDelimiterFailure.FieldName);
    }

    [TestMethod]
    public void UntilCorrectedExample_ShouldParseSingleDelimiterAndSpace()
    {
        const string schemaText =
            "text Example { Key: until ':', _: literal ' ', Value: until '\\n' }";

        var result = InvokeParse(CompileTextInterpreter(schemaText, "Example"), "key: value\n");

        Assert.AreEqual("key", GetPropertyValue<string>(result, "Key"));
        Assert.AreEqual("value", GetPropertyValue<string>(result, "Value"));
    }

    [TestMethod]
    public void CheckExample_ShouldRejectForwardFieldReferenceBeforeCodeGeneration()
    {
        const string query =
            "binary ValidatedHeader { " +
            "Magic: int le check Magic = 0xDEADBEEF, " +
            "Version: short le check Version >= 1 AND Version <= 5, " +
            "Length: int le check Length <= 1048576, " +
            "Checksum: int le check Checksum = Crc32(Data), " +
            "Data: byte[Length] };" +
            "select 1 from #test.files();";

        var result = new QueryAnalyzer(
                new UnknownSchemaProvider(Array.Empty<dynamic>()),
                compilationOptions: CompilationOptions)
            .Analyze(query);

        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            result,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            "documentation CHECK example forward reference");

        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual("Data", query.Substring(diagnostic.Span.Start, diagnostic.Span.Length));
    }

    [TestMethod]
    public void CorrectedCheckExample_ShouldRemainValidWhenDataPrecedesChecksum()
    {
        const string query =
            "binary ValidatedHeader { " +
            "Magic: int le check Magic = 0xDEADBEEF, " +
            "Version: short le check Version >= 1 AND Version <= 5, " +
            "Length: int le check Length <= 1048576, " +
            "Data: byte[Length], " +
            "Checksum: int le check Checksum = Crc32(Data) };" +
            "select 1 from #test.files();";

        var result = new QueryAnalyzer(
                new UnknownSchemaProvider(Array.Empty<dynamic>()),
                compilationOptions: CompilationOptions)
            .Analyze(query);

        DiagnosticContractTestAssertions.AssertNoErrors(
            result,
            "corrected CHECK definition-before-use example");
    }

    [TestMethod]
    public void SchemaReferenceExample_ShouldRejectUseBeforeDefinition()
    {
        const string query =
            "binary Triangle { A: Point, B: Point, C: Point };" +
            "binary Point { X: float le, Y: float le };" +
            "select 1 from #test.files();";

        var result = new QueryAnalyzer(
                new UnknownSchemaProvider(Array.Empty<dynamic>()),
                compilationOptions: CompilationOptions)
            .Analyze(query);

        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            result,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            "documentation schema reference forward declaration");

        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual("Point", query.Substring(diagnostic.Span.Start, diagnostic.Span.Length));
    }

    [TestMethod]
    public void CorrectedSchemaReferenceExample_ShouldRemainValidWhenDefinitionPrecedesUse()
    {
        const string query =
            "binary Point { X: float le, Y: float le };" +
            "binary Triangle { A: Point, B: Point, C: Point };" +
            "select 1 from #test.files();";

        var result = new QueryAnalyzer(
                new UnknownSchemaProvider(Array.Empty<dynamic>()),
                compilationOptions: CompilationOptions)
            .Analyze(query);

        DiagnosticContractTestAssertions.AssertNoErrors(
            result,
            "corrected schema reference definition-before-use example");
    }

    private static object CompileTextInterpreter(string schemaText, string schemaName)
    {
        var schema = new SchemaParser(new Lexer(schemaText, true)).ParseSchema();
        Assert.IsInstanceOfType<TextSchemaNode>(schema);

        var registry = new SchemaRegistry();
        registry.Register(schemaName, (TextSchemaNode)schema);

        var code = new InterpreterCodeGenerator(registry).GenerateAll();
        using var compilationUnit = new InterpreterCompilationUnit(
            $"REC140_{Guid.NewGuid():N}",
            code);

        Assert.IsTrue(
            compilationUnit.Compile(),
            string.Join(Environment.NewLine, compilationUnit.GetErrorMessages()));

        var interpreterType = compilationUnit.GetInterpreterType(schemaName);
        Assert.IsNotNull(interpreterType, schemaName);
        return Activator.CreateInstance(interpreterType)!;
    }

    private static object InvokeParse(object interpreter, string value)
    {
        var method = interpreter.GetType().GetMethod("Parse", [typeof(string)]);
        Assert.IsNotNull(method);
        return method.Invoke(interpreter, [value])!;
    }

    private static RuntimeParseException AssertParseFailure(object interpreter, string value)
    {
        var wrapper = Assert.ThrowsExactly<TargetInvocationException>(() => InvokeParse(interpreter, value));
        Assert.IsNotNull(wrapper.InnerException);
        Assert.IsInstanceOfType<RuntimeParseException>(wrapper.InnerException);
        return (RuntimeParseException)wrapper.InnerException!;
    }

    private static T GetPropertyValue<T>(object value, string propertyName)
    {
        var property = value.GetType().GetProperty(propertyName);
        Assert.IsNotNull(property, propertyName);
        return (T)property.GetValue(value)!;
    }
}
