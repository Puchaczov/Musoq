using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Schema;
using Musoq.Schema.Interpreters;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

/// <summary>
/// Permanent REC-131 matrix for strict, tolerant and partial interpretation.
/// The matrix keeps substrate parse failures separate from row-retention policy,
/// provider/runtime boundaries and partial-result metadata.
/// </summary>
[TestClass]
public sealed class REC131StrictTolerantPartialInterpretationTests : BinaryOrTextualEvaluatorTestBase
{
    [TestMethod]
    [DataRow("ST-01")]
    [DataRow("ST-02")]
    [DataRow("ST-03")]
    [DataRow("ST-04")]
    [DataRow("ST-05")]
    [DataRow("ST-06")]
    [DataRow("ST-07")]
    [DataRow("ST-08")]
    [DataRow("ST-09")]
    [DataRow("ST-10")]
    [DataRow("ST-11")]
    [DataRow("ST-12")]
    public void StrictAndTolerantMatrix_ShouldPreserveFailurePolicy(string caseId)
    {
        switch (caseId)
        {
            case "ST-01":
                AssertStrictBinaryFailure();
                return;
            case "ST-02":
                AssertStrictBinaryValidationFailure();
                return;
            case "ST-03":
                AssertStrictTextFailure();
                return;
            case "ST-04":
                AssertStrictTextLiteralFailure();
                return;
            case "ST-05":
                AssertTryBinary("cross", [0x2A]);
                return;
            case "ST-06":
                AssertTryBinary("outer", [0x2A]);
                return;
            case "ST-07":
                AssertTryText("cross", "bad");
                return;
            case "ST-08":
                AssertTryText("outer", "bad");
                return;
            case "ST-09":
                AssertPartialBinary(
                    "binary Packet { Prefix: byte, Value: int le }",
                    [0x07, 0x2A, 0x00, 0x00, 0x00],
                    [0x07, 0x2A],
                    ["Prefix"],
                    "Value",
                    "ISE0001",
                    1,
                    1);
                return;
            case "ST-10":
                AssertPartialBinary(
                    "binary Packet { Prefix: byte, Value: int le }",
                    [0x07, 0x2A, 0x00, 0x00, 0x00],
                    [],
                    [],
                    "Prefix",
                    "ISE0001",
                    0,
                    0);
                return;
            case "ST-11":
                AssertPartialText(
                    "text Pair { Key: until '=', Value: chars[3] }",
                    "host=abc",
                    "host=ab",
                    ["Key"],
                    "Value",
                    "ISE0001",
                    5,
                    5);
                return;
            case "ST-12":
                AssertPartialText(
                    "text Pair { Key: until '=', Value: chars[3] }",
                    "host=abc",
                    "missing-delimiter",
                    [],
                    "Key",
                    "ISE0005",
                    0,
                    0);
                return;
            default:
                Assert.Fail($"Unknown strict/tolerant case '{caseId}'.");
                return;
        }
    }

    [TestMethod]
    [DataRow("RA-01")]
    [DataRow("RA-02")]
    [DataRow("RA-03")]
    [DataRow("RA-04")]
    [DataRow("RA-05")]
    [DataRow("RA-06")]
    [DataRow("RA-07")]
    [DataRow("RA-08")]
    [DataRow("RA-09")]
    [DataRow("RA-10")]
    [DataRow("RA-11")]
    [DataRow("RA-12")]
    public void ApplyNullRetentionMatrix_ShouldKeepCrossAndOuterDistinct(string caseId)
    {
        switch (caseId)
        {
            case "RA-01":
                AssertTryBinary("cross", [0x2A]);
                return;
            case "RA-02":
                AssertTryBinary("outer", [0x2A]);
                return;
            case "RA-03":
                AssertTryBinary("cross", []);
                return;
            case "RA-04":
                AssertTryBinary("outer", []);
                return;
            case "RA-05":
                AssertTryBinary("cross", [0x00], validation: true);
                return;
            case "RA-06":
                AssertTryBinary("outer", [0x00], validation: true);
                return;
            case "RA-07":
                AssertTryText("cross", "bad");
                return;
            case "RA-08":
                AssertTryText("outer", "bad");
                return;
            case "RA-09":
                AssertTryText("cross", string.Empty);
                return;
            case "RA-10":
                AssertTryText("outer", string.Empty);
                return;
            case "RA-11":
                AssertTryTextPattern("cross");
                return;
            case "RA-12":
                AssertTryTextPattern("outer");
                return;
            default:
                Assert.Fail($"Unknown row-retention case '{caseId}'.");
                return;
        }
    }

    [TestMethod]
    [DataRow("EB-01")]
    [DataRow("EB-02")]
    [DataRow("EB-03")]
    [DataRow("EB-04")]
    [DataRow("EB-05")]
    [DataRow("EB-06")]
    [DataRow("EB-07")]
    [DataRow("EB-08")]
    [DataRow("EB-09")]
    [DataRow("EB-10")]
    [DataRow("EB-11")]
    [DataRow("EB-12")]
    public void FailureBoundaryMatrix_ShouldKeepUnrelatedFailuresDistinct(string caseId)
    {
        switch (caseId)
        {
            case "EB-01":
                AssertUndefinedTextSchemaReference();
                return;
            case "EB-02":
                AssertForwardTextSchemaReference();
                return;
            case "EB-03":
                AssertCircularTextSchemaReference();
                return;
            case "EB-04":
                AssertSchemaFailure("binary Broken { Value: byte, Value: byte }; select 1 from #test.files();",
                    DiagnosticCode.MQ4008_DuplicateSchemaField);
                return;
            case "EB-05":
            case "EB-06":
            case "EB-07":
                AssertProviderFailure();
                return;
            case "EB-08":
            case "EB-09":
            case "EB-10":
                AssertCancellation();
                return;
            case "EB-11":
                AssertInternalInvariant("select Population / (Population - Population) from #A.Entities()");
                return;
            case "EB-12":
                AssertInternalInvariant("select Population + Money from #A.Entities()", overflow: true);
                return;
            default:
                Assert.Fail($"Unknown failure-boundary case '{caseId}'.");
                return;
        }
    }

    [TestMethod]
    [DataRow("PF-01")]
    [DataRow("PF-02")]
    [DataRow("PF-03")]
    [DataRow("PF-04")]
    [DataRow("PF-05")]
    [DataRow("PF-06")]
    [DataRow("PF-07")]
    [DataRow("PF-08")]
    [DataRow("PF-09")]
    [DataRow("PF-10")]
    [DataRow("PF-11")]
    [DataRow("PF-12")]
    public void PartialMetadataMatrix_ShouldKeepFieldsAndPositionsTruthful(string caseId)
    {
        switch (caseId)
        {
            case "PF-01":
                AssertPartialBinary(
                    "binary Packet { Prefix: byte, Value: int le }",
                    [0x07, 0x2A, 0x00, 0x00, 0x00],
                    [0x07, 0x2A],
                    ["Prefix"],
                    "Value",
                    "ISE0001",
                    1,
                    1);
                return;
            case "PF-02":
                AssertPartialBinary(
                    "binary Packet { Value: int le }",
                    [0x2A, 0x00, 0x00, 0x00],
                    [0x2A, 0x00],
                    [],
                    "Value",
                    "ISE0001",
                    0,
                    0);
                return;
            case "PF-03":
                AssertPartialBinary(
                    "binary Packet { Value: byte check Value = 0x2A }",
                    [0x2A],
                    [0x01],
                    [],
                    "Value",
                    "ISE0002",
                    1,
                    1);
                return;
            case "PF-04":
                AssertPartialBinary(
                    "binary Packet { Prefix: byte, Value: int le }",
                    [0x07, 0x2A, 0x00, 0x00, 0x00],
                    [0x07, 0x2A, 0x00, 0x00, 0x00],
                    ["Prefix", "Value"],
                    null,
                    null,
                    5,
                    5);
                return;
            case "PF-05":
                AssertPartialText(
                    "text Pair { Key: until '=', Value: chars[3] }",
                    "host=abc",
                    "host=ab",
                    ["Key"],
                    "Value",
                    "ISE0001",
                    5,
                    5);
                return;
            case "PF-06":
                AssertPartialText(
                    "text Pair { Prefix: chars[2], Value: pattern '\\d+' }",
                    "xx12",
                    "xxab",
                    ["Prefix"],
                    "Value",
                    "ISE0003",
                    2,
                    2);
                return;
            case "PF-07":
                AssertPartialBinary(
                    "binary Child { Value: int le }; binary Root { Prefix: byte, Payload: Child }",
                    [0x07, 0x2A, 0x00, 0x00, 0x00],
                    [0x07, 0x2A],
                    ["Prefix"],
                    "Payload.Value",
                    "ISE0001",
                    1,
                    1);
                return;
            case "PF-08":
                AssertPartialText(
                    "text Child { Value: chars[3] }; text Root { Prefix: chars[2], Payload: Child }",
                    "xxabc",
                    "xxab",
                    ["Prefix"],
                    "Payload.Value",
                    "ISE0001",
                    2,
                    2);
                return;
            case "PF-09":
                AssertPartialBinary(
                    "binary Packet { Prefix: byte, Value: byte check Value = 0x2A }",
                    [0x07, 0x2A],
                    [0x07, 0x01],
                    ["Prefix"],
                    "Value",
                    "ISE0002",
                    2,
                    2);
                return;
            case "PF-10":
                AssertPartialBinary(
                    "binary Packet { Prefix: byte, Value: string[2] utf8, Tail: byte }",
                    [0x07, 0x41, 0x42, 0xA5],
                    [0x07, 0xC3, 0x28, 0xA5],
                    ["Prefix"],
                    "Value",
                    "ISE0010",
                    1,
                    1);
                return;
            case "PF-11":
                AssertPartialBinary(
                    "binary Packet { Values: byte[0], Tail: byte }",
                    [0x5A],
                    [0x5A],
                    ["Values", "Tail"],
                    null,
                    null,
                    1,
                    1);
                return;
            case "PF-12":
                AssertPartialText(
                    "text Pair { Value: chars[1] }",
                    "a",
                    string.Empty,
                    [],
                    "Value",
                    "ISE0001",
                    0,
                    0);
                return;
            default:
                Assert.Fail($"Unknown partial-metadata case '{caseId}'.");
                return;
        }
    }

    private void AssertStrictBinaryFailure()
    {
        const string query = @"
            binary Packet { Prefix: byte, Value: int le, Tail: byte };
            select f.Name, p.Value
            from #test.files() f
            cross apply Interpret<Packet>(f.Content) p
            order by f.Name";

        var exception = Assert.ThrowsExactly<ParseException>(() =>
        {
            var table = RunBinaryQuery(
                query,
                new BinaryEntity { Name = "01-valid.bin", Content = [0x07, 0x2A, 0x00, 0x00, 0x00, 0xA5] },
                new BinaryEntity { Name = "02-bad.bin", Content = [0x07, 0x2A] },
                new BinaryEntity { Name = "03-valid.bin", Content = [0x07, 0x2A, 0x00, 0x00, 0x00, 0xA5] });
            _ = table.Count;
        });

        Assert.AreEqual(ParseErrorCode.InsufficientData, exception.ErrorCode);
        Assert.AreEqual("Value", exception.FieldName);
        Assert.AreEqual(1, exception.Position);
        Assert.IsFalse(exception.Message.Contains("02-bad.bin", StringComparison.Ordinal));
    }

    private void AssertStrictBinaryValidationFailure()
    {
        const string query = @"
            binary Packet { Value: byte check Value = 0x2A };
            select f.Name, p.Value
            from #test.files() f
            cross apply Interpret<Packet>(f.Content) p
            order by f.Name";

        var exception = Assert.ThrowsExactly<ParseException>(() =>
        {
            var table = RunBinaryQuery(
                query,
                new BinaryEntity { Name = "01-valid.bin", Content = [0x2A] },
                new BinaryEntity { Name = "02-bad.bin", Content = [0x01] },
                new BinaryEntity { Name = "03-valid.bin", Content = [0x2A] });
            _ = table.Count;
        });

        Assert.AreEqual(ParseErrorCode.ValidationFailed, exception.ErrorCode);
        Assert.AreEqual("Value", exception.FieldName);
        Assert.AreEqual(1, exception.Position);
        Assert.IsFalse(exception.Message.Contains("02-bad.bin", StringComparison.Ordinal));
    }

    private void AssertStrictTextFailure()
    {
        const string query = @"
            text Pair { Key: until '=', Value: rest };
            select f.Name, p.Key
            from #test.lines() f
            cross apply Parse<Pair>(f.Text) p
            order by f.Name";

        var exception = Assert.ThrowsExactly<ParseException>(() =>
        {
            var table = RunTextQuery(
                query,
                new TextEntity { Name = "01-valid.txt", Text = "host=ok" },
                new TextEntity { Name = "02-bad.txt", Text = "missing-delimiter" },
                new TextEntity { Name = "03-valid.txt", Text = "host=ok" });
            _ = table.Count;
        });

        Assert.AreEqual(ParseErrorCode.DelimiterNotFound, exception.ErrorCode);
        Assert.AreEqual("Key", exception.FieldName);
        Assert.AreEqual(0, exception.Position);
        Assert.IsFalse(exception.Message.Contains("02-bad.txt", StringComparison.Ordinal));
    }

    private void AssertStrictTextLiteralFailure()
    {
        const string query = @"
            text Status { Prefix: literal 'OK', Tail: rest };
            select f.Name, p.Tail
            from #test.lines() f
            cross apply Parse<Status>(f.Text) p
            order by f.Name";

        var exception = Assert.ThrowsExactly<ParseException>(() =>
        {
            var table = RunTextQuery(
                query,
                new TextEntity { Name = "01-valid.txt", Text = "OK-ready" },
                new TextEntity { Name = "02-bad.txt", Text = "NO-ready" },
                new TextEntity { Name = "03-valid.txt", Text = "OK-ready" });
            _ = table.Count;
        });

        Assert.AreEqual(ParseErrorCode.LiteralMismatch, exception.ErrorCode);
        Assert.AreEqual("Prefix", exception.FieldName);
        Assert.AreEqual(0, exception.Position);
        Assert.IsFalse(exception.Message.Contains("02-bad.txt", StringComparison.Ordinal));
    }

    private void AssertTryBinary(string applyKind, byte[] failingData, bool validation = false)
    {
        var schema = validation
            ? "binary Packet { Value: byte check Value = 0x2A }"
            : "binary Packet { Value: int le }";
        byte[] validData = validation ? [0x2A] : [0x2A, 0x00, 0x00, 0x00];
        var query = $@"
            {schema};
            select f.Name, p.Value
            from #test.files() f
            {applyKind} apply TryInterpret<Packet>(f.Content) p
            order by f.Name";

        var table = RunBinaryQuery(
            query,
            new BinaryEntity { Name = "01-valid.bin", Content = validData },
            new BinaryEntity { Name = "02-bad.bin", Content = failingData },
            new BinaryEntity { Name = "03-valid.bin", Content = validData });

        Assert.AreEqual(applyKind == "cross" ? 2 : 3, table.Count);
        Assert.AreEqual("01-valid.bin", table[0][0]);
        object expectedValue = validation ? (object)(byte)0x2A : 42;
        if (applyKind == "cross")
        {
            Assert.AreEqual("03-valid.bin", table[1][0]);
            Assert.AreEqual(expectedValue, table[0][1]);
            Assert.AreEqual(expectedValue, table[1][1]);
            return;
        }

        Assert.AreEqual("02-bad.bin", table[1][0]);
        Assert.IsNull(table[1][1]);
        Assert.AreEqual("03-valid.bin", table[2][0]);
        Assert.AreEqual(expectedValue, table[2][1]);
    }

    private void AssertTryText(string applyKind, string failingText)
    {
        const string query = @"
            text Pair { Key: until '=', Value: rest };
            select f.Name, p.Key, p.Value
            from #test.lines() f
            APPLY_PLACEHOLDER TryParse<Pair>(f.Text) p
            order by f.Name";
        var actualQuery = query.Replace("APPLY_PLACEHOLDER", $"{applyKind} apply", StringComparison.Ordinal);

        var table = RunTextQuery(
            actualQuery,
            new TextEntity { Name = "01-valid.txt", Text = "host=ok" },
            new TextEntity { Name = "02-bad.txt", Text = failingText },
            new TextEntity { Name = "03-valid.txt", Text = "host=ok" });

        Assert.AreEqual(applyKind == "cross" ? 2 : 3, table.Count);
        Assert.AreEqual("01-valid.txt", table[0][0]);
        if (applyKind == "cross")
        {
            Assert.AreEqual("03-valid.txt", table[1][0]);
            Assert.AreEqual("host", table[0][1]);
            Assert.AreEqual("ok", table[0][2]);
            return;
        }

        Assert.AreEqual("02-bad.txt", table[1][0]);
        Assert.IsNull(table[1][1]);
        Assert.IsNull(table[1][2]);
        Assert.AreEqual("03-valid.txt", table[2][0]);
    }

    private void AssertTryTextPattern(string applyKind)
    {
        const string query = @"
            text Digits { Value: pattern '\d+' };
            select f.Name, p.Value
            from #test.lines() f
            APPLY_PLACEHOLDER TryParse<Digits>(f.Text) p
            order by f.Name";
        var actualQuery = query.Replace("APPLY_PLACEHOLDER", $"{applyKind} apply", StringComparison.Ordinal);
        var table = RunTextQuery(
            actualQuery,
            new TextEntity { Name = "01-valid.txt", Text = "123" },
            new TextEntity { Name = "02-bad.txt", Text = "abc" },
            new TextEntity { Name = "03-valid.txt", Text = "456" });

        Assert.AreEqual(applyKind == "cross" ? 2 : 3, table.Count);
        Assert.AreEqual("01-valid.txt", table[0][0]);
        Assert.AreEqual("03-valid.txt", table[applyKind == "cross" ? 1 : 2][0]);
        if (applyKind == "outer")
        {
            Assert.AreEqual("02-bad.txt", table[1][0]);
            Assert.IsNull(table[1][1]);
        }
    }

    private void AssertPartialBinary(
        string schema,
        byte[] validData,
        byte[] failingData,
        IReadOnlyList<string> expectedFields,
        string? expectedErrorField,
        string? expectedErrorCode,
        int expectedFailurePosition,
        int expectedFailureConsumed)
    {
        var schemaName = GetLastSchemaName(schema);
        var query = $@"
            {schema};
            select f.Name, p.ParsedFields, p.ErrorField, p.ErrorMessage, p.BytesConsumed
            from #test.files() f
            cross apply PartialInterpret<{schemaName}>(f.Content) p
            order by f.Name";
        var table = RunBinaryQuery(
            query,
            new BinaryEntity { Name = "01-valid.bin", Content = validData },
            new BinaryEntity { Name = "02-bad.bin", Content = failingData },
            new BinaryEntity { Name = "03-valid.bin", Content = validData });

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("01-valid.bin", table[0][0]);
        Assert.AreEqual("02-bad.bin", table[1][0]);
        Assert.AreEqual("03-valid.bin", table[2][0]);
        AssertPartialRow(table[0], expectedFields, validData.Length, null, null, null);
        AssertPartialRow(table[2], expectedFields, validData.Length, null, null, null);
        AssertPartialRow(table[1], expectedFields, expectedFailureConsumed, expectedErrorField,
            expectedErrorCode, expectedFailurePosition);
    }

    private void AssertPartialText(
        string schema,
        string validText,
        string failingText,
        IReadOnlyList<string> expectedFields,
        string? expectedErrorField,
        string? expectedErrorCode,
        int expectedFailurePosition,
        int expectedFailureConsumed)
    {
        var schemaName = GetLastSchemaName(schema);
        var query = $@"
            {schema};
            select f.Name, p.ParsedFields, p.ErrorField, p.ErrorMessage, p.BytesConsumed
            from #test.lines() f
            cross apply PartialParse<{schemaName}>(f.Text) p
            order by f.Name";
        var table = RunTextQuery(
            query,
            new TextEntity { Name = "01-valid.txt", Text = validText },
            new TextEntity { Name = "02-bad.txt", Text = failingText },
            new TextEntity { Name = "03-valid.txt", Text = validText });

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("01-valid.txt", table[0][0]);
        Assert.AreEqual("02-bad.txt", table[1][0]);
        Assert.AreEqual("03-valid.txt", table[2][0]);
        AssertPartialRow(table[0], expectedFields, validText.Length, null, null, null);
        AssertPartialRow(table[2], expectedFields, validText.Length, null, null, null);
        AssertPartialRow(table[1], expectedFields, expectedFailureConsumed, expectedErrorField,
            expectedErrorCode, expectedFailurePosition);
    }

    private static void AssertPartialRow(
        Row row,
        IReadOnlyList<string> expectedFields,
        int expectedConsumed,
        string? expectedErrorField,
        string? expectedErrorCode,
        int? expectedPosition)
    {
        var fields = (Dictionary<string, object?>)row[1]!;
        foreach (var field in expectedFields)
            Assert.IsTrue(fields.ContainsKey(field), field);

        Assert.AreEqual(expectedErrorField, row[2]);
        if (expectedErrorField == null)
        {
            Assert.IsNull(row[3]);
        }
        else
        {
            var message = (string)row[3]!;
            StringAssert.Contains(message, expectedErrorCode!);
            if (expectedPosition.HasValue)
                StringAssert.Contains(message, $"at position {expectedPosition.Value}");
        }

        Assert.AreEqual(expectedConsumed, row[4]);
    }

    private void AssertSchemaFailure(string query, DiagnosticCode expectedCode)
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileGeneratedQuery(
                query,
                Guid.NewGuid().ToString(),
                new BinarySchemaProvider(new Dictionary<string, IEnumerable<BinaryEntity>>
                {
                    ["#test"] = [new BinaryEntity { Name = "seed.bin", Content = [0x01] }]
                }),
                LoggerResolver,
                TestCompilationOptions));

        AssertErrorEnvelope(exception, expectedCode, DiagnosticPhase.Schema);
        Assert.IsFalse(exception.Message.Contains("ISE", StringComparison.Ordinal));
    }

    private static void AssertUndefinedTextSchemaReference()
    {
        var registry = new SchemaRegistry();
        var visitor = new SchemaDefinitionVisitor(registry);
        var exception = Assert.ThrowsExactly<QuerySyntaxException>(() =>
            visitor.Visit(new TextSchemaNode(
                "Broken",
                [new TextFieldDefinitionNode("Value", TextFieldType.SchemaReference, "Missing")])))
            ;

        Assert.AreEqual(DiagnosticCode.MQ4003_UndefinedSchemaReference, exception.Code);
    }

    private static void AssertForwardTextSchemaReference()
    {
        var registry = new SchemaRegistry();
        var visitor = new SchemaDefinitionVisitor(registry);
        var exception = Assert.ThrowsExactly<QuerySyntaxException>(() =>
        {
            visitor.Visit(new TextSchemaNode(
                "Broken",
                [new TextFieldDefinitionNode("Value", TextFieldType.SchemaReference, "Later")]));
        });

        Assert.AreEqual(DiagnosticCode.MQ4003_UndefinedSchemaReference, exception.Code);
    }

    private static void AssertCircularTextSchemaReference()
    {
        var registry = new SchemaRegistry();
        var visitor = new SchemaDefinitionVisitor(registry);
        var exception = Assert.ThrowsExactly<QuerySyntaxException>(() =>
            visitor.Visit(new TextSchemaNode(
                "Broken",
                [new TextFieldDefinitionNode("Child", TextFieldType.SchemaReference, "Broken")])))
            ;

        Assert.AreEqual(DiagnosticCode.MQ4004_CircularSchemaReference, exception.Code);
    }

    private void AssertProviderFailure()
    {
        const string query = "select 1 from #fault.files() f";
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileGeneratedQuery(
                query,
                Guid.NewGuid().ToString(),
                new FailingSchemaProvider(),
                LoggerResolver,
                TestCompilationOptions));

        AssertErrorEnvelope(exception, DiagnosticCode.MQ7010_DataSourceOpenFailed, DiagnosticPhase.DataSource);
        Assert.AreEqual(DiagnosticSourceKind.DataSource, exception.PrimaryEnvelope.SourceKind);
        Assert.DoesNotContain("rec131-provider-secret", exception.Message);
    }

    private void AssertCancellation()
    {
        const string query = @"
            binary Packet { Value: int le };
            select p.Value
            from #test.files() f
            outer apply TryInterpret<Packet>(f.Content) p";
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var compiled = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            new BinarySchemaProvider(new Dictionary<string, IEnumerable<BinaryEntity>>
            {
                ["#test"] = [new BinaryEntity { Name = "valid.bin", Content = [0x2A, 0x00, 0x00, 0x00] }]
            }),
            LoggerResolver,
            TestCompilationOptions);

        var exception = Assert.ThrowsExactly<OperationCanceledException>(() => _ = compiled.Run(cancellation.Token).Count);
        Assert.AreEqual(cancellation.Token, exception.CancellationToken);
    }

    private void AssertInternalInvariant(string query, bool overflow = false)
    {
        var compiled = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(new Dictionary<string, IEnumerable<BasicEntity>>
            {
                ["#A"] = [overflow
                    ? new BasicEntity { Population = decimal.MaxValue, Money = 1m }
                    : new BasicEntity { Population = 10m, Money = 1m }]
            }),
            LoggerResolver,
            TestCompilationOptions);

        var exception = Assert.ThrowsExactly<QueryExecutionException>(() => _ = compiled.Run(CancellationToken.None).Count);
        Assert.IsNotNull(exception.Envelope);
        Assert.AreEqual(DiagnosticCode.MQ9002_InternalExecutionError, exception.Envelope!.Code);
        Assert.AreEqual(DiagnosticPhase.Internal, exception.Envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Internal, exception.Envelope.SourceKind);
        Assert.IsNotNull(exception.InnerException);
        Assert.DoesNotContain("rec131-provider-secret", exception.Message);
    }

    private static string GetLastSchemaName(string schema)
    {
        var matches = Regex.Matches(
            schema,
            @"\b(?:binary|text)\s+([A-Za-z_][A-Za-z0-9_]*)",
            RegexOptions.CultureInvariant);
        Assert.IsGreaterThan(0, matches.Count);
        return matches[matches.Count - 1].Groups[1].Value;
    }

    private Table RunBinaryQuery(string query, params BinaryEntity[] entities)
    {
        var provider = new BinarySchemaProvider(new Dictionary<string, IEnumerable<BinaryEntity>>
        {
            ["#test"] = entities
        });
        var compiled = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            provider,
            LoggerResolver,
            TestCompilationOptions);
        return TableMaterializationTestHelper.Materialize(compiled.Run(CancellationToken.None));
    }

    private Table RunTextQuery(string query, params TextEntity[] entities)
    {
        var provider = new TextSchemaProvider(new Dictionary<string, IEnumerable<TextEntity>>
        {
            ["#test"] = entities
        });
        var compiled = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            provider,
            LoggerResolver,
            TestCompilationOptions);
        return TableMaterializationTestHelper.Materialize(compiled.Run(CancellationToken.None));
    }

    private sealed class FailingSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            throw new InvalidOperationException("rec131-provider-secret");
        }
    }
}
