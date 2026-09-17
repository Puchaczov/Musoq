using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Tests;

/// <summary>
/// Permanent REC-130 matrix for nested interpretation positions and partial prefixes.
/// The matrix keeps bounded slices, absolute seeks, binary byte units and text character
/// units visible through the real generated interpreter path.
/// </summary>
[TestClass]
public sealed class REC130NestedInterpretationTests : BinaryOrTextualEvaluatorTestBase
{
    [TestMethod]
    [DataRow("NP-01")]
    [DataRow("NP-02")]
    [DataRow("NP-03")]
    [DataRow("NP-04")]
    [DataRow("NP-05")]
    [DataRow("NP-06")]
    [DataRow("NP-07")]
    [DataRow("NP-08")]
    [DataRow("NP-09")]
    [DataRow("NP-10")]
    [DataRow("NP-11")]
    [DataRow("NP-12")]
    public void NestedAndOffsetMatrix_ShouldPreservePositions(string caseId)
    {
        switch (caseId)
        {
            case "NP-01":
                AssertInterpretAtValid(offset: 1, data: [0xAA, 0x42, 0xCC], expectedValue: 0x42);
                return;
            case "NP-02":
                AssertInterpretAtFailure(-1, [0x01, 0x02], ParseErrorCode.InvalidPosition, -1, null);
                return;
            case "NP-03":
                AssertInterpretAtFailure(3, [0x01, 0x02], ParseErrorCode.InvalidPosition, 3, null);
                return;
        }

        AssertBinaryPartial(CreateNestedPositionCase(caseId));
    }

    [TestMethod]
    [DataRow("SP-01")]
    [DataRow("SP-02")]
    [DataRow("SP-03")]
    [DataRow("SP-04")]
    [DataRow("SP-05")]
    [DataRow("SP-06")]
    [DataRow("SP-07")]
    [DataRow("SP-08")]
    [DataRow("SP-09")]
    [DataRow("SP-10")]
    [DataRow("SP-11")]
    [DataRow("SP-12")]
    public void SubstreamMatrix_ShouldPreserveBoundedPositions(string caseId)
    {
        AssertBinaryPartial(CreateSubstreamCase(caseId));
    }

    [TestMethod]
    [DataRow("PP-01")]
    [DataRow("PP-02")]
    [DataRow("PP-03")]
    [DataRow("PP-04")]
    [DataRow("PP-05")]
    [DataRow("PP-06")]
    [DataRow("PP-07")]
    [DataRow("PP-08")]
    [DataRow("PP-09")]
    [DataRow("PP-10")]
    [DataRow("PP-11")]
    [DataRow("PP-12")]
    public void PartialProgressMatrix_ShouldPreservePrefixes(string caseId)
    {
        var binaryCase = CreateBinaryProgressCase(caseId);
        if (binaryCase != null)
        {
            AssertBinaryPartial(binaryCase);
            return;
        }

        AssertTextPartial(CreateTextProgressCase(caseId));
    }

    [TestMethod]
    [DataRow("TU-01")]
    [DataRow("TU-02")]
    [DataRow("TU-03")]
    [DataRow("TU-04")]
    [DataRow("TU-05")]
    [DataRow("TU-06")]
    [DataRow("TU-07")]
    [DataRow("TU-08")]
    [DataRow("TU-09")]
    [DataRow("TU-10")]
    [DataRow("TU-11")]
    [DataRow("TU-12")]
    public void TextUnitMatrix_ShouldPreserveByteAndCharacterUnits(string caseId)
    {
        var binaryCase = CreateBinaryTextUnitCase(caseId);
        if (binaryCase != null)
        {
            AssertBinaryPartial(binaryCase);
            return;
        }

        AssertTextPartial(CreateTextUnitCase(caseId));
    }

    private void AssertInterpretAtValid(int offset, byte[] data, byte expectedValue)
    {
        var query = $@"
            binary Positioned {{
                Value: byte
            }};
            select p.Value
            from #test.files() f
            cross apply InterpretAt<Positioned>(f.Content, {offset}) p";

        var table = RunBinaryQuery(query, new BinaryEntity { Name = "positioned.bin", Content = data });
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(expectedValue, table[0][0]);
    }

    private void AssertInterpretAtFailure(
        int offset,
        byte[] data,
        ParseErrorCode expectedCode,
        int expectedPosition,
        string? expectedField)
    {
        var query = $@"
            binary Positioned {{
                Value: byte
            }};
            select p.Value
            from #test.files() f
            cross apply InterpretAt<Positioned>(f.Content, {offset}) p";

        var exception = Assert.ThrowsExactly<ParseException>(() =>
            RunBinaryQuery(query, new BinaryEntity { Name = "positioned.bin", Content = data }));
        Assert.AreEqual(expectedCode, exception.ErrorCode);
        Assert.AreEqual(expectedPosition, exception.Position);
        Assert.AreEqual(expectedField, exception.FieldName);
    }

    private PartialBinaryCase CreateNestedPositionCase(string caseId)
    {
        return caseId switch
        {
            "NP-04" => new PartialBinaryCase(
                @"
                    binary Child { A: byte, B: byte };
                    binary Root { Prefix: byte, Payload: Child, Tail: byte }",
                [0xAA, 0x01, 0x02, 0xBB],
                [0xAA, 0x01],
                "Payload.B",
                "ISE0001",
                2,
                ["Prefix"]),
            "NP-05" => new PartialBinaryCase(
                @"
                    binary Item { A: byte, B: byte };
                    binary Root { Prefix: byte, Items: Item[2], Tail: byte }",
                [0xAA, 0x01, 0x02, 0x03, 0x04, 0xBB],
                [0xAA, 0x01, 0x02, 0x03],
                "Items.B",
                "ISE0001",
                4,
                ["Prefix"]),
            "NP-06" => new PartialBinaryCase(
                @"
                    binary Item { A: byte, B: byte };
                    binary Root { Prefix: byte, Items: Item[2], Tail: byte }",
                [0xAA, 0x01, 0x02, 0x03, 0x04, 0xBB],
                [0xAA, 0x01],
                "Items.B",
                "ISE0001",
                2,
                ["Prefix"]),
            "NP-07" => new PartialBinaryCase(
                @"
                    binary Item { A: byte, B: byte };
                    binary Root { Prefix: byte, Items: Item[2], Tail: byte }",
                [0xAA, 0x01, 0x02, 0x03, 0x04, 0xBB],
                [0xAA, 0x01, 0x02, 0x03, 0x04, 0xBB],
                null,
                null,
                6,
                ["Prefix", "Items", "Tail"]),
            "NP-08" => new PartialBinaryCase(
                @"
                    binary Root { Prefix: byte, Jump: byte at 6, Tail: byte }",
                [0xAA, 0x00, 0x00, 0x00, 0x00, 0x00, 0x55, 0xBB],
                [0xAA, 0x00, 0x00],
                "Jump",
                "ISE0001",
                6,
                ["Prefix"]),
            "NP-09" => new PartialBinaryCase(
                @"
                    binary Root { Prefix: byte, Marker: byte at 0, Tail: byte }",
                [0x42, 0x99],
                [0x42, 0x99],
                null,
                null,
                2,
                ["Prefix", "Marker", "Tail"]),
            "NP-10" => new PartialBinaryCase(
                @"
                    binary Root { Offset: byte, Jump: byte at Offset, Tail: byte }",
                [0x03, 0x00, 0x00, 0x55, 0xBB],
                [0x03, 0x00],
                "Jump",
                "ISE0001",
                3,
                ["Offset"]),
            "NP-11" => new PartialBinaryCase(
                @"
                    binary Child { A: byte, B: byte };
                    binary Root { Prefix: byte, Payload: Child, Tail: byte }",
                [0xAA, 0x01, 0x02, 0xBB],
                [0xAA, 0x01],
                "Payload.B",
                "ISE0001",
                2,
                ["Prefix"]),
            "NP-12" => new PartialBinaryCase(
                @"
                    binary Child { A: byte, B: byte };
                    binary Root { Prefix: byte, Payload: Child, Tail: byte }",
                [0xAA, 0x01, 0x02, 0xBB],
                [0xAA, 0x01, 0x02, 0xBB],
                null,
                null,
                4,
                ["Prefix", "Payload", "Tail"]),
            _ => throw new AssertFailedException($"Unknown nested position case '{caseId}'.")
        };
    }

    private PartialBinaryCase CreateSubstreamCase(string caseId)
    {
        return caseId switch
        {
            "SP-01" => new PartialBinaryCase(
                @"
                    binary Root { Kind: byte, Length: byte, Payload: substream[Length] raw, Tail: byte }",
                [0x01, 0x02, 0xAA, 0xBB, 0x7F],
                [0x01, 0x02, 0xAA, 0xBB, 0x7F],
                null, null, 5, ["Kind", "Length", "Payload", "Tail"]),
            "SP-02" => new PartialBinaryCase(
                @"
                    binary Body { A: byte, B: byte };
                    binary Root { Kind: byte, Length: byte, Payload: substream[Length] as Body exact, Tail: byte }",
                [0x01, 0x02, 0xAA, 0xBB, 0x7F],
                [0x01, 0x02, 0xAA, 0xBB, 0x7F],
                null, null, 5, ["Kind", "Length", "Payload", "Tail"]),
            "SP-03" => new PartialBinaryCase(
                @"
                    binary Body { A: byte, B: byte };
                    binary Root { Kind: byte, Length: byte, Payload: substream[Length] as Body lax, Tail: byte }",
                [0x01, 0x03, 0xAA, 0xBB, 0xCC, 0x7F],
                [0x01, 0x03, 0xAA, 0xBB, 0xCC, 0x7F],
                null, null, 6, ["Kind", "Length", "Payload", "Tail"]),
            "SP-04" => new PartialBinaryCase(
                @"
                    binary Inner { X: byte };
                    binary Middle { Length: byte, Payload: substream[Length] as Inner, Tag: byte };
                    binary Root { Length: byte, Payload: substream[Length] as Middle, Footer: byte }",
                [0x03, 0x01, 0x2A, 0x55, 0x7E],
                [0x03, 0x01, 0x2A, 0x55, 0x7E],
                null, null, 5, ["Length", "Payload", "Footer"]),
            "SP-05" => new PartialBinaryCase(
                @"
                    binary Item { Value: byte, Magic: byte check Magic = 0xEE };
                    binary Body { Items: Item[2] };
                    binary Root { Prefix: byte, Length: byte, Payload: substream[Length] as Body, Tail: byte }",
                [0xAA, 0x04, 0x01, 0xEE, 0x02, 0xEE, 0xBB],
                [0xAA, 0x04, 0x01, 0xEE, 0x02, 0x00, 0xBB],
                "Payload.Items.Magic", "ISE0002", 6, ["Prefix"], 6),
            "SP-06" => new PartialBinaryCase(
                @"
                    binary Body { Value: byte[3] };
                    binary Root { Prefix: byte, Length: byte, Payload: substream[Length] as Body, Tail: byte }",
                [0xAA, 0x03, 0x01, 0x02, 0x03, 0xBB],
                [0xAA, 0x02, 0x01, 0x02, 0xBB],
                "Payload.Value", "ISE0001", 2, ["Prefix"]),
            "SP-07" => new PartialBinaryCase(
                @"
                    binary Body { A: byte, B: byte };
                    binary Root { Prefix: byte, Length: byte, Payload: substream[Length] as Body exact, Tail: byte }",
                [0xAA, 0x02, 0x01, 0x02, 0xBB],
                [0xAA, 0x03, 0x01, 0x02, 0xCC, 0xBB],
                "Payload", "ISE0002", 2, ["Prefix", "Length"]),
            "SP-08" => new PartialBinaryCase(
                @"
                    binary Root { Prefix: byte, Length: byte, Payload: substream[Length] raw, Tail: byte }",
                [0xAA, 0x00, 0xBB],
                [0xAA, 0x00, 0xBB],
                null, null, 3, ["Prefix", "Length", "Payload", "Tail"]),
            "SP-09" => new PartialBinaryCase(
                @"
                    text Body { Value: chars[1] };
                    binary Root { Prefix: byte, Value: string[2] utf8 as Body, Tail: byte }",
                [0xAA, 0xC3, 0xA9, 0xBB],
                [0xAA, 0xC3, 0xA9, 0xBB],
                null, null, 4, ["Prefix", "Value", "Tail"]),
            "SP-10" => new PartialBinaryCase(
                @"
                    binary Body { Value: byte check Value = 0xEE };
                    binary Root { Prefix: byte, Length: byte, Payload: substream[Length] as Body, Tail: byte }",
                [0xAA, 0x01, 0xEE, 0xBB],
                [0xAA, 0x01, 0x00, 0xBB],
                "Payload.Value", "ISE0002", 3, ["Prefix", "Length"], 3),
            "SP-11" => new PartialBinaryCase(
                @"
                    binary Body { Prefix: byte, Value: byte at 3 };
                    binary Root { Prefix: byte, Length: byte, Payload: substream[Length] as Body, Tail: byte }",
                [0xAA, 0x04, 0x01, 0x00, 0x00, 0x55, 0xBB],
                [0xAA, 0x02, 0x01, 0x02, 0xBB],
                "Payload.Value", "ISE0001", 5, ["Prefix", "Length"], 5),
            "SP-12" => new PartialBinaryCase(
                @"
                    binary Root { Prefix: byte, Length: byte, Payload: substream[Length] raw, Tail: byte }",
                [0xAA, 0x02, 0x01, 0x02, 0xBB],
                [0xAA, 0x04, 0x01],
                "Payload", "ISE0001", 2, ["Prefix", "Length"]),
            _ => throw new AssertFailedException($"Unknown substream case '{caseId}'.")
        };
    }

    private PartialBinaryCase? CreateBinaryProgressCase(string caseId)
    {
        return caseId switch
        {
            "PP-01" => new PartialBinaryCase(
                "binary Root { Value: byte }",
                [0x01], [], "Value", "ISE0001", 0, []),
            "PP-02" => new PartialBinaryCase(
                "binary Root { Prefix: byte, Value: int le }",
                [0xAA, 0x01, 0x02, 0x03, 0x04], [0xAA, 0x01], "Value", "ISE0001", 1, ["Prefix"]),
            "PP-03" => new PartialBinaryCase(
                "binary Child { Value: int le }; binary Root { Prefix: byte, Payload: Child }",
                [0xAA, 0x01, 0x02, 0x03, 0x04], [0xAA, 0x01], "Payload.Value", "ISE0001", 1, ["Prefix"]),
            "PP-04" => new PartialBinaryCase(
                "binary Child { A: byte, B: byte }; binary Root { Prefix: byte, Payload: Child }",
                [0xAA, 0x01, 0x02], [0xAA, 0x01], "Payload.B", "ISE0001", 2, ["Prefix"]),
            "PP-05" => new PartialBinaryCase(
                "binary Root { Prefix: byte, Values: byte[3] }",
                [0xAA, 0x01, 0x02, 0x03], [0xAA, 0x01, 0x02], "Values", "ISE0001", 1, ["Prefix"]),
            "PP-06" => new PartialBinaryCase(
                "binary Item { A: byte, B: byte }; binary Root { Prefix: byte, Items: Item[2] }",
                [0xAA, 0x01, 0x02, 0x03, 0x04], [0xAA, 0x01, 0x02, 0x03], "Items.B", "ISE0001", 4, ["Prefix"]),
            "PP-07" => new PartialBinaryCase(
                "binary Body { Value: byte[3] }; binary Root { Prefix: byte, Length: byte, Payload: substream[Length] as Body }",
                [0xAA, 0x03, 0x01, 0x02, 0x03], [0xAA, 0x02, 0x01, 0x02], "Payload.Value", "ISE0001", 2, ["Prefix", "Length"]),
            "PP-08" => new PartialBinaryCase(
                "binary Body { A: byte, B: byte[2] }; binary Root { Prefix: byte, Length: byte, Payload: substream[Length] as Body }",
                [0xAA, 0x03, 0x01, 0x02, 0x03], [0xAA, 0x02, 0x01, 0x02], "Payload.B", "ISE0001", 3, ["Prefix", "Length"], 3),
            _ => null
        };
    }

    private PartialTextCase CreateTextProgressCase(string caseId)
    {
        return caseId switch
        {
            "PP-09" => new PartialTextCase(
                "text Root { Value: chars[2] }", "ab", "", "Value", "ISE0001", 0, [], 0),
            "PP-10" => new PartialTextCase(
                "text Root { Prefix: chars[2], Value: chars[3] }", "abxyz", "abx", "Value", "ISE0001", 2, ["Prefix"], 2),
            "PP-11" => new PartialTextCase(
                "text Inner { Value: chars[2] }; text Root { Prefix: chars[1], Payload: Inner }", "pab", "pa", "Payload.Value", "ISE0001", 1, ["Prefix"], 1),
            "PP-12" => new PartialTextCase(
                "text Inner { A: chars[1], B: chars[2] }; text Root { Prefix: chars[1], Payload: Inner }", "pabc", "pab", "Payload.B", "ISE0001", 2, ["Prefix"], 2),
            _ => throw new AssertFailedException($"Unknown text progress case '{caseId}'.")
        };
    }

    private PartialBinaryCase? CreateBinaryTextUnitCase(string caseId)
    {
        return caseId switch
        {
            "TU-01" => new PartialBinaryCase(
                "binary Root { Prefix: byte, Value: string[2] utf8, Tail: byte }",
                [0x07, 0x41, 0x42, 0xA5], [0x07, 0xC3, 0x28, 0xA5], "Value", "ISE0010", 1, ["Prefix"], 1),
            "TU-02" => new PartialBinaryCase(
                "binary Root { Prefix: byte, Value: string[3] utf8 nullterm, Tail: byte }",
                [0x07, 0x41, 0x00, 0x00, 0xA5], [0x07, 0xC3, 0x28, 0x00, 0xA5], "Value", "ISE0010", 1, ["Prefix"], 1),
            "TU-05" => new PartialBinaryCase(
                "text Digits { Value: pattern '\\d+' }; binary Root { Prefix: byte, Value: string[2] ascii as Digits, Tail: byte }",
                [0x07, 0x31, 0x32, 0xA5], [0x07, 0x41, 0x21, 0xA5], "Value", "ISE0003", 3, ["Prefix"], 0),
            "TU-06" => new PartialBinaryCase(
                "text Digits { Value: pattern '\\d+' }; binary Root { Prefix: byte, Value: string[2] ascii as Digits, Tail: byte }",
                [0x07, 0x31, 0x32, 0xA5], [0x07, 0x31, 0x32, 0xA5], null, null, 4, ["Prefix", "Value", "Tail"]),
            _ => null
        };
    }

    private PartialTextCase CreateTextUnitCase(string caseId)
    {
        return caseId switch
        {
            "TU-03" => new PartialTextCase("text Root { Prefix: chars[2], Value: pattern '\\d+' }", "xx12", "xxabc", "Value", "ISE0003", 2, ["Prefix"], 2),
            "TU-04" => new PartialTextCase("text Root { Prefix: chars[2], Value: pattern '\\d+' }", "😀12", "😀a", "Value", "ISE0003", 2, ["Prefix"], 2),
            "TU-07" => new PartialTextCase("text Root { Prefix: chars[2], Value: pattern '\\d+' }", "xx12", "xxabc", "Value", "ISE0003", 2, ["Prefix"], 2),
            "TU-08" => new PartialTextCase("text Inner { Value: chars[2] }; text Root { Prefix: chars[1], Payload: Inner, Tail: rest }", "pabtail", "pabtail", null, null, 7, ["Prefix", "Payload", "Tail"]),
            "TU-09" => new PartialTextCase("text Root { Key: until '=', Value: rest }", "host=ok", "host", "Key", "ISE0005", 0, [], 0),
            "TU-10" => new PartialTextCase("text Root { Prefix: literal 'OK', Tail: rest }", "OKtail", "NO", "Prefix", "ISE0004", 0, [], 0),
            "TU-11" => new PartialTextCase("text Root { Prefix: chars[2], Value: chars[3] }", "abxyz", "abx", "Value", "ISE0001", 2, ["Prefix"], 2),
            "TU-12" => new PartialTextCase("text Root { Prefix: chars[2], Value: chars[3] }", "abxyz", "abxyz", null, null, 5, ["Prefix", "Value"]),
            _ => throw new AssertFailedException($"Unknown text unit case '{caseId}'.")
        };
    }

    private void AssertBinaryPartial(PartialBinaryCase testCase)
    {
        var query = $@"
            {testCase.Schema};
            select f.Name, p.ParsedFields, p.ErrorField, p.ErrorMessage, p.BytesConsumed
            from #test.files() f
            cross apply PartialInterpret<Root>(f.Content) p
            order by f.Name";

        var table = RunBinaryQuery(query, [
            new BinaryEntity { Name = "01-valid.bin", Content = testCase.ValidData },
            new BinaryEntity { Name = "02-failing.bin", Content = testCase.FailingData },
            new BinaryEntity { Name = "03-valid.bin", Content = testCase.ValidData }
        ]);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("01-valid.bin", table[0][0]);
        Assert.AreEqual("03-valid.bin", table[2][0]);
        AssertPrefix(table[0], testCase.ExpectedPrefixFields, testCase.ExpectedSuccessConsumed);
        AssertPrefix(table[2], testCase.ExpectedPrefixFields, testCase.ExpectedSuccessConsumed);

        var failing = table[1];
        var fields = (Dictionary<string, object?>)failing[1]!;
        foreach (var field in testCase.ExpectedPrefixFields)
            Assert.IsTrue(fields.ContainsKey(field), field);

        if (testCase.ExpectedErrorField == null)
        {
            Assert.IsNull(failing[2]);
            Assert.IsNull(failing[3]);
            Assert.AreEqual(testCase.ExpectedSuccessConsumed, failing[4]);
            return;
        }

        Assert.AreEqual(testCase.ExpectedErrorField, failing[2]);
        var message = (string)failing[3]!;
        StringAssert.Contains(message, testCase.ExpectedErrorCode!);
        if (testCase.ExpectedErrorPosition is int expectedPosition)
            StringAssert.Contains(message, $"at position {expectedPosition}");
        Assert.AreEqual(testCase.ExpectedFailureConsumed, failing[4]);
    }

    private void AssertTextPartial(PartialTextCase testCase)
    {
        var query = $@"
            {testCase.Schema};
            select f.Name, p.ParsedFields, p.ErrorField, p.ErrorMessage, p.BytesConsumed
            from #test.lines() f
            cross apply PartialParse<Root>(f.Text) p
            order by f.Name";

        var table = RunTextQuery(query, [
            new TextEntity { Name = "01-valid.txt", Text = testCase.ValidText },
            new TextEntity { Name = "02-failing.txt", Text = testCase.FailingText },
            new TextEntity { Name = "03-valid.txt", Text = testCase.ValidText }
        ]);

        Assert.AreEqual(3, table.Count);
        AssertPrefix(table[0], testCase.ExpectedPrefixFields, testCase.ExpectedSuccessConsumed);
        AssertPrefix(table[2], testCase.ExpectedPrefixFields, testCase.ExpectedSuccessConsumed);

        var failing = table[1];
        var fields = (Dictionary<string, object?>)failing[1]!;
        foreach (var field in testCase.ExpectedPrefixFields)
            Assert.IsTrue(fields.ContainsKey(field), field);

        if (testCase.ExpectedErrorField == null)
        {
            Assert.IsNull(failing[2]);
            Assert.IsNull(failing[3]);
            Assert.AreEqual(testCase.ExpectedSuccessConsumed, failing[4]);
            return;
        }

        Assert.AreEqual(testCase.ExpectedErrorField, failing[2]);
        var message = (string)failing[3]!;
        StringAssert.Contains(message, testCase.ExpectedErrorCode!);
        if (testCase.ExpectedErrorPosition is int expectedPosition)
            StringAssert.Contains(message, $"at position {expectedPosition}");
        Assert.AreEqual(testCase.ExpectedFailureConsumed, failing[4]);
    }

    private static void AssertPrefix(Row row, IReadOnlyList<string> expectedFields, int expectedConsumed)
    {
        var fields = (Dictionary<string, object?>)row[1]!;
        foreach (var field in expectedFields)
            Assert.IsTrue(fields.ContainsKey(field), field);
        Assert.IsNull(row[2]);
        Assert.IsNull(row[3]);
        Assert.AreEqual(expectedConsumed, row[4]);
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

    private sealed record PartialBinaryCase(
        string Schema,
        byte[] ValidData,
        byte[] FailingData,
        string? ExpectedErrorField,
        string? ExpectedErrorCode,
        int ExpectedFailureConsumed,
        IReadOnlyList<string> ExpectedPrefixFields,
        int? ExpectedErrorPosition = null)
    {
        public int ExpectedSuccessConsumed => ValidData.Length;
    }

    private sealed record PartialTextCase(
        string Schema,
        string ValidText,
        string FailingText,
        string? ExpectedErrorField,
        string? ExpectedErrorCode,
        int ExpectedFailureConsumed,
        IReadOnlyList<string> ExpectedPrefixFields,
        int? ExpectedErrorPosition = null)
    {
        public int ExpectedSuccessConsumed => ValidText.Length;
    }
}
