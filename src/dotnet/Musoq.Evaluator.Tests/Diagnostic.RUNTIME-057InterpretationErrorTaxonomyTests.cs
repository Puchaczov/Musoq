using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticRuntime057InterpretationErrorTaxonomyTests : BinaryOrTextualEvaluatorTestBase
{
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
        var parsedFields = (Dictionary<string, object?>)table[0][0]!;
        Assert.AreEqual((byte)7, parsedFields["Prefix"]);
        Assert.IsFalse(parsedFields.ContainsKey("Value"));
        Assert.AreEqual("Value", table[0][1]);
        StringAssert.Contains((string)table[0][2]!, "ISE0010");
        StringAssert.Contains((string)table[0][2]!, "Encoded.Value");
        Assert.DoesNotContain("C3", (string)table[0][2]!, StringComparison.OrdinalIgnoreCase);
        Assert.AreEqual(1, table[0][3]);
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
        Assert.AreEqual("invalid.txt", safeTable[0][0]);
        Assert.IsNull(safeTable[0][1]);
    }

    [TestMethod]
    public void AppendixC_ErrorCodes_ShouldRetainStableIseEnvelopeShape()
    {
        var expectedCodes = new[]
        {
            (ParseErrorCode.InsufficientData, "ISE0001"),
            (ParseErrorCode.ValidationFailed, "ISE0002"),
            (ParseErrorCode.PatternMismatch, "ISE0003"),
            (ParseErrorCode.LiteralMismatch, "ISE0004"),
            (ParseErrorCode.DelimiterNotFound, "ISE0005"),
            (ParseErrorCode.ExpectedDelimiter, "ISE0006"),
            (ParseErrorCode.InvalidSize, "ISE0007"),
            (ParseErrorCode.InvalidPosition, "ISE0008"),
            (ParseErrorCode.MaxIterationsExceeded, "ISE0009"),
            (ParseErrorCode.EncodingError, "ISE0010"),
            (ParseErrorCode.ExpectedWhitespace, "ISE0011"),
            (ParseErrorCode.NoAlternativeMatched, "ISE0012"),
            (ParseErrorCode.InvalidSchemaReference, "ISE0013"),
            (ParseErrorCode.FieldReferenceError, "ISE0014"),
            (ParseErrorCode.GeneralError, "ISE0015")
        };

        foreach (var (code, expected) in expectedCodes)
        {
            var exception = new ParseException(code, "RuntimeSchema", "Payload.Value", 17, "safe diagnostic detail");

            Assert.AreEqual(expected, exception.FormattedErrorCode);
            Assert.AreEqual(code, exception.ErrorCode);
            Assert.AreEqual("RuntimeSchema", exception.SchemaName);
            Assert.AreEqual("Payload.Value", exception.FieldName);
            Assert.AreEqual(17, exception.Position);
            Assert.AreEqual("safe diagnostic detail", exception.Details);
            StringAssert.Contains(exception.Message, expected);
            StringAssert.Contains(exception.Message, "RuntimeSchema.Payload.Value");
            StringAssert.Contains(exception.Message, "safe diagnostic detail");
        }
    }
}
