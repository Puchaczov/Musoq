using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualCoreBinaryTests
{
    [TestMethod]
    public void Query_SelectInterpret_WithBinarySchema_ShouldParseData()
    {
        // Arrange
        var query = @"
            binary HeaderFormat {
                Magic: int le,
                Version: short le
            };
            select
                h.Magic,
                h.Version
            from #test.files() f
            cross apply Interpret<HeaderFormat>(f.Content) h";

        // Create test data: Magic=0x12345678, Version=0x0100
        var testData = new byte[] { 0x78, 0x56, 0x34, 0x12, 0x00, 0x01 };
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
        Assert.AreEqual(0x12345678, table[0][0]);
        Assert.AreEqual((short)0x0100, table[0][1]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithMultipleRows_ShouldParseAllRows()
    {
        // Arrange
        var query = @"
            binary SimpleInt {
                Value: int le
            };
            select
                f.Name,
                h.Value
            from #test.files() f
            cross apply Interpret<SimpleInt>(f.Content) h
            order by f.Name asc";

        var entities = new[]
        {
            new BinaryEntity { Name = "file1.bin", Content = BitConverter.GetBytes(100) },
            new BinaryEntity { Name = "file2.bin", Content = BitConverter.GetBytes(200) },
            new BinaryEntity { Name = "file3.bin", Content = BitConverter.GetBytes(300) }
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

        // Assert
        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("file1.bin", table[0][0]);
        Assert.AreEqual(100, table[0][1]);
        Assert.AreEqual("file2.bin", table[1][0]);
        Assert.AreEqual(200, table[1][1]);
        Assert.AreEqual("file3.bin", table[2][0]);
        Assert.AreEqual(300, table[2][1]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithFilter_ShouldApplyWhere()
    {
        // Arrange
        var query = @"
            binary FlagData {
                Flags: byte
            };
            select
                f.Name
            from #test.files() f
            cross apply Interpret<FlagData>(f.Content) h
            where h.Flags = 1
            order by f.Name asc";

        var entities = new[]
        {
            new BinaryEntity { Name = "enabled.bin", Content = [0x01] },
            new BinaryEntity { Name = "disabled.bin", Content = [0x00] },
            new BinaryEntity { Name = "also_enabled.bin", Content = [0x01] }
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

        // Assert
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("also_enabled.bin", table[0][0]); // 'a' comes before 'e'
        Assert.AreEqual("enabled.bin", table[1][0]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithByteArray_ShouldParseFixedSizeBytes()
    {
        // Arrange
        var query = @"
            binary Packet {
                Header: byte[4],
                Data: byte[2]
            };
            select
                h.Header,
                h.Data
            from #test.files() f
            cross apply Interpret<Packet>(f.Content) h";

        // Test data: 4-byte header + 2-byte data
        var testData = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x12, 0x34 };
        var entities = new[] { new BinaryEntity { Name = "packet.bin", Content = testData } };

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
        var header = (byte[])table[0][0];
        var data = (byte[])table[0][1];
        Assert.HasCount(4, header);
        Assert.HasCount(2, data);
        Assert.AreEqual((byte)0xDE, header[0]);
        Assert.AreEqual((byte)0xAD, header[1]);
        Assert.AreEqual((byte)0xBE, header[2]);
        Assert.AreEqual((byte)0xEF, header[3]);
        Assert.AreEqual((byte)0x12, data[0]);
        Assert.AreEqual((byte)0x34, data[1]);
    }
}
