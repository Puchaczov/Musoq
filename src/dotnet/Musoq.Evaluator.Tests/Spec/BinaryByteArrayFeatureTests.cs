using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     Feature tests for Binary Schema Byte Arrays (Section 4.2.3 of specification).
///     Tests fixed-size, field-referenced, and expression-based byte arrays.
/// </summary>
[TestClass]
public class BinaryByteArrayFeatureTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    #region Section 4.2.3: Multiple Byte Arrays

    /// <summary>
    ///     Tests multiple byte arrays in same schema.
    /// </summary>
    [TestMethod]
    public void Binary_MultipleByteArrays_ShouldParseSequentially()
    {
        var query = @"
            binary Data { 
                Header: byte[4],
                Body: byte[8],
                Footer: byte[2]
            };
            select d.Header, d.Body, d.Footer from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";

        var testData = new byte[14];
        for (var i = 0; i < 14; i++)
            testData[i] = (byte)(i + 1);

        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        var header = (byte[])table[0][0];
        var body = (byte[])table[0][1];
        var footer = (byte[])table[0][2];

        Assert.HasCount(4, header);
        Assert.HasCount(8, body);
        Assert.HasCount(2, footer);

        Assert.AreEqual((byte)1, header[0]);
        Assert.AreEqual((byte)5, body[0]);
        Assert.AreEqual((byte)13, footer[0]);
    }

    #endregion

    #region Section 4.2.3: Fixed-Size Byte Arrays

    /// <summary>
    ///     Tests fixed-size byte array with literal size.
    /// </summary>
    [TestMethod]
    public void Binary_ByteArrayFixedSize_ShouldParseExactBytes()
    {
        var query = @"
            binary Data { Payload: byte[4] };
            select d.Payload from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";

        var testData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        var payload = (byte[])table[0][0];
        Assert.HasCount(4, payload);
        Assert.AreEqual((byte)0x01, payload[0]);
        Assert.AreEqual((byte)0x04, payload[3]);
    }

    /// <summary>
    ///     Tests empty byte array (size 0).
    /// </summary>
    [TestMethod]
    public void Binary_ByteArrayZeroSize_ShouldReturnEmptyArray()
    {
        var query = @"
            binary Data { 
                Length: byte,
                Payload: byte[Length] 
            };
            select d.Length, d.Payload from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";

        var testData = new byte[] { 0x00 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0, table[0][0]);
        var payload = (byte[])table[0][1];
        Assert.IsEmpty(payload);
    }

    /// <summary>
    ///     Tests large fixed-size byte array.
    /// </summary>
    [TestMethod]
    public void Binary_ByteArrayLargeFixed_ShouldParseAllBytes()
    {
        var query = @"
            binary Data { Payload: byte[256] };
            select d.Payload from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";

        var testData = new byte[256];
        for (var i = 0; i < 256; i++)
            testData[i] = (byte)i;

        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        var payload = (byte[])table[0][0];
        Assert.HasCount(256, payload);
        Assert.AreEqual((byte)0, payload[0]);
        Assert.AreEqual((byte)255, payload[255]);
    }

    #endregion

    #region Section 4.2.3: Field-Referenced Size

    /// <summary>
    ///     Tests byte array with size from previous byte field.
    /// </summary>
    [TestMethod]
    public void Binary_ByteArraySizeFromByteField_ShouldParseDynamically()
    {
        var query = @"
            binary Data { 
                Length: byte,
                Payload: byte[Length] 
            };
            select d.Length, d.Payload from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";

        var testData = new byte[] { 0x05, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)5, table[0][0]);
        var payload = (byte[])table[0][1];
        Assert.HasCount(5, payload);
        Assert.AreEqual((byte)0xAA, payload[0]);
        Assert.AreEqual((byte)0xEE, payload[4]);
    }

    /// <summary>
    ///     Tests byte array with size from short field.
    /// </summary>
    [TestMethod]
    public void Binary_ByteArraySizeFromShortField_ShouldParseDynamically()
    {
        var query = @"
            binary Data { 
                Length: short le,
                Payload: byte[Length] 
            };
            select d.Length, d.Payload from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";


        var testData = new byte[] { 0x08, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((short)8, table[0][0]);
        var payload = (byte[])table[0][1];
        Assert.HasCount(8, payload);
    }

    /// <summary>
    ///     Tests byte array with size from int field.
    /// </summary>
    [TestMethod]
    public void Binary_ByteArraySizeFromIntField_ShouldParseDynamically()
    {
        var query = @"
            binary Data { 
                Length: int le,
                Payload: byte[Length] 
            };
            select d.Length, d.Payload from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";


        var testData = new byte[] { 0x03, 0x00, 0x00, 0x00, 0xAA, 0xBB, 0xCC };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(3, table[0][0]);
        var payload = (byte[])table[0][1];
        Assert.HasCount(3, payload);
    }

    #endregion

    #region Section 4.2.3: Expression-Based Size

    /// <summary>
    ///     Tests byte array with computed size (Length * 2).
    /// </summary>
    [TestMethod]
    public void Binary_ByteArraySizeMultiplied_ShouldComputeCorrectly()
    {
        var query = @"
            binary Data { 
                Count: byte,
                Items: byte[Count * 2] 
            };
            select d.Count, d.Items from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";


        var testData = new byte[] { 0x03, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)3, table[0][0]);
        var items = (byte[])table[0][1];
        Assert.HasCount(6, items);
    }

    /// <summary>
    ///     Tests byte array with addition expression.
    /// </summary>
    [TestMethod]
    public void Binary_ByteArraySizeWithAddition_ShouldComputeCorrectly()
    {
        var query = @"
            binary Data { 
                BaseLen: byte,
                Extra: byte,
                Payload: byte[BaseLen + Extra] 
            };
            select d.BaseLen, d.Extra, d.Payload from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";


        var testData = new byte[] { 0x02, 0x03, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)2, table[0][0]);
        Assert.AreEqual((byte)3, table[0][1]);
        var payload = (byte[])table[0][2];
        Assert.HasCount(5, payload);
    }

    #endregion

    #region Section 4.2.3: Byte Array Access in Queries

    /// <summary>
    ///     Tests accessing byte array elements in WHERE clause.
    /// </summary>
    [TestMethod]
    public void Binary_ByteArrayInWhereClause_ShouldFilterByElement()
    {
        var query = @"
            binary Header { Magic: byte[4] };
            select f.Name from #test.files() f
            cross apply Interpret(f.Content, 'Header') h
            where h.Magic[0] = 0x89 and h.Magic[1] = 0x50";

        var pngMagic = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var gifMagic = new byte[] { 0x47, 0x49, 0x46, 0x38 };

        var entities = new[]
        {
            new BinaryEntity { Name = "image.png", Content = pngMagic },
            new BinaryEntity { Name = "image.gif", Content = gifMagic }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("image.png", table[0][0]);
    }

    /// <summary>
    ///     Tests accessing byte array element in SELECT clause.
    /// </summary>
    [TestMethod]
    public void Binary_ByteArrayInSelectClause_ShouldReturnElement()
    {
        var query = @"
            binary Data { Values: byte[4] };
            select d.Values[0], d.Values[1], d.Values[2], d.Values[3] 
            from #test.files() f
            cross apply Interpret(f.Content, 'Data') d";

        var testData = new byte[] { 0x10, 0x20, 0x30, 0x40 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x10, table[0][0]);
        Assert.AreEqual((byte)0x20, table[0][1]);
        Assert.AreEqual((byte)0x30, table[0][2]);
        Assert.AreEqual((byte)0x40, table[0][3]);
    }

    #endregion
}
