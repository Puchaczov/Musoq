using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualCoreBinaryTests
{
    [TestMethod]
    public void Query_SelectInterpret_WithBitFields_ShouldParseBits()
    {
        // Arrange: Parse TCP-like header with bit fields
        var query = @"
            binary TcpFlags {
                Reserved: bits[4],
                DataOffset: bits[4],
                FIN: bits[1],
                SYN: bits[1],
                RST: bits[1],
                PSH: bits[1],
                ACK: bits[1],
                URG: bits[1],
                ECE: bits[1],
                CWR: bits[1]
            };
            select
                f.DataOffset,
                f.SYN,
                f.ACK
            from #test.files() fl
            cross apply Interpret<TcpFlags>(fl.Content) f";

        // Byte 0: Reserved=0, DataOffset=5 -> 0x50
        // Byte 1: Flags CWR=0,ECE=0,URG=0,ACK=1,PSH=0,RST=0,SYN=1,FIN=0 -> 0b00010010 = 0x12
        var testData = new byte[] { 0x50, 0x12 };
        var entities = new[] { new BinaryEntity { Name = "tcp.bin", Content = testData } };

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
        Assert.AreEqual((byte)5, table[0][0]); // DataOffset
        Assert.AreEqual((byte)1, table[0][1]); // SYN flag is set
        Assert.AreEqual((byte)1, table[0][2]); // ACK flag is set
    }

    [TestMethod]
    public void Query_SelectInterpret_WithBitFieldsAndAlignment_ShouldAlignToByte()
    {
        // Arrange: Parse structure with bit fields followed by alignment
        var query = @"
            binary PackedHeader {
                Version: bits[4],
                Type: bits[4],
                Flags: bits[3],
                Reserved: align[8],
                Length: int le
            };
            select
                h.Version,
                h.Type,
                h.Flags,
                h.Length
            from #test.files() f
            cross apply Interpret<PackedHeader>(f.Content) h";

        // Bits are read LSB-first:
        // Byte 0 = 0x21: bits[0-3]=1 (Version), bits[4-7]=2 (Type)
        // Byte 1 = 0x05: bits[0-2]=5 (Flags), align[8] skips remaining bits
        // Bytes 2-5: Length = 0x12345678 (little-endian)
        var testData = new byte[] { 0x21, 0x05, 0x78, 0x56, 0x34, 0x12 };
        var entities = new[] { new BinaryEntity { Name = "packed.bin", Content = testData } };

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
        Assert.AreEqual((byte)1, table[0][0]); // Version
        Assert.AreEqual((byte)2, table[0][1]); // Type
        Assert.AreEqual((byte)5, table[0][2]); // Flags
        Assert.AreEqual(0x12345678, table[0][3]); // Length
    }

    [TestMethod]
    public void Query_SelectInterpret_WithBitFieldsFiltered_ShouldFilterByBitValue()
    {
        // Arrange: Filter files by bit flag values
        var query = @"
            binary StatusByte {
                Active: bits[1],
                Ready: bits[1],
                Error: bits[1],
                Reserved: bits[5]
            };
            select
                f.Name
            from #test.files() f
            cross apply Interpret<StatusByte>(f.Content) s
            where s.Active = 1 and s.Error = 0
            order by f.Name";

        var entities = new[]
        {
            // Active=1, Ready=0, Error=0 -> 0b00000001 = 0x01
            new BinaryEntity { Name = "good1.bin", Content = [0x01] },
            // Active=1, Ready=1, Error=1 -> 0b00000111 = 0x07
            new BinaryEntity { Name = "bad.bin", Content = [0x07] },
            // Active=1, Ready=1, Error=0 -> 0b00000011 = 0x03
            new BinaryEntity { Name = "good2.bin", Content = [0x03] },
            // Active=0, Ready=0, Error=0 -> 0x00
            new BinaryEntity { Name = "inactive.bin", Content = [0x00] }
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

        // Assert: Only files with Active=1 and Error=0
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("good1.bin", table[0][0]);
        Assert.AreEqual("good2.bin", table[1][0]);
    }



    [TestMethod]
    public void Query_SelectInterpret_WithBitwiseOperators_ShouldCalculateFlags()
    {
        // Arrange: Flag parsing using bitwise operators
        var query = @"
            binary FlaggedData {
                Flags: byte,
                Value: int le,
                IsEnabled: (Flags & 0x01) = 0x01,
                IsReadOnly: (Flags & 0x02) = 0x02,
                Priority: (Flags >> 4) & 0x0F,
                CombinedFlags: Flags | 0x80
            };
            select
                d.Flags,
                d.Value,
                d.IsEnabled,
                d.IsReadOnly,
                d.Priority,
                d.CombinedFlags
            from #test.files() f
            cross apply Interpret<FlaggedData>(f.Content) d";

        // Flags = 0x53 = 0101 0011 binary
        // - Bit 0 (0x01) = 1 => IsEnabled = true
        // - Bit 1 (0x02) = 1 => IsReadOnly = true
        // - Bits 4-7 = 0101 = 5 => Priority = 5
        // - CombinedFlags = 0x53 | 0x80 = 0xD3 = 211
        var testData = new byte[5];
        testData[0] = 0x53; // Flags
        BitConverter.GetBytes(12345).CopyTo(testData, 1); // Value = 12345
        var entities = new[] { new BinaryEntity { Name = "flags.bin", Content = testData } };

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
        Assert.AreEqual((byte)0x53, table[0][0]); // Flags
        Assert.AreEqual(12345, table[0][1]); // Value
        Assert.IsTrue((bool?)table[0][2]); // IsEnabled (0x53 & 0x01 = 0x01)
        Assert.IsTrue((bool?)table[0][3]); // IsReadOnly (0x53 & 0x02 = 0x02)
        Assert.AreEqual(5, table[0][4]); // Priority (0x53 >> 4 = 5)
        Assert.AreEqual(0xD3, table[0][5]); // CombinedFlags (0x53 | 0x80 = 0xD3)
    }

    [TestMethod]
    public void Query_SelectInterpret_WithBitwiseXorAndShift_ShouldCalculate()
    {
        // Arrange: Test XOR and shift operations
        var query = @"
            binary BitwiseData {
                A: int le,
                B: int le,
                Xor: A ^ B,
                LeftShift: A << 2,
                RightShift: B >> 1,
                Combined: (A & 0xFF) | ((B & 0xFF) << 8)
            };
            select
                d.A,
                d.B,
                d.Xor,
                d.LeftShift,
                d.RightShift,
                d.Combined
            from #test.files() f
            cross apply Interpret<BitwiseData>(f.Content) d";

        var testData = new byte[8];
        BitConverter.GetBytes(0x0F).CopyTo(testData, 0); // A = 15 (0x0F)
        BitConverter.GetBytes(0xF0).CopyTo(testData, 4); // B = 240 (0xF0)
        var entities = new[] { new BinaryEntity { Name = "bitwise.bin", Content = testData } };

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
        Assert.AreEqual(0x0F, table[0][0]); // A = 15
        Assert.AreEqual(0xF0, table[0][1]); // B = 240
        Assert.AreEqual(0xFF, table[0][2]); // Xor = 15 ^ 240 = 255
        Assert.AreEqual(0x3C, table[0][3]); // LeftShift = 15 << 2 = 60
        Assert.AreEqual(0x78, table[0][4]); // RightShift = 240 >> 1 = 120
        Assert.AreEqual(0xF00F, table[0][5]); // Combined = 15 | (240 << 8) = 61455
    }

}
