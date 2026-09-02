using System;
using System.Collections.Generic;
using System.Buffers.Binary;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticBinary040BinaryInterpretationTests : BinaryOrTextualEvaluatorTestBase
{
    [TestMethod]
    public void BinaryInterpretation_AllPrimitiveTypes_WithBothByteOrders_ShouldDecode()
    {
        const string query = @"
            binary PrimitiveRecord {
                ByteValue: byte,
                SByteValue: sbyte,
                ShortValue: short be,
                UShortValue: ushort le,
                IntValue: int be,
                UIntValue: uint le,
                LongValue: long be,
                ULongValue: ulong le,
                FloatValue: float be,
                DoubleValue: double le
            };
            select
                r.ByteValue,
                r.SByteValue,
                r.ShortValue,
                r.UShortValue,
                r.IntValue,
                r.UIntValue,
                r.LongValue,
                r.ULongValue,
                r.FloatValue,
                r.DoubleValue
            from #test.files() f
            cross apply Interpret<PrimitiveRecord>(f.Content) r";

        var data = new byte[42];
        var offset = 0;
        data[offset++] = 0xA5;
        data[offset++] = unchecked((byte)(sbyte)-2);
        BinaryPrimitives.WriteInt16BigEndian(data.AsSpan(offset, 2), -4660);
        offset += 2;
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(offset, 2), 60000);
        offset += 2;
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(offset, 4), 0x12345678);
        offset += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset, 4), 0x89ABCDEFu);
        offset += 4;
        BinaryPrimitives.WriteInt64BigEndian(data.AsSpan(offset, 8), -0x0102030405060708L);
        offset += 8;
        BinaryPrimitives.WriteUInt64LittleEndian(data.AsSpan(offset, 8), 0xFEDCBA9876543210UL);
        offset += 8;
        BinaryPrimitives.WriteSingleBigEndian(data.AsSpan(offset, 4), 3.25f);
        offset += 4;
        BinaryPrimitives.WriteDoubleLittleEndian(data.AsSpan(offset, 8), -12.5d);

        var table = CompileGeneratedQuery(
                query,
                Guid.NewGuid().ToString(),
                new BinarySchemaProvider(new Dictionary<string, IEnumerable<BinaryEntity>>
                {
                    ["#test"] = [new BinaryEntity { Name = "primitive.bin", Content = data }]
                }),
                LoggerResolver,
                TestCompilationOptions)
            .Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0xA5, table[0][0]);
        Assert.AreEqual((sbyte)-2, table[0][1]);
        Assert.AreEqual((short)-4660, table[0][2]);
        Assert.AreEqual((ushort)60000, table[0][3]);
        Assert.AreEqual(0x12345678, table[0][4]);
        Assert.AreEqual(0x89ABCDEFu, table[0][5]);
        Assert.AreEqual(-0x0102030405060708L, table[0][6]);
        Assert.AreEqual(0xFEDCBA9876543210UL, table[0][7]);
        Assert.AreEqual(3.25f, (float)table[0][8], 0.00001f);
        Assert.AreEqual(-12.5d, (double)table[0][9], 0.0000001d);
    }

    [TestMethod]
    public void BinaryInterpretation_ByteArraysAndEncodedStrings_ShouldHonorWidthsAndModifiers()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        const string query = @"
            binary Framed {
                Length: byte,
                Payload: byte[Length],
                Utf8Value: string[5] utf8,
                Utf16LeValue: string[4] utf16le,
                Utf16BeValue: string[4] utf16be,
                AsciiValue: string[3] ascii,
                Latin1Value: string[4] latin1,
                EbcdicValue: string[4] ebcdic,
                TrimmedValue: string[8] ascii trim,
                LeftTrimmedValue: string[7] ascii ltrim,
                RightTrimmedValue: string[7] ascii rtrim,
                NullTerminatedValue: string[8] ascii nullterm,
                Tail: byte
            };
            select
                r.Length,
                r.Payload,
                r.Utf8Value,
                r.Utf16LeValue,
                r.Utf16BeValue,
                r.AsciiValue,
                r.Latin1Value,
                r.EbcdicValue,
                r.TrimmedValue,
                r.LeftTrimmedValue,
                r.RightTrimmedValue,
                r.NullTerminatedValue,
                r.Tail
            from #test.files() f
            cross apply Interpret<Framed>(f.Content) r";

        var data = new List<byte> { 3, 0xAA, 0xBB, 0xCC };
        data.AddRange("Hello"u8.ToArray());
        data.AddRange(Encoding.Unicode.GetBytes("Hi"));
        data.AddRange(Encoding.BigEndianUnicode.GetBytes("Yo"));
        data.AddRange("ABC"u8.ToArray());
        data.AddRange(Encoding.Latin1.GetBytes("Café"));
        data.AddRange(Encoding.GetEncoding(37).GetBytes("CICS"));
        data.AddRange("  Hi    "u8.ToArray());
        data.AddRange("  Left "u8.ToArray());
        data.AddRange("Right  "u8.ToArray());
        data.AddRange([0x47, 0x6F, 0x00, 0x3F, 0x3F, 0x3F, 0x3F, 0x3F]);
        data.Add(0x5A);

        var table = CompileGeneratedQuery(
                query,
                Guid.NewGuid().ToString(),
                new BinarySchemaProvider(new Dictionary<string, IEnumerable<BinaryEntity>>
                {
                    ["#test"] = [new BinaryEntity { Name = "strings.bin", Content = data.ToArray() }]
                }),
                LoggerResolver,
                TestCompilationOptions)
            .Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)3, table[0][0]);
        CollectionAssert.AreEqual(new byte[] { 0xAA, 0xBB, 0xCC }, (byte[])table[0][1]);
        Assert.AreEqual("Hello", table[0][2]);
        Assert.AreEqual("Hi", table[0][3]);
        Assert.AreEqual("Yo", table[0][4]);
        Assert.AreEqual("ABC", table[0][5]);
        Assert.AreEqual("Café", table[0][6]);
        Assert.AreEqual("CICS", table[0][7]);
        Assert.AreEqual("Hi", table[0][8]);
        Assert.AreEqual("Left ", table[0][9]);
        Assert.AreEqual("Right", table[0][10]);
        Assert.AreEqual("Go", table[0][11]);
        Assert.AreEqual((byte)0x5A, table[0][12]);
    }

    [TestMethod]
    public void BinaryInterpretation_ZeroLengthByteArray_ShouldPreserveFollowingFieldBoundary()
    {
        const string query = @"
            binary EmptyPayload {
                Payload: byte[0],
                Tail: byte
            };
            select r.Payload, r.Tail
            from #test.files() f
            cross apply Interpret<EmptyPayload>(f.Content) r";

        var table = CompileGeneratedQuery(
                query,
                Guid.NewGuid().ToString(),
                new BinarySchemaProvider(new Dictionary<string, IEnumerable<BinaryEntity>>
                {
                    ["#test"] = [new BinaryEntity { Name = "empty.bin", Content = [0x5A] }]
                }),
                LoggerResolver,
                TestCompilationOptions)
            .Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.IsEmpty((byte[])table[0][0]);
        Assert.AreEqual((byte)0x5A, table[0][1]);
    }
}
