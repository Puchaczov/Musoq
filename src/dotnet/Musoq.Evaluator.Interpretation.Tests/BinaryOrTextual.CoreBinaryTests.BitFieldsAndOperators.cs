using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualCoreBinaryTests
{
    [TestMethod]
    public void Query_SelectInterpret_WithBitFields_ShouldParseBits()
    {
        // Arrange: Parse TCP-like header with bit fields
        var query = (
            "\n"
            + "            binary TcpFlags {\n"
            + "                Reserved: bits[4],\n"
            + "                DataOffset: bits[4],\n"
            + "                FIN: bits[1],\n"
            + "                SYN: bits[1],\n"
            + "                RST: bits[1],\n"
            + "                PSH: bits[1],\n"
            + "                ACK: bits[1],\n"
            + "                URG: bits[1],\n"
            + "                ECE: bits[1],\n"
            + "                CWR: bits[1]\n"
            + "            };\n"
            + "            select\n"
            + "                f.DataOffset,\n"
            + "                f.SYN,\n"
            + "                f.ACK\n"
            + "            from #test.files() fl\n"
            + "            cross apply Interpret<TcpFlags>(fl.Content) f"
        );

        // Byte 0: Reserved=0, DataOffset=5 -> 0x50
        // Byte 1: Flags CWR=0,ECE=0,URG=0,ACK=1,PSH=0,RST=0,SYN=1,FIN=0 -> 0b00010010 = 0x12
        var testData = new byte[] { 0x50, 0x12 };
        var entities = new[]
        {
            new BinaryEntity { Name = "tcp.bin", Content = testData },
        };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        // Act
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );

        var table = vm.Run(CancellationToken.None);

        // Assert
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)5, table[0].ReadValue(0)); // DataOffset
        Assert.AreEqual((byte)1, table[0].ReadValue(1)); // SYN flag is set
        Assert.AreEqual((byte)1, table[0].ReadValue(2)); // ACK flag is set
    }

    [TestMethod]
    public void Query_SelectInterpret_WithBitFieldsAndAlignment_ShouldAlignToByte()
    {
        // Arrange: Parse structure with bit fields followed by alignment
        var query = (
            "\n"
            + "            binary PackedHeader {\n"
            + "                Version: bits[4],\n"
            + "                Type: bits[4],\n"
            + "                Flags: bits[3],\n"
            + "                Reserved: align[8],\n"
            + "                Length: int le\n"
            + "            };\n"
            + "            select\n"
            + "                h.Version,\n"
            + "                h.Type,\n"
            + "                h.Flags,\n"
            + "                h.Length\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<PackedHeader>(f.Content) h"
        );

        // Bits are read LSB-first:
        // Byte 0 = 0x21: bits[0-3]=1 (Version), bits[4-7]=2 (Type)
        // Byte 1 = 0x05: bits[0-2]=5 (Flags), align[8] skips remaining bits
        // Bytes 2-5: Length = 0x12345678 (little-endian)
        var testData = new byte[] { 0x21, 0x05, 0x78, 0x56, 0x34, 0x12 };
        var entities = new[]
        {
            new BinaryEntity { Name = "packed.bin", Content = testData },
        };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        // Act
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );

        var table = vm.Run(CancellationToken.None);

        // Assert
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)1, table[0].ReadValue(0)); // Version
        Assert.AreEqual((byte)2, table[0].ReadValue(1)); // Type
        Assert.AreEqual((byte)5, table[0].ReadValue(2)); // Flags
        Assert.AreEqual(0x12345678, table[0].ReadValue(3)); // Length
    }

    [TestMethod]
    public void Query_SelectInterpret_WithBitFieldsFiltered_ShouldFilterByBitValue()
    {
        // Arrange: Filter files by bit flag values
        var query = (
            "\n"
            + "            binary StatusByte {\n"
            + "                Active: bits[1],\n"
            + "                Ready: bits[1],\n"
            + "                Error: bits[1],\n"
            + "                Reserved: bits[5]\n"
            + "            };\n"
            + "            select\n"
            + "                f.Name\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<StatusByte>(f.Content) s\n"
            + "            where s.Active = 1 and s.Error = 0\n"
            + "            order by f.Name"
        );

        var entities = new[]
        {
            // Active=1, Ready=0, Error=0 -> 0b00000001 = 0x01
            new BinaryEntity { Name = "good1.bin", Content = [0x01] },
            // Active=1, Ready=1, Error=1 -> 0b00000111 = 0x07
            new BinaryEntity { Name = "bad.bin", Content = [0x07] },
            // Active=1, Ready=1, Error=0 -> 0b00000011 = 0x03
            new BinaryEntity { Name = "good2.bin", Content = [0x03] },
            // Active=0, Ready=0, Error=0 -> 0x00
            new BinaryEntity { Name = "inactive.bin", Content = [0x00] },
        };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        // Act
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );

        var table = vm.Run(CancellationToken.None);

        // Assert: Only files with Active=1 and Error=0
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("good1.bin", table[0].ReadValue(0));
        Assert.AreEqual("good2.bin", table[1].ReadValue(0));
    }

    [TestMethod]
    public void Query_SelectInterpret_WithBitwiseOperators_ShouldCalculateFlags()
    {
        // Arrange: Flag parsing using bitwise operators
        var query = (
            "\n"
            + "            binary FlaggedData {\n"
            + "                Flags: byte,\n"
            + "                Value: int le,\n"
            + "                IsEnabled: (Flags & 0x01) = 0x01,\n"
            + "                IsReadOnly: (Flags & 0x02) = 0x02,\n"
            + "                Priority: (Flags >> 4) & 0x0F,\n"
            + "                CombinedFlags: Flags | 0x80\n"
            + "            };\n"
            + "            select\n"
            + "                d.Flags,\n"
            + "                d.Value,\n"
            + "                d.IsEnabled,\n"
            + "                d.IsReadOnly,\n"
            + "                d.Priority,\n"
            + "                d.CombinedFlags\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<FlaggedData>(f.Content) d"
        );

        // Flags = 0x53 = 0101 0011 binary
        // - Bit 0 (0x01) = 1 => IsEnabled = true
        // - Bit 1 (0x02) = 1 => IsReadOnly = true
        // - Bits 4-7 = 0101 = 5 => Priority = 5
        // - CombinedFlags = 0x53 | 0x80 = 0xD3 = 211
        var testData = new byte[5];
        testData[0] = 0x53; // Flags
        BitConverter.GetBytes(12345).CopyTo(testData, 1); // Value = 12345
        var entities = new[]
        {
            new BinaryEntity { Name = "flags.bin", Content = testData },
        };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        // Act
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );

        var table = vm.Run(CancellationToken.None);

        // Assert
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x53, table[0].ReadValue(0)); // Flags
        Assert.AreEqual(12345, table[0].ReadValue(1)); // Value
        Assert.IsTrue((bool?)table[0].ReadValue(2)); // IsEnabled (0x53 & 0x01 = 0x01)
        Assert.IsTrue((bool?)table[0].ReadValue(3)); // IsReadOnly (0x53 & 0x02 = 0x02)
        Assert.AreEqual(5, table[0].ReadValue(4)); // Priority (0x53 >> 4 = 5)
        Assert.AreEqual(0xD3, table[0].ReadValue(5)); // CombinedFlags (0x53 | 0x80 = 0xD3)
    }

    [TestMethod]
    public void Query_SelectInterpret_WithBitwiseXorAndShift_ShouldCalculate()
    {
        // Arrange: Test XOR and shift operations
        var query = (
            "\n"
            + "            binary BitwiseData {\n"
            + "                A: int le,\n"
            + "                B: int le,\n"
            + "                Xor: A ^ B,\n"
            + "                LeftShift: A << 2,\n"
            + "                RightShift: B >> 1,\n"
            + "                Combined: (A & 0xFF) | ((B & 0xFF) << 8)\n"
            + "            };\n"
            + "            select\n"
            + "                d.A,\n"
            + "                d.B,\n"
            + "                d.Xor,\n"
            + "                d.LeftShift,\n"
            + "                d.RightShift,\n"
            + "                d.Combined\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<BitwiseData>(f.Content) d"
        );

        var testData = new byte[8];
        BitConverter.GetBytes(0x0F).CopyTo(testData, 0); // A = 15 (0x0F)
        BitConverter.GetBytes(0xF0).CopyTo(testData, 4); // B = 240 (0xF0)
        var entities = new[]
        {
            new BinaryEntity { Name = "bitwise.bin", Content = testData },
        };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } }
        );

        // Act
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions
        );

        var table = vm.Run(CancellationToken.None);

        // Assert
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0x0F, table[0].ReadValue(0)); // A = 15
        Assert.AreEqual(0xF0, table[0].ReadValue(1)); // B = 240
        Assert.AreEqual(0xFF, table[0].ReadValue(2)); // Xor = 15 ^ 240 = 255
        Assert.AreEqual(0x3C, table[0].ReadValue(3)); // LeftShift = 15 << 2 = 60
        Assert.AreEqual(0x78, table[0].ReadValue(4)); // RightShift = 240 >> 1 = 120
        Assert.AreEqual(0xF00F, table[0].ReadValue(5)); // Combined = 15 | (240 << 8) = 61455
    }
}
