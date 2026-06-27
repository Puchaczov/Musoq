using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

public partial class StressTestsInterpretationSchemasTests
{
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
            cross apply Interpret<Data>(f.Content) s";

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
            cross apply Interpret<Data>(f.Content) s";

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
            cross apply Interpret<Data>(f.Content) s";

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
            cross apply Interpret<Data>(f.Content) s";

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
            cross apply Interpret<Data>(f.Content) s";

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
            cross apply Interpret<Data>(f.Content) s";

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
            cross apply Interpret<Data>(f.Content) s";

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
            cross apply Interpret<Data>(f.Content) s";

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
            cross apply Interpret<Data>(f.Content) s";

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
            cross apply Interpret<Data>(f.Content) s";

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // short 0x0102 BE => bytes 01 02
        ms.Write([0x01, 0x02]);
        // int 0x03040506 BE => bytes 03 04 05 06
        ms.Write([0x03, 0x04, 0x05, 0x06]);
        // long 0x0708090A0B0C0D0E BE
        ms.Write([0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E]);
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
            cross apply Interpret<Data>(f.Content) s";

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
            cross apply Interpret<Data>(f.Content) s
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
}
