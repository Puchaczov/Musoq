using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticRuntime057InterpretationErrorTaxonomyTests : BinaryOrTextualEvaluatorTestBase
{
    [TestMethod]
    public void TextInterpretation_ExpectedWhitespace_ShouldExposeMq7111InputDiagnostic()
    {
        const string query = @"
            text Spaced { Prefix: chars[2], _: whitespace+, Value: rest };
            select r.Value
            from #test.lines() f
            cross apply Parse<Spaced>(f.Text) r";

        var compiled = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            new TextSchemaProvider(new Dictionary<string, IEnumerable<TextEntity>>
            {
                ["#test"] = [new TextEntity { Name = "private-name.txt", Text = "ab-secret" }]
            }),
            LoggerResolver,
            TestCompilationOptions);

        var exception = Assert.ThrowsExactly<ParseException>(() => _ = compiled.Run(CancellationToken.None).Count);
        var envelope = MusoqErrorEnvelope.FromException(exception);

        Assert.AreEqual(ParseErrorCode.ExpectedWhitespace, exception.ErrorCode);
        Assert.AreEqual(DiagnosticCode.MQ7111_InterpretationExpectedWhitespace, envelope.Code);
        Assert.AreEqual("ISE0011", envelope.Arguments["interpretationCode"]);
        Assert.AreEqual(2L, envelope.InputLocation?.CharacterOffset);
        Assert.DoesNotContain("ab-secret", MusoqErrorEnvelopeFormatter.FormatJson(envelope));
    }

    [TestMethod]
    public void BinaryInterpretation_InsufficientData_ShouldExposeMq7101InputDiagnostic()
    {
        const string query = @"
            binary Header { Prefix: byte, Value: int le };
            select r.Value
            from #test.files() f
            cross apply Interpret<Header>(f.Content) r";

        var compiled = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            new BinarySchemaProvider(new Dictionary<string, IEnumerable<BinaryEntity>>
            {
                ["#test"] = [new BinaryEntity { Name = "private-name.bin", Content = [0x07] }]
            }),
            LoggerResolver,
            TestCompilationOptions);

        var exception = Assert.ThrowsExactly<ParseException>(() => _ = compiled.Run(CancellationToken.None).Count);
        var envelope = MusoqErrorEnvelope.FromException(exception);

        Assert.AreEqual(ParseErrorCode.InsufficientData, exception.ErrorCode);
        Assert.AreEqual(DiagnosticCode.MQ7101_InterpretationInsufficientData, envelope.Code);
        Assert.AreEqual("ISE0001", envelope.Arguments["interpretationCode"]);
        Assert.IsNotNull(envelope.InputLocation?.ByteOffset);
        Assert.IsNull(envelope.InputLocation?.CharacterOffset);
        Assert.IsNull(envelope.Offset);
        Assert.DoesNotContain("private-name.bin", MusoqErrorEnvelopeFormatter.FormatJson(envelope));
    }

    [TestMethod]
    public void BinaryInterpretation_InvalidEncoding_ShouldExposeStableIse0010Envelope()
    {
        const string query = @"
            binary Encoded { Prefix: byte, Value: string[2] utf8, Tail: byte };
            select r.Value
            from #test.files() f
            cross apply Interpret<Encoded>(f.Content) r";

        var compiled = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            new BinarySchemaProvider(new Dictionary<string, IEnumerable<BinaryEntity>>
            {
                ["#test"] = [new BinaryEntity { Name = "invalid-utf8.bin", Content = [0x07, 0xC3, 0x28, 0xA5] }]
            }),
            LoggerResolver,
            TestCompilationOptions);

        var exception = Assert.ThrowsExactly<ParseException>(() =>
        {
            var table = compiled.Run(CancellationToken.None);
            _ = table.Count;
        });

        Assert.AreEqual(ParseErrorCode.EncodingError, exception.ErrorCode);
        Assert.AreEqual("ISE0010", exception.FormattedErrorCode);
        Assert.AreEqual("Encoded", exception.SchemaName);
        Assert.AreEqual("Value", exception.FieldName);
        Assert.AreEqual(1, exception.Position);
        StringAssert.Contains(exception.Details, "utf-8");
        Assert.DoesNotContain("C3", exception.Details, StringComparison.OrdinalIgnoreCase);
    }

    [TestMethod]
    public void BinaryInterpretation_InvalidNullTerminatedEncoding_ShouldUseSameEnvelope()
    {
        const string query = @"
            binary Encoded { Prefix: byte, Value: string[3] utf8 nullterm, Tail: byte };
            select r.Value
            from #test.files() f
            cross apply Interpret<Encoded>(f.Content) r";

        var compiled = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            new BinarySchemaProvider(new Dictionary<string, IEnumerable<BinaryEntity>>
            {
                ["#test"] = [new BinaryEntity { Name = "invalid-nullterm.bin", Content = [0x07, 0xC3, 0x28, 0x00, 0xA5] }]
            }),
            LoggerResolver,
            TestCompilationOptions);

        var exception = Assert.ThrowsExactly<ParseException>(() =>
        {
            var table = compiled.Run(CancellationToken.None);
            _ = table.Count;
        });

        Assert.AreEqual(ParseErrorCode.EncodingError, exception.ErrorCode);
        Assert.AreEqual("ISE0010", exception.FormattedErrorCode);
        Assert.AreEqual("Encoded", exception.SchemaName);
        Assert.AreEqual("Value", exception.FieldName);
        Assert.AreEqual(1, exception.Position);
    }

    [TestMethod]
    public void PartialInterpret_InvalidEncoding_ShouldRetainSuccessfulFieldsAndSafeError()
    {
        const string query = @"
            binary Encoded { Prefix: byte, Value: string[2] utf8, Tail: byte };
            select p.ParsedFields, p.ErrorField, p.ErrorMessage, p.BytesConsumed
            from #test.files() f
            cross apply PartialInterpret<Encoded>(f.Content) p";

        var compiled = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            new BinarySchemaProvider(new Dictionary<string, IEnumerable<BinaryEntity>>
            {
                ["#test"] = [new BinaryEntity { Name = "invalid-utf8.bin", Content = [0x07, 0xC3, 0x28, 0xA5] }]
            }),
            LoggerResolver,
            TestCompilationOptions);

        var table = compiled.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        var parsedFields = PartialPayloadAssertions.CompletedValues(table[0].ReadValue(0)!);
        Assert.AreEqual((byte)7, parsedFields["Prefix"]);
        Assert.IsFalse(parsedFields.ContainsKey("Value"));
        Assert.AreEqual("Value", table[0].ReadValue(1));
        StringAssert.Contains((string)table[0].ReadValue(2)!, "ISE0010");
        StringAssert.Contains((string)table[0].ReadValue(2)!, "Encoded.Value");
        Assert.DoesNotContain("C3", (string)table[0].ReadValue(2)!, StringComparison.OrdinalIgnoreCase);
        Assert.AreEqual(1, table[0].ReadValue(3));
    }

    [TestMethod]
    public void Parse_InvalidText_ShouldExposeStablePatternEnvelopeAndTryParseShouldReturnNull()
    {
        const string query = @"
            text Digits { Prefix: chars[2], Value: pattern '\d+' };
            select r.Prefix, r.Value
            from #test.lines() f
            cross apply Parse<Digits>(f.Text) r";

        var compiled = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            new TextSchemaProvider(new Dictionary<string, IEnumerable<TextEntity>>
            {
                ["#test"] = [new TextEntity { Name = "invalid.txt", Text = "xxabc" }]
            }),
            LoggerResolver,
            TestCompilationOptions);

        var exception = Assert.ThrowsExactly<ParseException>(() =>
        {
            var table = compiled.Run(CancellationToken.None);
            _ = table.Count;
        });

        Assert.AreEqual(ParseErrorCode.PatternMismatch, exception.ErrorCode);
        Assert.AreEqual("ISE0003", exception.FormattedErrorCode);
        Assert.AreEqual("Digits", exception.SchemaName);
        Assert.AreEqual("Value", exception.FieldName);
        Assert.AreEqual(2, exception.Position);

        const string safeQuery = @"
            text Digits { Value: pattern '\d+' };
            select f.Name, r.Value
            from #test.lines() f
            outer apply TryParse<Digits>(f.Text) r";

        var safeCompiled = CompileGeneratedQuery(
            safeQuery,
            Guid.NewGuid().ToString(),
            new TextSchemaProvider(new Dictionary<string, IEnumerable<TextEntity>>
            {
                ["#test"] = [new TextEntity { Name = "invalid.txt", Text = "abc" }]
            }),
            LoggerResolver,
            TestCompilationOptions);

        var safeTable = safeCompiled.Run(CancellationToken.None);
        Assert.AreEqual(1, safeTable.Count);
        Assert.AreEqual("invalid.txt", safeTable[0].ReadValue(0));
        Assert.IsNull(safeTable[0].ReadValue(1));
    }

    [TestMethod]
    [DataRow(ParseErrorCode.InsufficientData, "ISE0001", DiagnosticCode.MQ7101_InterpretationInsufficientData)]
    [DataRow(ParseErrorCode.ValidationFailed, "ISE0002", DiagnosticCode.MQ7102_InterpretationValidationFailed)]
    [DataRow(ParseErrorCode.PatternMismatch, "ISE0003", DiagnosticCode.MQ7103_InterpretationPatternMismatch)]
    [DataRow(ParseErrorCode.LiteralMismatch, "ISE0004", DiagnosticCode.MQ7104_InterpretationLiteralMismatch)]
    [DataRow(ParseErrorCode.DelimiterNotFound, "ISE0005", DiagnosticCode.MQ7105_InterpretationDelimiterNotFound)]
    [DataRow(ParseErrorCode.ExpectedDelimiter, "ISE0006", DiagnosticCode.MQ7106_InterpretationExpectedDelimiter)]
    [DataRow(ParseErrorCode.InvalidSize, "ISE0007", DiagnosticCode.MQ7107_InterpretationInvalidSize)]
    [DataRow(ParseErrorCode.InvalidPosition, "ISE0008", DiagnosticCode.MQ7108_InterpretationInvalidPosition)]
    [DataRow(ParseErrorCode.MaxIterationsExceeded, "ISE0009", DiagnosticCode.MQ7109_InterpretationMaxIterationsExceeded)]
    [DataRow(ParseErrorCode.EncodingError, "ISE0010", DiagnosticCode.MQ7110_InterpretationEncodingError)]
    [DataRow(ParseErrorCode.ExpectedWhitespace, "ISE0011", DiagnosticCode.MQ7111_InterpretationExpectedWhitespace)]
    [DataRow(ParseErrorCode.NoAlternativeMatched, "ISE0012", DiagnosticCode.MQ7112_InterpretationNoAlternativeMatched)]
    [DataRow(ParseErrorCode.InvalidSchemaReference, "ISE0013", DiagnosticCode.MQ7113_InterpretationInvalidSchemaReference)]
    [DataRow(ParseErrorCode.FieldReferenceError, "ISE0014", DiagnosticCode.MQ7114_InterpretationFieldReferenceError)]
    [DataRow(ParseErrorCode.GeneralError, "ISE0015", DiagnosticCode.MQ7115_InterpretationGeneralError)]
    public void AppendixC_ErrorCodes_ShouldRetainStableIseEnvelopeShape(
        ParseErrorCode code,
        string expected,
        DiagnosticCode expectedDiagnosticCode)
    {
        var exception = new ParseException(code, "RuntimeSchema", "Payload.Value", 17, "safe diagnostic detail");
        var diagnostic = exception.ToDiagnostic();

        Assert.AreEqual(expected, exception.FormattedErrorCode);
        Assert.AreEqual(code, exception.ErrorCode);
        Assert.AreEqual(expectedDiagnosticCode, diagnostic.Code);
        Assert.AreEqual(expected, diagnostic.Arguments["interpretationCode"]);
        Assert.AreEqual("RuntimeSchema", exception.SchemaName);
        Assert.AreEqual("Payload.Value", exception.FieldName);
        Assert.AreEqual(17, exception.Position);
        Assert.AreEqual("safe diagnostic detail", exception.Details);
        StringAssert.Contains(exception.GetPartialInterpretationMessage(), expected);
        StringAssert.Contains(exception.GetPartialInterpretationMessage(), "RuntimeSchema.Payload.Value");
        StringAssert.Contains(exception.GetPartialInterpretationMessage(), "at position 17");
        Assert.DoesNotContain("safe diagnostic detail", exception.GetPartialInterpretationMessage());
        Assert.DoesNotContain("safe diagnostic detail", exception.Message);
    }
}
