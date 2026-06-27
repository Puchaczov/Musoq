using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualTypesAndExpressionsTests
{
    #region Complex Expression Tests

    [TestMethod]
    public void Query_SelectInterpret_WithComputedArraySize_ShouldUseExpression()
    {
        // Arrange: Array size computed from expression
        var query = @"
            binary ExpressionSize {
                Count: byte,
                Data: byte[Count * 2]
            };
            select
                h.Count,
                h.Data
            from #test.files() f
            cross apply Interpret<ExpressionSize>(f.Content) h";

        // Count=3, Data should be 6 bytes (3*2)
        var testData = new byte[] { 0x03, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        // Assert
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)3, table[0][0]);
        var data = (byte[])table[0][1];
        Assert.HasCount(6, data);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithFieldReferenceArraySize_ShouldWork()
    {
        // Arrange: Array size from single field reference
        // Note: Complex arithmetic expressions in array sizes (e.g., Total - Offset)
        // are tested at the interpreter level in BinaryInterpretationTests.
        var query = @"
            binary SimpleHeader {
                Count: byte,
                Data: byte[Count]
            };
            select
                h.Count,
                h.Data
            from #test.files() f
            cross apply Interpret<SimpleHeader>(f.Content) h";

        // Count=4, Data should be 4 bytes
        var testData = new byte[] { 0x04, 0x11, 0x22, 0x33, 0x44 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        // Assert
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)4, table[0][0]);
        var data = (byte[])table[0][1];
        Assert.HasCount(4, data);
        Assert.AreEqual((byte)0x11, data[0]);
        Assert.AreEqual((byte)0x44, data[3]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithArithmeticSizeExpression_ShouldWork()
    {
        // Arrange: Array size from arithmetic expression (Total - HeaderSize)
        var query = @"
            binary DynamicPacket {
                Total: byte,
                HeaderSize: byte,
                Data: byte[Total - HeaderSize]
            };
            select
                h.Total,
                h.HeaderSize,
                h.Data
            from #test.files() f
            cross apply Interpret<DynamicPacket>(f.Content) h";

        // Total=10, HeaderSize=2, so Data should be 10 - 2 = 8 bytes
        var testData = new byte[] { 10, 2, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        // Assert
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)10, table[0][0]); // Total
        Assert.AreEqual((byte)2, table[0][1]); // HeaderSize
        var data = (byte[])table[0][2];
        Assert.HasCount(8, data); // 10 - 2 = 8 bytes
        Assert.AreEqual((byte)0x01, data[0]);
        Assert.AreEqual((byte)0x08, data[7]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithMultiplicationSizeExpression_ShouldWork()
    {
        // Arrange: Array of shorts with size from field reference
        var query = @"
            binary ArrayPacket {
                Count: byte,
                Values: short[Count] le
            };
            select
                h.Count,
                h.Values
            from #test.files() f
            cross apply Interpret<ArrayPacket>(f.Content) h";

        // Count=3, so Values should be 3 short values = 6 bytes
        var testData = new byte[] { 3, 0x01, 0x00, 0x02, 0x00, 0x03, 0x00 }; // 3 little-endian shorts: 1, 2, 3
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        // Assert
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)3, table[0][0]); // Count
        var values = (short[])table[0][1];
        Assert.HasCount(3, values);
        Assert.AreEqual((short)1, values[0]);
        Assert.AreEqual((short)2, values[1]);
        Assert.AreEqual((short)3, values[2]);
    }

    #endregion

    #region CTE With Interpret Tests

    [TestMethod]
    public void Query_SelectInterpret_WithCte_ShouldWorkWithCommonTableExpression()
    {
        // Arrange: Use CTE with Interpret - aliases must be used in WHERE clause
        var query = @"
            binary Header {
                Id: int le,
                FileSize: int le
            };
            with ParsedHeaders as (
                select
                    f.Name as FileName,
                    h.Id as HeaderId,
                    h.FileSize as HeaderSize
                from #test.files() f
                cross apply Interpret<Header>(f.Content) h
            )
            select
                FileName,
                HeaderId,
                HeaderSize
            from ParsedHeaders
            where HeaderSize > 100
            order by HeaderId";

        var entities = new[]
        {
            new BinaryEntity { Name = "small.bin", Content = CreateHeaderData(1, 50) },
            new BinaryEntity { Name = "medium.bin", Content = CreateHeaderData(2, 150) },
            new BinaryEntity { Name = "large.bin", Content = CreateHeaderData(3, 500) }
        };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        // Assert: Only files with Size > 100
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("medium.bin", table[0][0]);
        Assert.AreEqual(2, table[0][1]);
        Assert.AreEqual(150, table[0][2]);
        Assert.AreEqual("large.bin", table[1][0]);
        Assert.AreEqual(3, table[1][1]);
        Assert.AreEqual(500, table[1][2]);
    }

    private static byte[] CreateHeaderData(int id, int size)
    {
        var data = new byte[8];
        BitConverter.GetBytes(id).CopyTo(data, 0);
        BitConverter.GetBytes(size).CopyTo(data, 4);
        return data;
    }

    #endregion
}
