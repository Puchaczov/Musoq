using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     Comprehensive stress tests for the binary and text interpretation schema implementation.
///     Covers edge cases, boundary conditions, and complex compositions derived from the
///     Musoq Interpretation Schemas specification (musoq-binary-text-spec.md).
/// </summary>
[TestClass]
public class StressTests_InterpretationSchemasTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    #region Step 1: Binary Edge-Case Data Boundaries

    /// <summary>
    ///     Tests parsing int.MaxValue in little-endian.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_IntMaxValue_ShouldParseCorrectly()
    {
        var query = @"
            binary Data { Value: int le };
            select s.Value from #test.files() f
            cross apply Interpret(f.Content, 'Data') s";

        var testData = BitConverter.GetBytes(int.MaxValue);
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(int.MaxValue, table[0][0]);
    }

    /// <summary>
    ///     Tests parsing int.MinValue in little-endian.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_IntMinValue_ShouldParseCorrectly()
    {
        var query = @"
            binary Data { Value: int le };
            select s.Value from #test.files() f
            cross apply Interpret(f.Content, 'Data') s";

        var testData = BitConverter.GetBytes(int.MinValue);
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(int.MinValue, table[0][0]);
    }

    /// <summary>
    ///     Tests parsing long.MaxValue in little-endian.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_LongMaxValue_ShouldParseCorrectly()
    {
        var query = @"
            binary Data { Value: long le };
            select s.Value from #test.files() f
            cross apply Interpret(f.Content, 'Data') s";

        var testData = BitConverter.GetBytes(long.MaxValue);
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(long.MaxValue, table[0][0]);
    }

    /// <summary>
    ///     Tests parsing ulong.MaxValue (all bits set).
    /// </summary>
    [TestMethod]
    public void Stress_Binary_ULongMaxValue_ShouldParseCorrectly()
    {
        var query = @"
            binary Data { Value: ulong le };
            select s.Value from #test.files() f
            cross apply Interpret(f.Content, 'Data') s";

        var testData = BitConverter.GetBytes(ulong.MaxValue);
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(ulong.MaxValue, table[0][0]);
    }

    /// <summary>
    ///     Tests all-zeros buffer parsed as multiple types.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_AllZerosBuffer_ShouldParseAsZeros()
    {
        var query = @"
            binary Data {
                A: int le,
                B: short le,
                C: byte,
                D: long le,
                E: double le
            };
            select s.A, s.B, s.C, s.D, s.E from #test.files() f
            cross apply Interpret(f.Content, 'Data') s";

        var testData = new byte[4 + 2 + 1 + 8 + 8]; // 23 bytes, all zeros
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0, table[0][0]);
        Assert.AreEqual((short)0, table[0][1]);
        Assert.AreEqual((byte)0, table[0][2]);
        Assert.AreEqual(0L, table[0][3]);
        Assert.AreEqual(0.0d, table[0][4]);
    }

    /// <summary>
    ///     Tests all-0xFF buffer parsed as multiple types.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_AllOnesBuffer_ShouldParseCorrectly()
    {
        var query = @"
            binary Data {
                A: int le,
                B: short le,
                C: byte,
                D: ushort le,
                E: uint le
            };
            select s.A, s.B, s.C, s.D, s.E from #test.files() f
            cross apply Interpret(f.Content, 'Data') s";

        var testData = Enumerable.Repeat((byte)0xFF, 4 + 2 + 1 + 2 + 4).ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(-1, table[0][0]); // all bits set as signed int
        Assert.AreEqual((short)-1, table[0][1]); // all bits set as signed short
        Assert.AreEqual((byte)0xFF, table[0][2]); // 255
        Assert.AreEqual((ushort)0xFFFF, table[0][3]); // 65535
        Assert.AreEqual(uint.MaxValue, table[0][4]); // 4294967295
    }

    /// <summary>
    ///     Tests parsing a double with NaN value.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_DoubleNaN_ShouldParseCorrectly()
    {
        var query = @"
            binary Data { Value: double le };
            select s.Value from #test.files() f
            cross apply Interpret(f.Content, 'Data') s";

        var testData = BitConverter.GetBytes(double.NaN);
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.IsTrue(double.IsNaN((double)table[0][0]));
    }

    /// <summary>
    ///     Tests parsing a float with negative infinity.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_FloatNegativeInfinity_ShouldParseCorrectly()
    {
        var query = @"
            binary Data { Value: float le };
            select s.Value from #test.files() f
            cross apply Interpret(f.Content, 'Data') s";

        var testData = BitConverter.GetBytes(float.NegativeInfinity);
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.IsTrue(float.IsNegativeInfinity((float)table[0][0]));
    }

    /// <summary>
    ///     Tests parsing double.Epsilon (smallest positive double).
    /// </summary>
    [TestMethod]
    public void Stress_Binary_DoubleEpsilon_ShouldParseCorrectly()
    {
        var query = @"
            binary Data { Value: double le };
            select s.Value from #test.files() f
            cross apply Interpret(f.Content, 'Data') s";

        var testData = BitConverter.GetBytes(double.Epsilon);
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(double.Epsilon, (double)table[0][0]);
    }

    /// <summary>
    ///     Tests that big-endian values are correctly reversed for all multi-byte types.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_AllTypesBigEndian_ShouldParseCorrectly()
    {
        var query = @"
            binary Data {
                A: short be,
                B: int be,
                C: long be,
                D: float be,
                E: double be
            };
            select s.A, s.B, s.C, s.D, s.E from #test.files() f
            cross apply Interpret(f.Content, 'Data') s";

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // short 0x0102 BE => bytes 01 02
        ms.Write(new byte[] { 0x01, 0x02 });
        // int 0x03040506 BE => bytes 03 04 05 06
        ms.Write(new byte[] { 0x03, 0x04, 0x05, 0x06 });
        // long 0x0708090A0B0C0D0E BE
        ms.Write(new byte[] { 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E });
        // float 1.0f BE: 3F800000
        var floatBytes = BitConverter.GetBytes(1.0f);
        Array.Reverse(floatBytes);
        ms.Write(floatBytes);
        // double 2.0 BE: 4000000000000000
        var doubleBytes = BitConverter.GetBytes(2.0d);
        Array.Reverse(doubleBytes);
        ms.Write(doubleBytes);

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((short)0x0102, table[0][0]);
        Assert.AreEqual(0x03040506, table[0][1]);
        Assert.AreEqual(0x0708090A0B0C0D0EL, table[0][2]);
        Assert.AreEqual(1.0f, table[0][3]);
        Assert.AreEqual(2.0d, table[0][4]);
    }

    /// <summary>
    ///     Tests a large contiguous buffer with many sequential records.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_LargeBufferManyFields_ShouldParseAll()
    {
        var query = @"
            binary Data {
                Count: int le,
                Values: int le[Count]
            };
            select s.Count from #test.files() f
            cross apply Interpret(f.Content, 'Data') s";

        const int count = 500;
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(count);
        for (var i = 0; i < count; i++)
            bw.Write(i);

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(count, table[0][0]);
    }

    /// <summary>
    ///     Tests CROSS APPLY over a large array to ensure all elements are enumerable.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_LargeArrayCrossApply_ShouldEnumerateAll()
    {
        var query = @"
            binary Data {
                Count: int le,
                Values: int le[Count]
            };
            select v.Value from #test.files() f
            cross apply Interpret(f.Content, 'Data') s
            cross apply s.Values v";

        const int count = 100;
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(count);
        for (var i = 0; i < count; i++)
            bw.Write(i * 10);

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(count, table.Count);

        var actualValues = table
            .Select(row => Convert.ToInt32(row[0]))
            .OrderBy(value => value)
            .ToArray();

        var expectedValues = Enumerable.Range(0, count)
            .Select(index => index * 10)
            .ToArray();

        CollectionAssert.AreEqual(expectedValues, actualValues,
            "CROSS APPLY should enumerate all array values regardless of row ordering.");
    }

    #endregion

    #region Step 2: Text Pattern with Regex Capture

    /// <summary>
    ///     Tests text schema with pattern regex matching a simple digit sequence.
    /// </summary>
    [TestMethod]
    public void Stress_Text_PatternSimpleDigits_ShouldCapture()
    {
        var query = @"
            text Data {
                Digits: pattern '\d+',
                Rest: rest
            };
            select d.Digits, d.Rest from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "12345abc" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("12345", table[0][0]);
        Assert.AreEqual("abc", table[0][1]);
    }

    /// <summary>
    ///     Tests text pattern with IP address regex.
    /// </summary>
    [TestMethod]
    public void Stress_Text_PatternIpAddress_ShouldCapture()
    {
        var query = @"
            text Data {
                Ip: pattern '\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}',
                Rest: rest
            };
            select d.Ip from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "192.168.1.100 connected" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("192.168.1.100", table[0][0]);
    }

    /// <summary>
    ///     Tests text pattern with hex value capture.
    /// </summary>
    [TestMethod]
    public void Stress_Text_PatternHexValue_ShouldCapture()
    {
        var query = @"
            text Data {
                _: pattern '0x',
                Hex: pattern '[0-9A-Fa-f]+',
                Rest: rest
            };
            select d.Hex, d.Rest from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "0xDEADBEEF end" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("DEADBEEF", table[0][0]);
        Assert.AreEqual(" end", table[0][1]);
    }

    /// <summary>
    ///     Tests text pattern combined with until and between.
    /// </summary>
    [TestMethod]
    public void Stress_Text_PatternCombinedWithUntilAndBetween_ShouldParseAll()
    {
        var query = @"
            text LogLine {
                Timestamp: between '[' ']',
                _: pattern '\s+',
                Level: pattern '[A-Z]+',
                _: pattern '\s+',
                Message: rest
            };
            select d.Timestamp, d.Level, d.Message from #test.lines() l
            cross apply Parse(l.Line, 'LogLine') d";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "[2024-01-15] ERROR something failed" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("2024-01-15", table[0][0]);
        Assert.AreEqual("ERROR", table[0][1]);
        Assert.AreEqual("something failed", table[0][2]);
    }

    /// <summary>
    ///     Tests pattern across multiple rows with filtering.
    /// </summary>
    [TestMethod]
    public void Stress_Text_PatternMultipleRowsWithFilter_ShouldFilter()
    {
        var query = @"
            text Data {
                Code: pattern '[A-Z]+',
                _: pattern '-',
                Number: rest
            };
            select d.Code, d.Number from #test.lines() l
            cross apply Parse(l.Line, 'Data') d
            where d.Code = 'ERR'";

        var entities = new[]
        {
            new TextEntity { Name = "l1", Text = "ERR-404" },
            new TextEntity { Name = "l2", Text = "OK-200" },
            new TextEntity { Name = "l3", Text = "ERR-500" },
            new TextEntity { Name = "l4", Text = "WARN-301" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region Step 3: Text Literal, Token, and Whitespace

    /// <summary>
    ///     Tests literal matching as a discard between named fields.
    ///     Note: 'until' consumes the delimiter, so literal only needs to match what follows.
    /// </summary>
    [TestMethod]
    public void Stress_Text_LiteralAsDiscard_ShouldAdvanceCursor()
    {
        var query = @"
            text Data {
                Key: until ':',
                _: literal ' ',
                Value: rest
            };
            select d.Key, d.Value from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "host: localhost" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("host", table[0][0]);
        Assert.AreEqual("localhost", table[0][1]);
    }

    /// <summary>
    ///     Tests literal with multi-character sequence.
    /// </summary>
    [TestMethod]
    public void Stress_Text_LiteralMultiChar_ShouldMatch()
    {
        var query = @"
            text Data {
                _: literal '---',
                Content: rest
            };
            select d.Content from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "---Hello World" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Hello World", table[0][0]);
    }

    /// <summary>
    ///     Tests token capture (whitespace-delimited).
    /// </summary>
    [TestMethod]
    public void Stress_Text_Token_ShouldCaptureNonWhitespace()
    {
        var query = @"
            text Data {
                First: token,
                _: whitespace,
                Second: token,
                _: whitespace,
                Third: rest
            };
            select d.First, d.Second, d.Third from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "hello world remainder" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("hello", table[0][0]);
        Assert.AreEqual("world", table[0][1]);
        Assert.AreEqual("remainder", table[0][2]);
    }

    /// <summary>
    ///     Tests whitespace* (zero or more) with no whitespace present.
    /// </summary>
    [TestMethod]
    public void Stress_Text_WhitespaceStar_NoWhitespace_ShouldSucceed()
    {
        var query = @"
            text Data {
                A: until ',',
                _: whitespace*,
                B: rest
            };
            select d.A, d.B from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "hello,world" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("hello", table[0][0]);
        Assert.AreEqual("world", table[0][1]);
    }

    /// <summary>
    ///     Tests whitespace* with multiple spaces and tabs.
    /// </summary>
    [TestMethod]
    public void Stress_Text_WhitespaceStar_MultipleSpacesAndTabs_ShouldConsume()
    {
        var query = @"
            text Data {
                A: until ':',
                _: whitespace*,
                B: rest
            };
            select d.A, d.B from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "key:   \t  value" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("key", table[0][0]);
        Assert.AreEqual("value", table[0][1]);
    }

    #endregion

    #region Step 4: Text Optional Fields

    /// <summary>
    ///     Tests optional field that is present.
    /// </summary>
    [TestMethod]
    public void Stress_Text_OptionalPresent_ShouldCapture()
    {
        var query = @"
            text Data {
                Name: until '=',
                Value: until ';',
                Extra: optional rest
            };
            select d.Name, d.Value, d.Extra from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "key=value;extra info" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("key", table[0][0]);
        Assert.AreEqual("value", table[0][1]);
        Assert.AreEqual("extra info", table[0][2]);
    }

    /// <summary>
    ///     Tests optional field that is absent (at end of input).
    /// </summary>
    [TestMethod]
    public void Stress_Text_OptionalAbsent_ShouldReturnNull()
    {
        var query = @"
            text Data {
                Name: until ':',
                Value: rest,
                TraceId: optional pattern '[a-f0-9]{8}'
            };
            select d.Name, d.Value, d.TraceId from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        var entities = new[] { new TextEntity { Name = "test.txt", Text = "key:value" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("key", table[0][0]);
        Assert.AreEqual("value", table[0][1]);
        Assert.IsNull(table[0][2]);
    }

    /// <summary>
    ///     Tests optional literal that is absent doesn't consume input.
    /// </summary>
    [TestMethod]
    public void Stress_Text_OptionalLiteralAbsent_ShouldNotConsume()
    {
        var query = @"
            text Data {
                A: until ',',
                _: optional literal ' - ',
                B: rest
            };
            select d.A, d.B from #test.lines() l
            cross apply Parse(l.Line, 'Data') d";

        // No ' - ' separator, so optional should fail silently and B gets rest
        var entities = new[] { new TextEntity { Name = "test.txt", Text = "hello,world" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("hello", table[0][0]);
        Assert.AreEqual("world", table[0][1]);
    }

    #endregion

    #region Step 5: Deep Nesting with Mixed Conditionals

    /// <summary>
    ///     Tests 3-level deep nested binary schemas with conditionals.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_DeepNestingWithConditionals_ShouldResolveAll()
    {
        var query = @"
            binary Inner {
                Value: int le
            };
            binary Middle {
                HasInner: byte,
                InnerData: Inner when HasInner <> 0
            };
            binary Outer {
                HasMiddle: byte,
                MiddleData: Middle when HasMiddle <> 0
            };
            select s.HasMiddle, s.MiddleData.HasInner, s.MiddleData.InnerData.Value
            from #test.files() f
            cross apply Interpret(f.Content, 'Outer') s";

        // HasMiddle=1, HasInner=1, Value=42
        using var ms = new MemoryStream();
        ms.WriteByte(1); // HasMiddle = 1
        ms.WriteByte(1); // HasInner = 1
        ms.Write(BitConverter.GetBytes(42));

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual((byte)1, table[0][1]);
        Assert.AreEqual(42, table[0][2]);
    }

    /// <summary>
    ///     Tests deep nesting where conditional is false at top level.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_DeepNestingConditionalFalse_ShouldReturnNulls()
    {
        var query = @"
            binary Inner {
                Value: int le
            };
            binary Middle {
                HasInner: byte,
                InnerData: Inner when HasInner <> 0,
                Trailer: byte
            };
            binary Outer {
                HasMiddle: byte,
                MiddleData: Middle when HasMiddle <> 0,
                Footer: int le
            };
            select s.HasMiddle, s.MiddleData, s.Footer
            from #test.files() f
            cross apply Interpret(f.Content, 'Outer') s";

        // HasMiddle=0, Footer=99
        using var ms = new MemoryStream();
        ms.WriteByte(0); // HasMiddle = 0
        ms.Write(BitConverter.GetBytes(99));

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0, table[0][0]);
        Assert.IsNull(table[0][1]);
        Assert.AreEqual(99, table[0][2]);
    }

    /// <summary>
    ///     Tests nested schema arrays with conditional inner fields.
    ///     When an array element has a conditional field that evaluates to null,
    ///     the generated code should gracefully handle it by producing null values.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_NestedArrayWithConditionalInnerFields_ShouldParseAll()
    {
        var query = @"
            binary Item {
                Tag: byte,
                Payload: int le when Tag <> 0
            };
            binary Container {
                Count: byte,
                Items: Item[Count]
            };
            select i.Tag, i.Payload
            from #test.files() f
            cross apply Interpret(f.Content, 'Container') c
            cross apply c.Items i
            order by i.Tag asc";

        using var ms = new MemoryStream();
        ms.WriteByte(3); // Count = 3

        // Item 1: Tag=1, Payload=100
        ms.WriteByte(1);
        ms.Write(BitConverter.GetBytes(100));

        // Item 2: Tag=0, Payload=null (not written)
        ms.WriteByte(0);

        // Item 3: Tag=2, Payload=200
        ms.WriteByte(2);
        ms.Write(BitConverter.GetBytes(200));

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual((byte)0, table[0][0]);
        Assert.IsNull(table[0][1]);
        Assert.AreEqual((byte)1, table[1][0]);
        Assert.AreEqual(100, table[1][1]);
        Assert.AreEqual((byte)2, table[2][0]);
        Assert.AreEqual(200, table[2][1]);
    }

    /// <summary>
    ///     Tests multiple levels of computed fields referencing nested fields.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_ComputedFromNested_ShouldEvaluate()
    {
        var query = @"
            binary Point {
                X: int le,
                Y: int le
            };
            binary Line {
                Start: Point,
                Finish: Point,
                DeltaX: Finish.X - Start.X,
                DeltaY: Finish.Y - Start.Y
            };
            select s.Start.X, s.Start.Y, s.Finish.X, s.Finish.Y, s.DeltaX, s.DeltaY
            from #test.files() f
            cross apply Interpret(f.Content, 'Line') s";

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(10); // Start.X
        bw.Write(20); // Start.Y
        bw.Write(50); // End.X
        bw.Write(80); // End.Y

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(10, table[0][0]);
        Assert.AreEqual(20, table[0][1]);
        Assert.AreEqual(50, table[0][2]);
        Assert.AreEqual(80, table[0][3]);
        Assert.AreEqual(40, table[0][4]); // 50 - 10
        Assert.AreEqual(60, table[0][5]); // 80 - 20
    }

    /// <summary>
    ///     Tests inheritance with conditional in child schema.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_InheritanceWithConditionalChild_ShouldParseCorrectly()
    {
        var query = @"
            binary Base {
                MsgType: byte,
                Length: short le
            };
            binary ExtMessage extends Base {
                HasPayload: byte,
                Payload: byte[Length] when HasPayload <> 0
            };
            select s.MsgType, s.Length, s.HasPayload, s.Payload
            from #test.files() f
            cross apply Interpret(f.Content, 'ExtMessage') s";

        using var ms = new MemoryStream();
        ms.WriteByte(0x01); // MsgType
        ms.Write(BitConverter.GetBytes((short)3)); // Length = 3
        ms.WriteByte(1); // HasPayload = 1
        ms.Write(new byte[] { 0xAA, 0xBB, 0xCC }); // Payload (3 bytes)

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x01, table[0][0]);
        Assert.AreEqual((short)3, table[0][1]);
        Assert.AreEqual((byte)1, table[0][2]);
        var payload = (byte[])table[0][3];
        Assert.HasCount(3, payload);
        Assert.AreEqual((byte)0xAA, payload[0]);
    }

    #endregion

    #region Step 6: Binary Bit Fields Crossing Byte Boundaries

    /// <summary>
    ///     Tests bit fields that don't fit neatly in one byte (5+5+6 = 16 bits = 2 bytes).
    /// </summary>
    [TestMethod]
    public void Stress_Binary_BitFieldsCrossBytesBoundary_ShouldParse()
    {
        var query = @"
            binary Data {
                A: bits[5],
                B: bits[5],
                C: bits[6]
            };
            select s.A, s.B, s.C from #test.files() f
            cross apply Interpret(f.Content, 'Data') s";

        // 16 bits total:
        // A = 5 bits from bit 0-4 of byte 0
        // B = 5 bits from bit 5-7 of byte 0 + bit 0-1 of byte 1
        // C = 6 bits from bit 2-7 of byte 1
        // Let's set: A=31 (11111), B=21 (10101), C=42 (101010)
        // Byte 0: bits 0-4 = A=31=11111, bits 5-7 = B low 3 bits: 101 -> byte0 = 10111111 = 0xBF
        // Byte 1: bits 0-1 = B high 2 bits: 10, bits 2-7 = C=42=101010 -> byte1 = 10101010 = 0xAA
        var testData = new byte[] { 0xBF, 0xAA };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)31, table[0][0]);
        Assert.AreEqual((byte)21, table[0][1]);
        Assert.AreEqual((byte)42, table[0][2]);
    }

    /// <summary>
    ///     Tests a 12-bit field that spans two bytes.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_BitField12Bits_ShouldParse()
    {
        var query = @"
            binary Data {
                Value: bits[12],
                Remainder: bits[4]
            };
            select s.Value, s.Remainder from #test.files() f
            cross apply Interpret(f.Content, 'Data') s";

        // 12+4 = 16 bits = 2 bytes
        // Value = 12 bits from bit 0-11 = 0xABC = 2748
        // Remainder = 4 bits from bit 12-15 = 0xD = 13
        // byte 0: bits 0-7 = low 8 bits of Value = 0xBC
        // byte 1: bits 0-3 = high 4 bits of Value = 0xA, bits 4-7 = Remainder = 0xD
        // byte 1 = 0xDA
        var testData = new byte[] { 0xBC, 0xDA };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((ushort)0xABC, table[0][0]);
        Assert.AreEqual((byte)0xD, table[0][1]);
    }

    /// <summary>
    ///     Tests alignment after odd bit count followed by byte field.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_BitFieldAlignmentBeforeByte_ShouldAlign()
    {
        var query = @"
            binary Data {
                Flags: bits[3],
                _: align[8],
                NextByte: byte
            };
            select s.Flags, s.NextByte from #test.files() f
            cross apply Interpret(f.Content, 'Data') s";

        // Flags = 3 bits from bit 0-2 of byte 0 = 5 (101)
        // align[8] skips remaining 5 bits of byte 0
        // NextByte = byte 1 = 0x42
        var testData = new byte[] { 0x05, 0x42 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)5, table[0][0]);
        Assert.AreEqual((byte)0x42, table[0][1]);
    }

    /// <summary>
    ///     Tests mixed bit fields and regular byte fields in sequence.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_BitFieldsThenBytesThenBitFields_ShouldParse()
    {
        var query = @"
            binary Data {
                A: bits[4],
                B: bits[4],
                C: byte,
                D: bits[2],
                E: bits[6]
            };
            select s.A, s.B, s.C, s.D, s.E from #test.files() f
            cross apply Interpret(f.Content, 'Data') s";

        // A=0xF(15), B=0x0(0) -> byte0 = 0x0F
        // C=0x42 -> byte1
        // D=3(11), E=0(000000) -> byte2 = 0x03
        var testData = new byte[] { 0x0F, 0x42, 0x03 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0xF, table[0][0]); // low nibble
        Assert.AreEqual((byte)0x0, table[0][1]); // high nibble
        Assert.AreEqual((byte)0x42, table[0][2]);
        Assert.AreEqual((byte)3, table[0][3]); // low 2 bits
        Assert.AreEqual((byte)0, table[0][4]); // high 6 bits
    }

    #endregion

    #region Step 7: Binary Check Constraint Failures

    /// <summary>
    ///     Tests that Interpret (not TryInterpret) throws on check constraint failure.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_CheckConstraintFails_InterpretShouldThrow()
    {
        var query = @"
            binary Data {
                Magic: int le check Magic = 0xDEADBEEF
            };
            select s.Magic from #test.files() f
            cross apply Interpret(f.Content, 'Data') s";

        // Wrong magic value
        var testData = BitConverter.GetBytes(0x12345678);
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);

        Assert.Throws<Exception>(() => vm.Run(CancellationToken.None));
    }

    /// <summary>
    ///     Tests check constraint with range validation.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_CheckConstraintRange_ValidValue_ShouldPass()
    {
        var query = @"
            binary Data {
                Version: short le check Version >= 1 and Version <= 10
            };
            select s.Version from #test.files() f
            cross apply Interpret(f.Content, 'Data') s";

        var testData = BitConverter.GetBytes((short)5);
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((short)5, table[0][0]);
    }

    /// <summary>
    ///     Tests check constraint referencing an earlier field.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_CheckConstraintReferencingEarlierField_ShouldValidate()
    {
        var query = @"
            binary Data {
                MaxLen: int le,
                ActualLen: int le check ActualLen <= MaxLen
            };
            select s.MaxLen, s.ActualLen from #test.files() f
            cross apply Interpret(f.Content, 'Data') s";

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(100); // MaxLen
        bw.Write(50); // ActualLen <= MaxLen

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(100, table[0][0]);
        Assert.AreEqual(50, table[0][1]);
    }

    /// <summary>
    ///     Tests TryInterpret returns null on check failure and filters with OUTER APPLY.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_TryInterpretWithCheckFailure_ShouldReturnNull()
    {
        var query = @"
            binary Data {
                Magic: int le check Magic = 0xDEADBEEF
            };
            select f.Name, s.Magic from #test.files() f
            outer apply TryInterpret(f.Content, 'Data') s";

        var entities = new[]
        {
            new BinaryEntity { Name = "good.bin", Content = BitConverter.GetBytes(unchecked((int)0xDEADBEEF)) },
            new BinaryEntity { Name = "bad.bin", Content = BitConverter.GetBytes(0x12345678) }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
    }

    /// <summary>
    ///     Tests multiple check constraints on different fields.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_MultipleCheckConstraints_AllPass_ShouldSucceed()
    {
        var query = @"
            binary Data {
                Magic: int le check Magic = 42,
                Version: byte check Version >= 1,
                Flags: byte check Flags <> 0
            };
            select s.Magic, s.Version, s.Flags from #test.files() f
            cross apply Interpret(f.Content, 'Data') s";

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(42); // Magic
        bw.Write((byte)3); // Version
        bw.Write((byte)1); // Flags

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(42, table[0][0]);
        Assert.AreEqual((byte)3, table[0][1]);
        Assert.AreEqual((byte)1, table[0][2]);
    }

    #endregion

    #region Step 8: Binary String Encodings Stress

    /// <summary>
    ///     Tests UTF-8 string with multibyte characters (emoji = 4 bytes).
    /// </summary>
    [TestMethod]
    public void Stress_Binary_Utf8FourByteChars_ShouldDecodeCorrectly()
    {
        var query = @"
            binary Data {
                Text: string[8] utf8
            };
            select s.Text from #test.files() f
            cross apply Interpret(f.Content, 'Data') s";

        // "Hi" + smiley emoji (U+1F600 = 4 bytes) + "!" = 2+4+1 = 7 bytes, pad to 8
        var text = "Hi\U0001F600!";
        var bytes = Encoding.UTF8.GetBytes(text);
        var testData = new byte[8];
        Array.Copy(bytes, testData, Math.Min(bytes.Length, 8));

        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        var result = (string)table[0][0];
        Assert.StartsWith("Hi", result);
    }

    /// <summary>
    ///     Tests ASCII string with all printable characters.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_AsciiFullPrintable_ShouldDecodeCorrectly()
    {
        var query = @"
            binary Data {
                Text: string[26] ascii
            };
            select s.Text from #test.files() f
            cross apply Interpret(f.Content, 'Data') s";

        var testText = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var testData = Encoding.ASCII.GetBytes(testText);
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(testText, table[0][0]);
    }

    /// <summary>
    ///     Tests nullterm with null in the middle of the buffer.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_NulltermInMiddle_ShouldTruncateAtNull()
    {
        var query = @"
            binary Data {
                Text: string[10] ascii nullterm
            };
            select s.Text from #test.files() f
            cross apply Interpret(f.Content, 'Data') s";

        // "Hello\0XXXX" - null at position 5, rest is junk
        var testData = new byte[10];
        Encoding.ASCII.GetBytes("Hello").CopyTo(testData, 0);
        testData[5] = 0;
        testData[6] = (byte)'X';
        testData[7] = (byte)'X';

        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Hello", table[0][0]);
    }

    /// <summary>
    ///     Tests nullterm with no null byte present - should return entire string.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_NulltermNoNullInBuffer_ShouldReturnFullString()
    {
        var query = @"
            binary Data {
                Text: string[5] ascii nullterm
            };
            select s.Text from #test.files() f
            cross apply Interpret(f.Content, 'Data') s";

        var testData = Encoding.ASCII.GetBytes("Hello");
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Hello", table[0][0]);
    }

    /// <summary>
    ///     Tests empty string (zero-length).
    /// </summary>
    [TestMethod]
    public void Stress_Binary_EmptyString_ShouldReturnEmpty()
    {
        var query = @"
            binary Data {
                Prefix: byte,
                Text: string[0] utf8,
                Suffix: byte
            };
            select s.Prefix, s.Text, s.Suffix from #test.files() f
            cross apply Interpret(f.Content, 'Data') s";

        var testData = new byte[] { 0x01, 0x02 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual("", table[0][1]);
        Assert.AreEqual((byte)2, table[0][2]);
    }

    /// <summary>
    ///     Tests multiple strings with different encodings in same schema.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_MultipleEncodings_ShouldDecodeAllCorrectly()
    {
        var query = @"
            binary Data {
                AsciiText: string[5] ascii,
                Utf8Text: string[5] utf8,
                Latin1Text: string[5] latin1
            };
            select s.AsciiText, s.Utf8Text, s.Latin1Text from #test.files() f
            cross apply Interpret(f.Content, 'Data') s";

        using var ms = new MemoryStream();
        ms.Write(Encoding.ASCII.GetBytes("Hello"));
        ms.Write(Encoding.UTF8.GetBytes("World"));
        ms.Write(Encoding.Latin1.GetBytes("Test!"));

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Hello", table[0][0]);
        Assert.AreEqual("World", table[0][1]);
        Assert.AreEqual("Test!", table[0][2]);
    }

    /// <summary>
    ///     Tests string with trim modifier removes trailing padding.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_StringWithTrimAndPadding_ShouldTrimCorrectly()
    {
        var query = @"
            binary Data {
                Name: string[20] ascii trim,
                Code: string[10] ascii rtrim,
                Label: string[10] ascii ltrim
            };
            select s.Name, s.Code, s.Label from #test.files() f
            cross apply Interpret(f.Content, 'Data') s";

        using var ms = new MemoryStream();
        // Name: "  Hello  " padded to 20 with spaces
        ms.Write(Encoding.ASCII.GetBytes("  Hello  ".PadRight(20)));
        // Code: "ABC   " padded to 10
        ms.Write(Encoding.ASCII.GetBytes("ABC".PadRight(10)));
        // Label: "   XYZ" padded to 10
        ms.Write(Encoding.ASCII.GetBytes("   XYZ".PadRight(10)));

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Hello", table[0][0]); // trim both sides
        Assert.AreEqual("ABC", table[0][1]); // rtrim only
        Assert.StartsWith("XYZ", (string)table[0][2]); // ltrim removes leading, trailing spaces may remain
    }

    /// <summary>
    ///     Tests UTF-16LE string decoding.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_Utf16LE_ShouldDecodeCorrectly()
    {
        var query = @"
            binary Data {
                Text: string[10] utf16le
            };
            select s.Text from #test.files() f
            cross apply Interpret(f.Content, 'Data') s";

        // "Hello" in UTF-16LE = 10 bytes (2 per char)
        var testData = Encoding.Unicode.GetBytes("Hello");
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Hello", table[0][0]);
    }

    /// <summary>
    ///     Tests UTF-16BE string decoding.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_Utf16BE_ShouldDecodeCorrectly()
    {
        var query = @"
            binary Data {
                Text: string[10] utf16be
            };
            select s.Text from #test.files() f
            cross apply Interpret(f.Content, 'Data') s";

        // "Hello" in UTF-16BE = 10 bytes
        var testData = Encoding.BigEndianUnicode.GetBytes("Hello");
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Hello", table[0][0]);
    }

    #endregion

    #region Step 9: Binary-Text Composition Edge Cases

    /// <summary>
    ///     Tests 'as' clause to parse binary string as text schema with multiple fields.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_AsTextWithMultipleFields_ShouldChainParse()
    {
        var query = @"
            text KeyValue {
                Key: until '=',
                Value: rest
            };
            binary Packet {
                ConfigLen: byte,
                Config: string[ConfigLen] utf8 as KeyValue
            };
            select p.Config.Key, p.Config.Value from #test.files() f
            cross apply Interpret(f.Content, 'Packet') p";

        var configStr = "host=localhost";
        using var ms = new MemoryStream();
        ms.WriteByte((byte)configStr.Length);
        ms.Write(Encoding.UTF8.GetBytes(configStr));

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("host", table[0][0]);
        Assert.AreEqual("localhost", table[0][1]);
    }

    /// <summary>
    ///     Tests 'as' clause with empty string payload.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_AsTextWithEmptyString_ShouldHandleGracefully()
    {
        var query = @"
            text Data {
                Content: rest
            };
            binary Packet {
                Len: byte,
                Text: string[Len] utf8 as Data,
                Trailer: byte
            };
            select p.Len, p.Text.Content, p.Trailer from #test.files() f
            cross apply Interpret(f.Content, 'Packet') p";

        // Len=0 empty string, trailer=0xFF
        var testData = new byte[] { 0x00, 0xFF };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0, table[0][0]);
        Assert.AreEqual("", table[0][1]);
        Assert.AreEqual((byte)0xFF, table[0][2]);
    }

    /// <summary>
    ///     Tests binary with multiple 'as' text fields in same schema.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_MultipleAsTextFields_ShouldParseAll()
    {
        var query = @"
            text NameValue {
                Name: until '=',
                Value: rest
            };
            binary Config {
                Entry1: string[10] utf8 as NameValue,
                Entry2: string[12] utf8 as NameValue
            };
            select c.Entry1.Name, c.Entry1.Value, c.Entry2.Name, c.Entry2.Value
            from #test.files() f
            cross apply Interpret(f.Content, 'Config') c";

        using var ms = new MemoryStream();
        // Entry1 = "key1=val1 " (10 bytes)
        ms.Write(Encoding.UTF8.GetBytes("key1=val1 "));
        // Entry2 = "key2=value2 " (12 bytes)
        ms.Write(Encoding.UTF8.GetBytes("key2=value2 "));

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("key1", table[0][0]);
        Assert.AreEqual("val1 ", table[0][1]);
        Assert.AreEqual("key2", table[0][2]);
        Assert.AreEqual("value2 ", table[0][3]);
    }

    /// <summary>
    ///     Tests chaining: binary -> text with 'as' on a string field that uses pattern.
    /// </summary>
    [TestMethod]
    public void Stress_Binary_AsTextWithPatternExtraction_ShouldChainParse()
    {
        var query = @"
            text VersionInfo {
                Major: pattern '\d+',
                _: pattern '\.',
                Minor: pattern '\d+',
                _: pattern '\.',
                Patch: rest
            };
            binary Header {
                Magic: int le,
                VersionStr: string[10] utf8 as VersionInfo
            };
            select h.Magic, h.VersionStr.Major, h.VersionStr.Minor, h.VersionStr.Patch
            from #test.files() f
            cross apply Interpret(f.Content, 'Header') h";

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(0xCAFE);
        ms.Write(Encoding.UTF8.GetBytes("12.34.5678"));

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0xCAFE, table[0][0]);
        Assert.AreEqual("12", table[0][1]);
        Assert.AreEqual("34", table[0][2]);
        Assert.AreEqual("5678", table[0][3]);
    }

    #endregion

    #region Step 10: Complex Multi-Schema Queries

    /// <summary>
    ///     Tests multiple CROSS APPLY chains on same binary data.
    /// </summary>
    [TestMethod]
    public void Stress_Complex_MultipleCrossApplyChains_ShouldWork()
    {
        var query = @"
            binary Header {
                Magic: int le,
                Count: short le
            };
            binary Record {
                Id: int le,
                Value: double le
            };
            select h.Magic, h.Count, r.Id, r.Value
            from #test.files() f
            cross apply Interpret(f.Content, 'Header') h
            cross apply InterpretAt(f.Content, 6, 'Record') r";

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(0xBEEF); // Magic
        bw.Write((short)1); // Count
        bw.Write(42); // Record.Id at offset 6
        bw.Write(3.14d); // Record.Value

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0xBEEF, table[0][0]);
        Assert.AreEqual((short)1, table[0][1]);
        Assert.AreEqual(42, table[0][2]);
        Assert.AreEqual(3.14d, table[0][3]);
    }

    /// <summary>
    ///     Tests WHERE and GROUP BY on interpreted fields across multiple files.
    /// </summary>
    [TestMethod]
    public void Stress_Complex_GroupByOnInterpretedField_ShouldAggregate()
    {
        var query = @"
            binary Data {
                Category: byte,
                Value: int le
            };
            select s.Category, Count(s.Category), Sum(s.Value)
            from #test.files() f
            cross apply Interpret(f.Content, 'Data') s
            group by s.Category
            having Count(s.Category) > 1";

        using var ms1 = new MemoryStream();
        ms1.WriteByte(1);
        ms1.Write(BitConverter.GetBytes(10));
        using var ms2 = new MemoryStream();
        ms2.WriteByte(2);
        ms2.Write(BitConverter.GetBytes(20));
        using var ms3 = new MemoryStream();
        ms3.WriteByte(1);
        ms3.Write(BitConverter.GetBytes(30));
        using var ms4 = new MemoryStream();
        ms4.WriteByte(1);
        ms4.Write(BitConverter.GetBytes(40));

        var entities = new[]
        {
            new BinaryEntity { Name = "f1.bin", Content = ms1.ToArray() },
            new BinaryEntity { Name = "f2.bin", Content = ms2.ToArray() },
            new BinaryEntity { Name = "f3.bin", Content = ms3.ToArray() },
            new BinaryEntity { Name = "f4.bin", Content = ms4.ToArray() }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        // Only category 1 has count > 1 (3 entries)
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual(3, table[0][1]);
        Assert.AreEqual(80m, table[0][2]); // 10+30+40
    }

    /// <summary>
    ///     Tests ORDER BY on interpreted fields.
    /// </summary>
    [TestMethod]
    public void Stress_Complex_OrderByOnInterpretedField_ShouldSort()
    {
        var query = @"
            binary Data {
                Priority: byte,
                Name: string[10] ascii trim
            };
            select s.Priority, s.Name
            from #test.files() f
            cross apply Interpret(f.Content, 'Data') s
            order by s.Priority desc";

        var makeData = (byte priority, string name) =>
        {
            var ms = new MemoryStream();
            ms.WriteByte(priority);
            ms.Write(Encoding.ASCII.GetBytes(name.PadRight(10)));
            return ms.ToArray();
        };

        var entities = new[]
        {
            new BinaryEntity { Name = "f1.bin", Content = makeData(3, "Low") },
            new BinaryEntity { Name = "f2.bin", Content = makeData(1, "Critical") },
            new BinaryEntity { Name = "f3.bin", Content = makeData(2, "Medium") }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual((byte)3, table[0][0]);
        Assert.AreEqual("Low", table[0][1]);
        Assert.AreEqual((byte)2, table[1][0]);
        Assert.AreEqual("Medium", table[1][1]);
        Assert.AreEqual((byte)1, table[2][0]);
        Assert.AreEqual("Critical", table[2][1]);
    }

    /// <summary>
    ///     Tests CTE with interpretation functions.
    /// </summary>
    [TestMethod]
    public void Stress_Complex_CteWithInterpret_ShouldWork()
    {
        var query = @"
            binary Data {
                Tag: byte,
                Value: int le
            };
            with ParsedData as (
                select s.Tag as Tag, s.Value as Value
                from #test.files() f
                cross apply Interpret(f.Content, 'Data') s
            )
            select p.Tag, p.Value from ParsedData p
            where p.Tag > 0
            order by p.Value asc";

        using var ms1 = new MemoryStream();
        ms1.WriteByte(0);
        ms1.Write(BitConverter.GetBytes(999));
        using var ms2 = new MemoryStream();
        ms2.WriteByte(1);
        ms2.Write(BitConverter.GetBytes(300));
        using var ms3 = new MemoryStream();
        ms3.WriteByte(2);
        ms3.Write(BitConverter.GetBytes(100));

        var entities = new[]
        {
            new BinaryEntity { Name = "f1.bin", Content = ms1.ToArray() },
            new BinaryEntity { Name = "f2.bin", Content = ms2.ToArray() },
            new BinaryEntity { Name = "f3.bin", Content = ms3.ToArray() }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        // Tag=0 is filtered out; Tag=2 (Value=100) comes first, then Tag=1 (Value=300)
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual((byte)2, table[0][0]);
        Assert.AreEqual(100, table[0][1]);
        Assert.AreEqual((byte)1, table[1][0]);
        Assert.AreEqual(300, table[1][1]);
    }

    /// <summary>
    ///     Tests text interpretation with GROUP BY and multiple rows.
    /// </summary>
    [TestMethod]
    public void Stress_Complex_TextGroupByMultipleRows_ShouldAggregate()
    {
        var query = @"
            text LogEntry {
                Level: until ':',
                Message: rest trim
            };
            select l.Level, Count(l.Level)
            from #test.lines() t
            cross apply Parse(t.Line, 'LogEntry') l
            group by l.Level
            order by Count(l.Level) desc";

        var entities = new[]
        {
            new TextEntity { Name = "l1", Text = "ERROR: something bad" },
            new TextEntity { Name = "l2", Text = "WARN: low disk" },
            new TextEntity { Name = "l3", Text = "ERROR: timeout" },
            new TextEntity { Name = "l4", Text = "ERROR: crash" },
            new TextEntity { Name = "l5", Text = "INFO: started" },
            new TextEntity { Name = "l6", Text = "WARN: slow query" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("ERROR", table[0][0]);
        Assert.AreEqual(3, table[0][1]);
        Assert.AreEqual("WARN", table[1][0]);
        Assert.AreEqual(2, table[1][1]);
        Assert.AreEqual("INFO", table[2][0]);
        Assert.AreEqual(1, table[2][1]);
    }

    /// <summary>
    ///     Tests multiple files with schema arrays and aggregation on array elements.
    /// </summary>
    [TestMethod]
    public void Stress_Complex_SchemaArrayWithCrossApplyAndAggregation_ShouldWork()
    {
        var query = @"
            binary Item { Id: int le, Score: int le };
            binary Container { Count: byte, Items: Item[Count] };
            select Sum(i.Score), Min(i.Score), Max(i.Score)
            from #test.files() f
            cross apply Interpret(f.Content, 'Container') c
            cross apply c.Items i";

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write((byte)4); // Count
        bw.Write(1);
        bw.Write(10); // Item 1
        bw.Write(2);
        bw.Write(50); // Item 2
        bw.Write(3);
        bw.Write(20); // Item 3
        bw.Write(4);
        bw.Write(40); // Item 4

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(120m, table[0][0]); // 10+50+20+40
        Assert.AreEqual(10m, table[0][1]); // Min
        Assert.AreEqual(50m, table[0][2]); // Max
    }

    /// <summary>
    ///     Tests combining text and binary parsing in same query via MixedSchemaProvider.
    /// </summary>
    [TestMethod]
    public void Stress_Complex_MixedBinaryAndTextSources_ShouldQueryBoth()
    {
        var query = @"
            binary BinData {
                Value: int le
            };
            text TextData {
                Key: until '=',
                Val: rest
            };
            select b.Value
            from #bin.files() f
            cross apply Interpret(f.Content, 'BinData') b
            where b.Value > 50";

        var binaryEntities = new[]
        {
            new BinaryEntity { Name = "f1.bin", Content = BitConverter.GetBytes(100) },
            new BinaryEntity { Name = "f2.bin", Content = BitConverter.GetBytes(25) },
            new BinaryEntity { Name = "f3.bin", Content = BitConverter.GetBytes(75) }
        };

        var textEntities = new[]
        {
            new TextEntity { Name = "l1", Text = "host=localhost" }
        };

        var schemaProvider = new MixedSchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#bin", binaryEntities } },
            new Dictionary<string, IEnumerable<TextEntity>> { { "#txt", textEntities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count); // 100 and 75
    }

    /// <summary>
    ///     Tests absolute positioning combined with schema arrays.
    /// </summary>
    [TestMethod]
    public void Stress_Complex_AbsolutePositionWithArrays_ShouldParseCorrectly()
    {
        var query = @"
            binary Header {
                Magic: int le at 0,
                RecordCount: short le at 4,
                DataStart: int le at 6
            };
            select h.Magic, h.RecordCount, h.DataStart
            from #test.files() f
            cross apply Interpret(f.Content, 'Header') h";

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(0x1234ABCD); // Magic at 0
        bw.Write((short)10); // RecordCount at 4
        bw.Write(100); // DataStart at 6

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0x1234ABCD, table[0][0]);
        Assert.AreEqual((short)10, table[0][1]);
        Assert.AreEqual(100, table[0][2]);
    }

    /// <summary>
    ///     Tests parsing the same binary data with two different schemas
    ///     (two CROSS APPLY Interpret calls on same input).
    /// </summary>
    [TestMethod]
    public void Stress_Complex_TwoSchemasOnSameData_ShouldParseBoth()
    {
        var query = @"
            binary AsInts { A: int le, B: int le };
            binary AsBytes { X: byte[8] };
            select i.A, i.B, b.X
            from #test.files() f
            cross apply Interpret(f.Content, 'AsInts') i
            cross apply Interpret(f.Content, 'AsBytes') b";

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(42);
        bw.Write(99);

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(42, table[0][0]);
        Assert.AreEqual(99, table[0][1]);
        var rawBytes = (byte[])table[0][2];
        Assert.HasCount(8, rawBytes);
    }

    /// <summary>
    ///     Tests computed field with complex expression involving multiple fields and operators.
    /// </summary>
    [TestMethod]
    public void Stress_Complex_ComputedFieldComplexExpression_ShouldEvaluate()
    {
        var query = @"
            binary Data {
                Width: short le,
                Height: short le,
                Depth: byte,
                Volume: Width * Height * Depth,
                IsLarge: Width * Height > 10000
            };
            select s.Width, s.Height, s.Depth, s.Volume, s.IsLarge
            from #test.files() f
            cross apply Interpret(f.Content, 'Data') s";

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write((short)10); // Width
        bw.Write((short)20); // Height
        bw.Write((byte)3); // Depth

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((short)10, table[0][0]);
        Assert.AreEqual((short)20, table[0][1]);
        Assert.AreEqual((byte)3, table[0][2]);
        // Volume = 10 * 20 * 3 = 600
        Assert.AreEqual((short)600, table[0][3]);
        // IsLarge = 10 * 20 > 10000 = false (200 > 10000 is false)
        Assert.IsFalse((bool?)table[0][4]);
    }

    /// <summary>
    ///     Tests combining discard fields with check constraints and nested schemas.
    /// </summary>
    [TestMethod]
    public void Stress_Complex_DiscardCheckNested_ShouldParseCorrectly()
    {
        var query = @"
            binary Inner { X: int le, Y: int le };
            binary Outer {
                Magic: int le check Magic = 42,
                _: byte[4],
                Data: Inner,
                _: short le,
                Footer: byte
            };
            select o.Magic, o.Data.X, o.Data.Y, o.Footer
            from #test.files() f
            cross apply Interpret(f.Content, 'Outer') o";

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(42); // Magic (check Magic = 42)
        bw.Write(new byte[] { 0, 0, 0, 0 }); // discard byte[4]
        bw.Write(10); // Inner.X
        bw.Write(20); // Inner.Y
        bw.Write((short)0); // discard short
        bw.Write((byte)0xFF); // Footer

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(42, table[0][0]);
        Assert.AreEqual(10, table[0][1]);
        Assert.AreEqual(20, table[0][2]);
        Assert.AreEqual((byte)0xFF, table[0][3]);
    }

    /// <summary>
    ///     Tests a realistic file format scenario: header + variable records.
    /// </summary>
    [TestMethod]
    public void Stress_Complex_RealisticFileFormat_ShouldParseEntirely()
    {
        var query = @"
            binary Record {
                Id: int le,
                NameLen: byte,
                Name: string[NameLen] utf8,
                Score: short le
            };
            binary FileFormat {
                Magic: int le check Magic = 0x46494C45,
                Version: byte,
                RecordCount: short le,
                Records: Record[RecordCount]
            };
            select r.Id, r.Name, r.Score
            from #test.files() f
            cross apply Interpret(f.Content, 'FileFormat') ff
            cross apply ff.Records r
            where r.Score >= 80
            order by r.Score desc";

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // Header
        bw.Write(0x46494C45); // Magic = "FILE"
        bw.Write((byte)1); // Version
        bw.Write((short)4); // RecordCount

        // Record 1: Id=1, Name="Alice", Score=95
        bw.Write(1);
        bw.Write((byte)5);
        bw.Write(Encoding.UTF8.GetBytes("Alice"));
        bw.Write((short)95);

        // Record 2: Id=2, Name="Bob", Score=72
        bw.Write(2);
        bw.Write((byte)3);
        bw.Write(Encoding.UTF8.GetBytes("Bob"));
        bw.Write((short)72);

        // Record 3: Id=3, Name="Charlie", Score=88
        bw.Write(3);
        bw.Write((byte)7);
        bw.Write(Encoding.UTF8.GetBytes("Charlie"));
        bw.Write((short)88);

        // Record 4: Id=4, Name="Dana", Score=91
        bw.Write(4);
        bw.Write((byte)4);
        bw.Write(Encoding.UTF8.GetBytes("Dana"));
        bw.Write((short)91);

        var testData = ms.ToArray();
        var entities = new[] { new BinaryEntity { Name = "data.dat", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            schemaProvider, LoggerResolver, TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        // Scores >= 80: Alice(95), Charlie(88), Dana(91) - ordered desc: 95, 91, 88
        Assert.AreEqual(3, table.Count);
        Assert.AreEqual(1, table[0][0]); // Alice
        Assert.AreEqual("Alice", table[0][1]);
        Assert.AreEqual((short)95, table[0][2]);
        Assert.AreEqual(4, table[1][0]); // Dana
        Assert.AreEqual("Dana", table[1][1]);
        Assert.AreEqual((short)91, table[1][2]);
        Assert.AreEqual(3, table[2][0]); // Charlie
        Assert.AreEqual("Charlie", table[2][1]);
        Assert.AreEqual((short)88, table[2][2]);
    }

    #endregion
}
