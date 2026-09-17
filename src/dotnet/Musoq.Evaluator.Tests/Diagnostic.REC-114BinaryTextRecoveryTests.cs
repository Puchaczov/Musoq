using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Build;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Schema.Interpreters;
using RuntimeParseException = Musoq.Schema.Interpreters.ParseException;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticREC114BinaryTextRecoveryTests : BinaryInterpretationTestBase
{
    private static readonly IReadOnlyList<RecoveryCandidate> CandidateCases =
    [
        SchemaInvalid(
            "A01", "BINARY_SCHEMA",
            "binary Packet { Value: int le }",
            "binary Packet { Value: int }",
            DiagnosticCode.MQ4005_InvalidEndianness,
            "endianness",
            "missing multi-byte endianness",
            "4.2 Primitive Types"),
        SchemaInvalid(
            "A02", "BINARY_SCHEMA",
            "binary Packet { Value: int le }",
            "binary Packet { Value: int middle }",
            DiagnosticCode.MQ4005_InvalidEndianness,
            "middle",
            "invalid multi-byte endianness",
            "4.2 Primitive Types"),
        SchemaInvalid(
            "A03", "BINARY_SCHEMA",
            "binary Packet { Value: byte }",
            "binary Packet { Value: byte le }",
            DiagnosticCode.MQ4005_InvalidEndianness,
            "le",
            "endianness on a single-byte field",
            "4.2 Primitive Types"),
        SchemaInvalid(
            "A04", "BINARY_SCHEMA",
            "binary Packet { Value: string[4] utf8 }",
            "binary Packet { Value: string[-5] utf8 }",
            DiagnosticCode.MQ4001_InvalidBinarySchemaField,
            "-5",
            "negative fixed string size",
            "4.2.4 String Types"),
        SchemaValid(
            "A05", "BINARY_SCHEMA",
            "binary Packet { Value: bits[64] }",
            "maximum-width bit field",
            "4.7 Bit Fields"),
        SchemaValid(
            "A06", "BINARY_SCHEMA",
            "binary Packet { Value: byte[2] const [1, 2] }",
            "valid constant byte-list constraint",
            "4.9.1 Field Value Validations"),

        SchemaInvalid(
            "B01", "TEXT_SCHEMA",
            "text Log { Value: rest }",
            "text Log { Content: switch { } }",
            DiagnosticCode.MQ4002_InvalidTextSchemaField,
            "switch",
            "empty text alternative",
            "5.10 Alternatives (Switch)"),
        SchemaInvalid(
            "B02", "TEXT_SCHEMA",
            "text Log { Content: switch { pattern 'X' => Payload, _ => Fallback } }",
            "text Log { Content: switch { _ => Fallback, pattern 'X' => Payload } }",
            DiagnosticCode.MQ4002_InvalidTextSchemaField,
            "pattern",
            "default branch must be last",
            "5.10 Alternatives (Switch)"),
        SchemaInvalid(
            "B03", "TEXT_SCHEMA",
            "text Log { Content: switch { pattern 'X' => Payload, _ => Fallback } }",
            "text Log { Content: switch { pattern 'X' => Payload, _ => Fallback, _ => Other } }",
            DiagnosticCode.MQ4002_InvalidTextSchemaField,
            "_",
            "duplicate text default branch",
            "5.10 Alternatives (Switch)"),
        SchemaInvalid(
            "B04", "TEXT_SCHEMA",
            "text Log { Value: between '[' ']' escaped '~' }",
            "text Log { Value: between '[' ']' escaped 'ab' }",
            DiagnosticCode.MQ4002_InvalidTextSchemaField,
            "ab",
            "multi-character escape modifier",
            "5.4.3 Escaped Content"),
        SchemaValid(
            "B05", "TEXT_SCHEMA",
            "text Log { Value: between '[' ']' escaped '~' }",
            "valid custom escaped delimiter",
            "5.4.3 Escaped Content"),
        SchemaValid(
            "B06", "TEXT_SCHEMA",
            "text Log { Value: chars[3] trim }",
            "valid fixed-width text modifier",
            "5.5 Fixed-Width Fields"),

        BinaryRuntime(
            "C01", "BINARY_SUBSTRATE",
            "binary Header { Magic: byte, Value: int le }",
            "Header",
            [1, 2, 0, 0, 0],
            [1, 2],
            ParseErrorCode.InsufficientData,
            "Value",
            "truncated multi-byte value",
            "4.2 Primitive Types"),
        BinaryRuntime(
            "C02", "BINARY_SUBSTRATE",
            "binary Header { Magic: byte check Magic = 0x7F }",
            "Header",
            [0x7F],
            [0x00],
            ParseErrorCode.ValidationFailed,
            "Magic",
            "failed field validation",
            "4.9 Validation Constraints"),
        BinaryRuntime(
            "C03", "BINARY_SUBSTRATE",
            "binary Header { Value: string[2] utf8 }",
            "Header",
            [(byte)'O', (byte)'K'],
            [0xC3, 0x28],
            ParseErrorCode.EncodingError,
            "Value",
            "invalid UTF-8 substrate",
            "9.2 Error Categories"),
        BinaryRuntime(
            "C04", "BINARY_SUBSTRATE",
            "binary Header { Offset: int le, Value: byte at Offset }",
            "Header",
            [0, 0, 0, 0, 0xAA],
            [0xFF, 0xFF, 0xFF, 0xFF, 0xAA],
            ParseErrorCode.InvalidPosition,
            "",
            "negative runtime position",
            "4.8 Absolute Positioning"),
        BinaryRuntime(
            "C05", "BINARY_SUBSTRATE",
            "binary Header { Length: sbyte, Payload: byte[Length] }",
            "Header",
            [2, 0xAA, 0xBB],
            [0xFF],
            ParseErrorCode.InvalidSize,
            "Payload",
            "negative dynamic payload size",
            "4.2.3 Byte Arrays"),
        BinaryRuntime(
            "C06", "BINARY_SUBSTRATE",
            "binary Header { Length: byte, Payload: byte[Length] }",
            "Header",
            [2, 0xAA, 0xBB],
            [3, 0x01, 0x02, 0x03],
            null,
            "",
            "valid dynamic payload boundary",
            "4.2.3 Byte Arrays"),

        TextRuntime(
            "D01", "TEXT_SUBSTRATE",
            "text Record { Value: pattern '\\d+' }",
            "Record",
            "123",
            "abc",
            ParseErrorCode.PatternMismatch,
            "Value",
            "pattern mismatch at current position",
            "5.2 Pattern Matching"),
        TextRuntime(
            "D02", "TEXT_SUBSTRATE",
            "text Record { Value: literal 'OK' }",
            "Record",
            "OK",
            "NO",
            ParseErrorCode.LiteralMismatch,
            "Value",
            "literal mismatch at current position",
            "5.3 Literal Matching"),
        TextRuntime(
            "D03", "TEXT_SUBSTRATE",
            "text Record { Value: until ':' }",
            "Record",
            "name:value",
            "missing-delimiter",
            ParseErrorCode.DelimiterNotFound,
            "Value",
            "missing until delimiter",
            "5.4.1 Until Delimiter"),
        TextRuntime(
            "D04", "TEXT_SUBSTRATE",
            "text Record { Value: between '[' ']' }",
            "Record",
            "[value]",
            "value]",
            ParseErrorCode.ExpectedDelimiter,
            "Value",
            "missing opening delimiter",
            "5.4.2 Between Delimiters"),
        TextRuntime(
            "D05", "TEXT_SUBSTRATE",
            "text Record { Value: chars[3] }",
            "Record",
            "abc",
            "ab",
            ParseErrorCode.InsufficientData,
            "Value",
            "truncated fixed-width text",
            "5.5 Fixed-Width Fields"),
        TextRuntime(
            "D06", "TEXT_SUBSTRATE",
            "text Record { Value: rest trim }",
            "Record",
            " value ",
            " other ",
            null,
            "",
            "valid rest capture with modifier",
            "5.7 Rest of Input"),

        SchemaInvalid(
            "E01", "REFERENCE_AND_BRANCH",
            "binary Packet { Type: byte, Payload: switch Type { 1 => Login: byte } }",
            "binary Packet { Type: byte, Payload: switch Missing { 1 => Login: byte } }",
            DiagnosticCode.MQ4011_SwitchSelectorNotPreviousField,
            "Missing",
            "switch selector forward or unknown reference",
            "4.12.4 Diagnostics"),
        SchemaInvalid(
            "E02", "REFERENCE_AND_BRANCH",
            "binary Packet { Type: byte, Payload: switch Type { 1 => Same: byte } }",
            "binary Packet { Type: byte, Payload: switch Type { 1 => Same: byte, 2 => same: byte } }",
            DiagnosticCode.MQ4012_DuplicateSwitchBranchAlias,
            "same",
            "duplicate switch branch alias",
            "4.12.4 Diagnostics"),
        SchemaInvalid(
            "E03", "REFERENCE_AND_BRANCH",
            "binary Packet { Type: byte, Payload: switch Type { 1 => Login: byte } }",
            "binary Packet { Type: byte, Payload: switch Type { 'login' => Login: byte } }",
            DiagnosticCode.MQ4013_InvalidSwitchCaseLabel,
            "login",
            "case label incompatible with selector",
            "4.12.4 Diagnostics"),
        SchemaInvalid(
            "E04", "REFERENCE_AND_BRANCH",
            "binary Packet { Type: byte, Payload: switch Type { 1 => Login: byte, _ => Raw: byte[1] } }",
            "binary Packet { Type: byte, Payload: switch Type { _ => Raw: byte[1], 1 => Login: byte } }",
            DiagnosticCode.MQ4013_InvalidSwitchCaseLabel,
            "_",
            "switch default branch must be last",
            "4.12.3 Default and No-Match Behavior"),
        SchemaInvalid(
            "E05", "REFERENCE_AND_BRANCH",
            "text Config { Content: switch { pattern 'X' => Payload, _ => Fallback } }",
            "text Config { Content: switch { _ => Fallback, pattern 'X' => Payload } }",
            DiagnosticCode.MQ4002_InvalidTextSchemaField,
            "pattern",
            "text default branch must be last",
            "5.10 Alternatives (Switch)"),
        SchemaValid(
            "E06", "REFERENCE_AND_BRANCH",
            "binary Packet { Type: byte, Payload: switch Type { 1 => Login: byte, _ => Raw: byte[1] } }",
            "valid selector, branch and default boundary",
            "4.12 Switch (Tagged Union) Payloads"),

        BinaryRuntime(
            "F01", "REPETITION_PROGRESS",
            "binary Stream { Items: byte repeat until eof }",
            "Stream",
            [],
            [],
            null,
            "",
            "zero-length EOF repetition is an empty result",
            "4.10 Repetition Until Condition"),
        BinaryRuntime(
            "F02", "REPETITION_PROGRESS",
            "binary Stream { Items: byte repeat until eof }",
            "Stream",
            [1, 2, 3],
            [4, 5, 6],
            null,
            "",
            "EOF repetition consumes each byte",
            "4.10 Repetition Until Condition"),
        BinaryRuntime(
            "F03", "REPETITION_PROGRESS",
            "binary Stream { Items: byte[0] repeat until eof }",
            "Stream",
            [],
            [1],
            ParseErrorCode.MaxIterationsExceeded,
            "Items",
            "zero-progress binary repetition",
            "4.10 Repetition Until Condition"),
        TextRuntimeWithFixture(
            "F04", "REPETITION_PROGRESS",
            "text Empty { }; text Container { Items: repeat Empty }",
            "Container",
            "TEXT_ZERO_PROGRESS",
            "",
            "y",
            ParseErrorCode.MaxIterationsExceeded,
            "Items",
            "zero-progress text repetition",
            "5.9 Repetition"),
        BinaryRuntime(
            "F05", "REPETITION_PROGRESS",
            "binary Stream { Items: byte[2] repeat until eof }",
            "Stream",
            [1, 2, 3, 4],
            [1],
            ParseErrorCode.InsufficientData,
            "Items",
            "truncated repeated binary element",
            "4.10 Repetition Until Condition"),
        BinaryRuntime(
            "F06", "REPETITION_PROGRESS",
            "binary Stream { Items: byte repeat until eof }",
            "Stream",
            [1, 2],
            [3, 4],
            null,
            "",
            "valid repeated binary elements",
            "4.10 Repetition Until Condition"),

        BinaryRuntimeWithFixture(
            "G01", "BOUNDED_SUBSTREAM",
            "binary Body { A: byte, B: byte }; binary Packet { Length: byte, Payload: substream[Length] as Body exact, Tail: byte }",
            "Packet",
            "EXACT",
            [2, 0xAA, 0xBB, 0xCC],
            [1, 0xAA, 0xCC],
            ParseErrorCode.InsufficientData,
            "",
            "nested parser overread is bounded"),
        BinaryRuntimeWithFixture(
            "G02", "BOUNDED_SUBSTREAM",
            "binary Body { A: byte, B: byte }; binary Packet { Length: byte, Payload: substream[Length] as Body exact, Tail: byte }",
            "Packet",
            "EXACT",
            [2, 0xAA, 0xBB, 0xCC],
            [3, 0xAA, 0xBB, 0xCC],
            ParseErrorCode.ValidationFailed,
            "Payload",
            "exact substream rejects under-consumption"),
        BinaryRuntimeWithFixture(
            "G03", "BOUNDED_SUBSTREAM",
            "binary Packet { Length: byte, Payload: substream[Length] raw, Tail: byte }",
            "Packet",
            "RAW",
            [2, 0xAA, 0xBB, 0xCC],
            [4, 0xAA, 0xBB, 0xCC],
            ParseErrorCode.InsufficientData,
            "Payload",
            "raw substream length cannot overrun input"),
        BinaryRuntimeWithFixture(
            "G04", "BOUNDED_SUBSTREAM",
            "binary Packet { Length: sbyte, Payload: substream[Length] raw }",
            "Packet",
            "NEGATIVE",
            [2, 0xAA, 0xBB],
            [0xFF],
            ParseErrorCode.InvalidSize,
            "Payload",
            "negative substream length"),
        BinaryRuntimeWithFixture(
            "G05", "BOUNDED_SUBSTREAM",
            "binary Body { A: byte, B: byte }; binary Packet { Length: byte, Payload: substream[Length] as Body exact, Tail: byte }",
            "Packet",
            "EXACT",
            [2, 0xAA, 0xBB, 0xCC],
            [2, 0x10, 0x20, 0x30],
            null,
            "",
            "valid exact bounded payload"),
        BinaryRuntimeWithFixture(
            "G06", "BOUNDED_SUBSTREAM",
            "binary Body { A: byte, B: byte }; binary Packet { Length: byte, Payload: substream[Length] as Body lax, Tail: byte }",
            "Packet",
            "LAX",
            [3, 0xAA, 0xBB, 0xCC, 0xDD],
            [3, 0x11, 0x22, 0x33, 0x44],
            null,
            "",
            "valid lax bounded payload resumes parent cursor"),

        SchemaInvalid(
            "H01", "DOMAIN_BOUNDARY",
            "binary Packet { Value: int le }",
            "binary Packet { Value: int }",
            DiagnosticCode.MQ4005_InvalidEndianness,
            "Value",
            "schema failure remains compile-time MQ diagnostic",
            "9.1 Parse Error Structure"),
        SchemaInvalid(
            "H02", "DOMAIN_BOUNDARY",
            "text Log { Value: pattern '[A-Z]+' }",
            "text Log { Value: pattern '(?=abc)abc' }",
            DiagnosticCode.MQ4002_InvalidTextSchemaField,
            "(?=abc)",
            "unsupported pattern construct remains schema diagnostic",
            "9.2 Error Categories"),
        SchemaValid(
            "H03", "DOMAIN_BOUNDARY",
            "binary Packet { Flags: bits[3], _: align[8], Value: byte }",
            "valid binary alignment schema",
            "4.7 Bit Fields"),
        SchemaValid(
            "H04", "DOMAIN_BOUNDARY",
            "text Record { Code: chars[4] trim upper, Tail: rest }",
            "valid text modifier schema",
            "5.12 Text Field Modifiers"),
        SchemaValid(
            "H05", "DOMAIN_BOUNDARY",
            "binary Packet { Flag: byte, Payload: byte when Flag = 1 }",
            "valid conditional binary schema",
            "4.5 Conditional Fields"),
        SchemaValid(
            "H06", "DOMAIN_BOUNDARY",
            "text Record { Value: rest }",
            "valid text substrate boundary",
            "9.3 Error Behavior")
    ];

    [TestMethod]
    public void CandidateMatrix_ShouldHonorFrozenContract()
    {
        Assert.HasCount(48, CandidateCases);
        Assert.HasCount(8, CandidateCases.GroupBy(static candidate => candidate.Family));
        Assert.IsTrue(CandidateCases.GroupBy(static candidate => candidate.Family).All(static group => group.Count() == 6));
        Assert.AreEqual(32, CandidateCases.Count(static candidate => candidate.IsInvalid));
        Assert.AreEqual(16, CandidateCases.Count(static candidate => !candidate.IsInvalid));

        foreach (var candidate in CandidateCases)
        {
            switch (candidate.Kind)
            {
                case CandidateKind.Schema:
                    AssertSchemaCandidate(candidate);
                    break;
                case CandidateKind.BinaryRuntime:
                    AssertBinaryRuntimeCandidate(candidate);
                    break;
                case CandidateKind.TextRuntime:
                    AssertTextRuntimeCandidate(candidate);
                    break;
                default:
                    Assert.Fail($"Unknown candidate kind for {candidate.Id}.");
                    break;
            }
        }
    }

    [TestMethod]
    public void SchemaAndSubstrateFailures_ShouldRemainInSeparateDomains()
    {
        var schemaCandidate = Candidate("A01");
        var schemaException = Assert.ThrowsExactly<SyntaxException>(() => ParseSchema(schemaCandidate.Query));
        var schemaDiagnostic = schemaException.ToDiagnostic(new SourceText(schemaCandidate.Query));

        Assert.AreEqual(DiagnosticCode.MQ4005_InvalidEndianness, schemaDiagnostic.Code);
        Assert.AreEqual(DiagnosticPhase.Schema, schemaDiagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Schema, schemaDiagnostic.SourceKind);

        var runtimeCandidate = Candidate("C01");
        var interpreter = CompileBinaryCandidate(runtimeCandidate);
        var runtimeException = InvokeBinaryFailure(interpreter, runtimeCandidate.Bytes!);

        Assert.AreEqual(ParseErrorCode.InsufficientData, runtimeException.ErrorCode);
        StringAssert.StartsWith(runtimeException.FormattedErrorCode, "ISE");
        Assert.DoesNotContain("MQ", runtimeException.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void BoundedAndProgressFailures_ShouldRetainFieldAndBoundaryContext()
    {
        var substreamCandidate = Candidate("G02");
        var substreamException = InvokeBinaryFailure(
            CompileBinaryCandidate(substreamCandidate),
            substreamCandidate.Bytes!);

        Assert.AreEqual(ParseErrorCode.ValidationFailed, substreamException.ErrorCode);
        Assert.AreEqual("Payload", substreamException.FieldName);
        StringAssert.Contains(substreamException.Details, "declared 3 bytes");

        var repetitionCandidate = Candidate("F03");
        var repetitionException = InvokeBinaryFailure(
            CompileBinaryCandidate(repetitionCandidate),
            repetitionCandidate.Bytes!);

        Assert.AreEqual(ParseErrorCode.MaxIterationsExceeded, repetitionException.ErrorCode);
        Assert.AreEqual("Items", repetitionException.FieldName);
        StringAssert.Contains(repetitionException.Details, "progress");
    }

    private static void AssertSchemaCandidate(RecoveryCandidate candidate)
    {
        _ = ParseSchema(candidate.SeedQuery);

        if (candidate.ExpectedMqCode is null)
        {
            _ = ParseSchema(candidate.Query);
            return;
        }

        var exception = Assert.ThrowsExactly<SyntaxException>(() => ParseSchema(candidate.Query), candidate.Id);
        Assert.AreEqual(candidate.ExpectedMqCode.Value, exception.Code, candidate.Id);
        Assert.IsTrue(exception.Span.HasValue, candidate.Id);
        var span = exception.Span!.Value;
        Assert.IsTrue(span.Start >= 0 && span.End <= candidate.Query.Length, candidate.Id);

        var diagnostic = exception.ToDiagnostic(new SourceText(candidate.Query));
        Assert.AreEqual(candidate.ExpectedMqCode.Value, diagnostic.Code, candidate.Id);
        Assert.AreEqual(DiagnosticPhase.Schema, diagnostic.Phase, candidate.Id);
        Assert.AreEqual(DiagnosticSourceKind.Schema, diagnostic.SourceKind, candidate.Id);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Message), candidate.Id);
    }

    private static void AssertBinaryRuntimeCandidate(RecoveryCandidate candidate)
    {
        var interpreter = CompileBinaryCandidate(candidate);
        _ = InvokeInterpret(interpreter, candidate.SeedBytes!);

        if (candidate.ExpectedRuntimeCode is null)
        {
            _ = InvokeInterpret(interpreter, candidate.Bytes!);
            return;
        }

        var exception = InvokeBinaryFailure(interpreter, candidate.Bytes!);
        Assert.AreEqual(candidate.ExpectedRuntimeCode.Value, exception.ErrorCode, candidate.Id);
        Assert.AreEqual(candidate.ExpectedRuntimeCode.Value.ToString(), ParseErrorName(exception), candidate.Id);
        if (!string.IsNullOrEmpty(candidate.ExpectedField))
            Assert.AreEqual(candidate.ExpectedField, exception.FieldName, candidate.Id);
        StringAssert.StartsWith(exception.FormattedErrorCode, "ISE", candidate.Id);
        Assert.IsFalse(exception.Message.Contains("MQ", StringComparison.Ordinal), candidate.Id);
    }

    private static void AssertTextRuntimeCandidate(RecoveryCandidate candidate)
    {
        var interpreter = CompileTextCandidate(candidate);
        _ = InvokeParse(interpreter, candidate.SeedText!);

        if (candidate.ExpectedRuntimeCode is null)
        {
            _ = InvokeParse(interpreter, candidate.Text!);
            return;
        }

        var exception = InvokeTextFailure(interpreter, candidate.Text!);
        Assert.AreEqual(candidate.ExpectedRuntimeCode.Value, exception.ErrorCode, candidate.Id);
        Assert.AreEqual(candidate.ExpectedRuntimeCode.Value.ToString(), ParseErrorName(exception), candidate.Id);
        if (!string.IsNullOrEmpty(candidate.ExpectedField))
            Assert.AreEqual(candidate.ExpectedField, exception.FieldName, candidate.Id);
        StringAssert.StartsWith(exception.FormattedErrorCode, "ISE", candidate.Id);
        Assert.IsFalse(exception.Message.Contains("MQ", StringComparison.Ordinal), candidate.Id);
    }

    private static object CompileBinaryCandidate(RecoveryCandidate candidate)
    {
        if (!string.IsNullOrEmpty(candidate.FixtureKey))
            return CompileInterpreter(CreateFixtureRegistry(candidate.FixtureKey), candidate.SchemaName);

        var schema = ParseSchema(candidate.SchemaText);
        Assert.IsInstanceOfType<BinarySchemaNode>(schema, candidate.Id);
        var registry = new SchemaRegistry();
        registry.Register(candidate.SchemaName, (BinarySchemaNode)schema);
        return CompileInterpreter(registry, candidate.SchemaName);
    }

    private static object CompileTextCandidate(RecoveryCandidate candidate)
    {
        if (!string.IsNullOrEmpty(candidate.FixtureKey))
            return CompileTextInterpreter(CreateTextFixtureRegistry(candidate.FixtureKey), candidate.SchemaName);

        var schema = ParseSchema(candidate.SchemaText);
        Assert.IsInstanceOfType<TextSchemaNode>(schema, candidate.Id);
        var registry = new SchemaRegistry();
        registry.Register(candidate.SchemaName, (TextSchemaNode)schema);
        return CompileTextInterpreter(registry, candidate.SchemaName);
    }

    private static SchemaRegistry CreateTextFixtureRegistry(string fixtureKey)
    {
        if (fixtureKey != "TEXT_ZERO_PROGRESS")
            throw new InvalidOperationException($"Unknown REC-114 text fixture '{fixtureKey}'.");

        var registry = new SchemaRegistry();
        registry.Register("Empty", new TextSchemaNode("Empty", []));
        registry.Register(
            "Container",
            new TextSchemaNode(
                "Container",
                [new TextFieldDefinitionNode("Items", TextFieldType.Repeat, "Empty")]));
        return registry;
    }

    private static object CompileTextInterpreter(SchemaRegistry registry, string schemaName)
    {
        var code = new InterpreterCodeGenerator(registry).GenerateAll();
        using var compilationUnit = new InterpreterCompilationUnit(
            $"REC114_{Guid.NewGuid():N}",
            code);

        Assert.IsTrue(compilationUnit.Compile(), string.Join(Environment.NewLine, compilationUnit.GetErrorMessages()));
        var interpreterType = compilationUnit.GetInterpreterType(schemaName);
        Assert.IsNotNull(interpreterType, schemaName);
        return Activator.CreateInstance(interpreterType)!;
    }

    private static SchemaRegistry CreateFixtureRegistry(string fixtureKey)
    {
        var registry = new SchemaRegistry();
        var byteType = new PrimitiveTypeNode(PrimitiveTypeName.Byte, Endianness.NotApplicable);

        switch (fixtureKey)
        {
            case "EXACT":
            case "LAX":
                registry.Register("Body", new BinarySchemaNode(
                    "Body",
                    [
                        new FieldDefinitionNode("A", byteType),
                        new FieldDefinitionNode("B", byteType)
                    ]));
                var mode = fixtureKey == "LAX" ? SubstreamMode.Lax : SubstreamMode.Exact;
                registry.Register("Packet", new BinarySchemaNode(
                    "Packet",
                    [
                        new FieldDefinitionNode("Length", byteType),
                        new FieldDefinitionNode(
                            "Payload",
                            new SubstreamTypeNode(
                                new IdentifierNode("Length"),
                                mode,
                                new SchemaReferenceTypeNode("Body"))),
                        new FieldDefinitionNode("Tail", byteType)
                    ]));
                return registry;
            case "RAW":
                registry.Register("Packet", new BinarySchemaNode(
                    "Packet",
                    [
                        new FieldDefinitionNode("Length", byteType),
                        new FieldDefinitionNode(
                            "Payload",
                            new SubstreamTypeNode(
                                new IdentifierNode("Length"),
                                SubstreamMode.Raw,
                                null)),
                        new FieldDefinitionNode("Tail", byteType)
                    ]));
                return registry;
            case "NEGATIVE":
                registry.Register("Packet", new BinarySchemaNode(
                    "Packet",
                    [
                        new FieldDefinitionNode(
                            "Length",
                            new PrimitiveTypeNode(PrimitiveTypeName.SByte, Endianness.NotApplicable)),
                        new FieldDefinitionNode(
                            "Payload",
                            new SubstreamTypeNode(
                                new IdentifierNode("Length"),
                                SubstreamMode.Raw,
                                null))
                    ]));
                return registry;
            default:
                throw new InvalidOperationException($"Unknown REC-114 fixture '{fixtureKey}'.");
        }
    }

    private static RuntimeParseException InvokeBinaryFailure(object interpreter, byte[] data)
    {
        var wrapper = Assert.ThrowsExactly<TargetInvocationException>(() => InvokeInterpret(interpreter, data));
        Assert.IsNotNull(wrapper.InnerException);
        Assert.IsInstanceOfType<RuntimeParseException>(wrapper.InnerException);
        return (RuntimeParseException)wrapper.InnerException!;
    }

    private static RuntimeParseException InvokeTextFailure(object interpreter, string data)
    {
        var wrapper = Assert.ThrowsExactly<TargetInvocationException>(() => InvokeParse(interpreter, data));
        Assert.IsNotNull(wrapper.InnerException);
        Assert.IsInstanceOfType<RuntimeParseException>(wrapper.InnerException);
        return (RuntimeParseException)wrapper.InnerException!;
    }

    private static object InvokeParse(object interpreter, string data)
    {
        var method = interpreter.GetType().GetMethod("Parse", [typeof(string)]);
        Assert.IsNotNull(method);
        return method.Invoke(interpreter, [data])!;
    }

    private static string ParseErrorName(RuntimeParseException exception)
    {
        return exception.ErrorCode.ToString();
    }

    private static Node ParseSchema(string schema)
    {
        return new SchemaParser(new Lexer(schema, true)).ParseSchema();
    }

    private static RecoveryCandidate Candidate(string id)
    {
        return CandidateCases.Single(candidate => candidate.Id == id);
    }

    private static RecoveryCandidate SchemaInvalid(
        string id,
        string family,
        string seed,
        string query,
        DiagnosticCode code,
        string messageToken,
        string rootCause,
        string authoritySection)
    {
        return new RecoveryCandidate(
            id,
            family,
            CandidateKind.Schema,
            seed,
            query,
            query,
            SchemaName(query),
            null,
            null,
            null,
            null,
            seed,
            query,
            code,
            null,
            messageToken,
            "",
            rootCause,
            authoritySection,
            "");
    }

    private static RecoveryCandidate SchemaValid(
        string id,
        string family,
        string schema,
        string rootCause,
        string authoritySection)
    {
        return new RecoveryCandidate(
            id,
            family,
            CandidateKind.Schema,
            schema,
            schema,
            schema,
            SchemaName(schema),
            null,
            null,
            null,
            null,
            schema,
            schema,
            null,
            null,
            "",
            "",
            rootCause,
            authoritySection,
            "");
    }

    private static RecoveryCandidate BinaryRuntime(
        string id,
        string family,
        string schema,
        string schemaName,
        byte[] seed,
        byte[] data,
        ParseErrorCode? code,
        string expectedField,
        string rootCause,
        string authoritySection)
    {
        var seedMarker = $"substrate:{Convert.ToHexString(seed)}";
        var dataMarker = $"substrate:{Convert.ToHexString(data)}";
        return new RecoveryCandidate(
            id,
            family,
            CandidateKind.BinaryRuntime,
            Describe(schema, seedMarker),
            Describe(schema, dataMarker),
            schema,
            schemaName,
            seed,
            data,
            null,
            null,
            seedMarker,
            dataMarker,
            null,
            code,
            "",
            expectedField,
            rootCause,
            authoritySection,
            "");
    }

    private static RecoveryCandidate BinaryRuntimeWithFixture(
        string id,
        string family,
        string schemaDescription,
        string schemaName,
        string fixtureKey,
        byte[] seed,
        byte[] data,
        ParseErrorCode? code,
        string expectedField,
        string rootCause)
    {
        var seedMarker = $"substrate:{Convert.ToHexString(seed)}";
        var dataMarker = $"substrate:{Convert.ToHexString(data)}";
        return new RecoveryCandidate(
            id,
            family,
            CandidateKind.BinaryRuntime,
            Describe(schemaDescription, seedMarker),
            Describe(schemaDescription, dataMarker),
            schemaDescription,
            schemaName,
            seed,
            data,
            null,
            null,
            seedMarker,
            dataMarker,
            null,
            code,
            "",
            expectedField,
            rootCause,
            "4.13 Substream (Length-Bounded) Payloads",
            fixtureKey);
    }

    private static RecoveryCandidate TextRuntime(
        string id,
        string family,
        string schema,
        string schemaName,
        string seed,
        string data,
        ParseErrorCode? code,
        string expectedField,
        string rootCause,
        string authoritySection)
    {
        var seedMarker = $"substrate:{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(seed))}";
        var dataMarker = $"substrate:{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(data))}";
        return new RecoveryCandidate(
            id,
            family,
            CandidateKind.TextRuntime,
            Describe(schema, seedMarker),
            Describe(schema, dataMarker),
            schema,
            schemaName,
            null,
            null,
            seed,
            data,
            seedMarker,
            dataMarker,
            null,
            code,
            "",
            expectedField,
            rootCause,
            authoritySection,
            "");
    }

    private static RecoveryCandidate TextRuntimeWithFixture(
        string id,
        string family,
        string schemaDescription,
        string schemaName,
        string fixtureKey,
        string seed,
        string data,
        ParseErrorCode? code,
        string expectedField,
        string rootCause,
        string authoritySection)
    {
        var seedMarker = $"substrate:{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(seed))}";
        var dataMarker = $"substrate:{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(data))}";
        return new RecoveryCandidate(
            id,
            family,
            CandidateKind.TextRuntime,
            Describe(schemaDescription, seedMarker),
            Describe(schemaDescription, dataMarker),
            schemaDescription,
            schemaName,
            null,
            null,
            seed,
            data,
            seedMarker,
            dataMarker,
            null,
            code,
            "",
            expectedField,
            rootCause,
            authoritySection,
            fixtureKey);
    }

    private static string Describe(string schema, string marker)
    {
        return $"{schema}\n-- {marker}";
    }

    private static string SchemaName(string schema)
    {
        var tokens = schema.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var index = Array.FindIndex(tokens, static token => token is "binary" or "text");
        return index >= 0 && index + 1 < tokens.Length
            ? tokens[index + 1]
            : "Schema";
    }

    private enum CandidateKind
    {
        Schema,
        BinaryRuntime,
        TextRuntime
    }

    private sealed record RecoveryCandidate(
        string Id,
        string Family,
        CandidateKind Kind,
        string SeedQuery,
        string Query,
        string SchemaText,
        string SchemaName,
        byte[]? SeedBytes,
        byte[]? Bytes,
        string? SeedText,
        string? Text,
        string MutationBefore,
        string MutationAfter,
        DiagnosticCode? ExpectedMqCode,
        ParseErrorCode? ExpectedRuntimeCode,
        string MessageToken,
        string ExpectedField,
        string RootCause,
        string AuthoritySection,
        string FixtureKey)
    {
        public bool IsInvalid => ExpectedMqCode is not null || ExpectedRuntimeCode is not null;
    }
}
