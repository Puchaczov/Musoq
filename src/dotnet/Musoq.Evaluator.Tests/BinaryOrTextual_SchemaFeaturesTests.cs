#nullable enable annotations

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Plugins;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class BinaryOrTextual_SchemaFeaturesTests : BinaryOrTextualEvaluatorTestBase
{
    #region TryInterpret/TryParse E2E Tests

    [TestMethod]
    public void Query_SelectTryInterpret_WithValidData_ShouldReturnResult()
    {
        // Arrange: TryInterpret with valid data
        var query = @"
            binary SimpleHeader {
                Magic: int le,
                Version: byte
            };
            select
                d.Magic,
                d.Version
            from #test.files() f
            cross apply TryInterpret(f.Content, 'SimpleHeader') d";

        var testData = new byte[5];
        BitConverter.GetBytes(0x12345678).CopyTo(testData, 0); // Magic
        testData[4] = 1; // Version
        var entities = new[] { new BinaryEntity { Name = "valid.bin", Content = testData } };

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
        Assert.AreEqual(0x12345678, table[0][0]); // Magic
        Assert.AreEqual((byte)1, table[0][1]); // Version
    }

    [TestMethod]
    public void Query_SelectTryInterpret_WithInvalidData_ShouldReturnNullValues()
    {
        // Arrange: TryInterpret with insufficient data returns null for field values
        // Using OUTER APPLY, the row is preserved with null values for the parsed fields
        var query = @"
            binary SimpleValue {
                Value: int le
            };
            select
                f.Name,
                d.Value
            from #test.files() f
            outer apply TryInterpret(f.Content, 'SimpleValue') d";

        // This data is invalid - only 2 bytes for a 4-byte int
        var testData = new byte[] { 0x01, 0x02 };
        var entities = new[] { new BinaryEntity { Name = "invalid.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        // Assert - Row is present but Value is null due to failed parse
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("invalid.bin", table[0][0]);
        Assert.IsNull(table[0][1]); // Value is null because TryInterpret failed
    }

    [TestMethod]
    public void Query_SelectTryInterpret_WithMixedData_CrossApply_ExcludesFailedParses()
    {
        // Arrange: Mix of valid and invalid files
        // With TryInterpret + CROSS APPLY, failed parses (null result) are excluded from results
        var query = @"
            binary MagicHeader {
                Magic: int le
            };
            select
                f.Name,
                d.Magic
            from #test.files() f
            cross apply TryInterpret(f.Content, 'MagicHeader') d";

        var validData = new byte[4];
        BitConverter.GetBytes(0xDEADBEEF).CopyTo(validData, 0);
        var invalidData = new byte[] { 0x01 }; // Too short - TryInterpret returns null

        var entities = new[]
        {
            new BinaryEntity { Name = "valid.bin", Content = validData },
            new BinaryEntity { Name = "invalid.bin", Content = invalidData }
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

        // Assert - Only one row (valid.bin) since invalid.bin's TryInterpret returns null
        // which is filtered out by CROSS APPLY behavior
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("valid.bin", table[0][0]);
        Assert.AreEqual(unchecked((int)0xDEADBEEF), table[0][1]);
    }

    [TestMethod]
    public void Query_SelectTryInterpret_WithMixedData_OuterApply_IncludesAllRows()
    {
        // Arrange: Mix of valid and invalid files
        // With TryInterpret + OUTER APPLY, failed parses return rows with null field values
        var query = @"
            binary MagicHeader {
                Magic: int le
            };
            select
                f.Name,
                d.Magic
            from #test.files() f
            outer apply TryInterpret(f.Content, 'MagicHeader') d
            order by f.Name asc";

        var validData = new byte[4];
        BitConverter.GetBytes(0xDEADBEEF).CopyTo(validData, 0);
        var invalidData = new byte[] { 0x01 }; // Too short - TryInterpret returns null

        var entities = new[]
        {
            new BinaryEntity { Name = "invalid.bin", Content = invalidData },
            new BinaryEntity { Name = "valid.bin", Content = validData }
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

        // Assert - Both rows present, invalid.bin has null Magic value
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("invalid.bin", table[0][0]);
        Assert.IsNull(table[0][1]); // Magic is null for failed parse
        Assert.AreEqual("valid.bin", table[1][0]);
        Assert.AreEqual(unchecked((int)0xDEADBEEF), table[1][1]);
    }

    /// <summary>
    ///     Session 5: Tests TryInterpret with empty data returns null.
    /// </summary>
    [TestMethod]
    public void Query_TryInterpret_WithEmptyData_ShouldReturnNull()
    {
        var query = @"
            binary Header {
                Magic: int le
            };
            select
                f.Name,
                d.Magic
            from #test.files() f
            outer apply TryInterpret(f.Content, 'Header') d";

        var emptyData = Array.Empty<byte>();
        var entities = new[] { new BinaryEntity { Name = "empty.bin", Content = emptyData } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("empty.bin", table[0][0]);
        Assert.IsNull(table[0][1]);
    }

    /// <summary>
    ///     Session 5: Tests TryInterpret with all invalid data returns no rows with CROSS APPLY.
    /// </summary>
    [TestMethod]
    public void Query_TryInterpret_AllInvalid_CrossApply_ReturnsNoRows()
    {
        var query = @"
            binary Header {
                Value: int le
            };
            select d.Value
            from #test.files() f
            cross apply TryInterpret(f.Content, 'Header') d";

        var entities = new[]
        {
            new BinaryEntity { Name = "a.bin", Content = [0x01] },
            new BinaryEntity { Name = "b.bin", Content = [0x02, 0x03] }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(0, table.Count);
    }

    /// <summary>
    ///     Session 5: Tests counting valid vs invalid parses with TryInterpret.
    /// </summary>
    [TestMethod]
    public void Query_TryInterpret_CountValidAndInvalid_ShouldCountCorrectly()
    {
        var query = @"
            binary Header {
                Value: short le
            };
            select
                Count(d.Value) as ValidCount
            from #test.files() f
            outer apply TryInterpret(f.Content, 'Header') d";

        var entities = new[]
        {
            new BinaryEntity { Name = "valid1.bin", Content = [0x01, 0x00] },
            new BinaryEntity { Name = "valid2.bin", Content = [0x02, 0x00] },
            new BinaryEntity { Name = "invalid.bin", Content = [0x01] }
        };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(2, table[0][0]);
    }

    #endregion

    #region Session 8: Code Generation Coverage Tests

    /// <summary>
    ///     Tests that conditional value type fields become nullable.
    /// </summary>
    [TestMethod]
    public void Query_SelectInterpret_ConditionalValueType_ShouldBeNullable()
    {
        var query = @"
            binary Message {
                HasValue: byte,
                Value: int le when HasValue <> 0
            };
            select
                m.HasValue,
                m.Value
            from #test.files() f
            cross apply Interpret(f.Content, 'Message') m";


        var data = new byte[] { 0x00 };
        var entities = new[] { new BinaryEntity { Name = "msg.bin", Content = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0, table[0][0]);
        Assert.IsNull(table[0][1]);
    }

    /// <summary>
    ///     Tests nested schema property access in SELECT.
    /// </summary>
    [TestMethod]
    public void Query_SelectInterpret_NestedSchema_ShouldAccessNestedProperties()
    {
        var query = @"
            binary Inner {
                X: short le,
                Y: short le
            };
            binary Outer {
                Id: byte,
                Point: Inner
            };
            select
                o.Id,
                o.Point.X,
                o.Point.Y
            from #test.files() f
            cross apply Interpret(f.Content, 'Outer') o";

        var data = new byte[]
        {
            0x42,
            0x0A, 0x00,
            0x14, 0x00
        };
        var entities = new[] { new BinaryEntity { Name = "data.bin", Content = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x42, table[0][0]);
        Assert.AreEqual((short)10, table[0][1]);
        Assert.AreEqual((short)20, table[0][2]);
    }

    #endregion

    #region Session 9: Real-World Format Tests

    /// <summary>
    ///     Tests parsing a simple TLV (Type-Length-Value) structure.
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_TlvStructure_ShouldParse()
    {
        var query = @"
            binary TlvRecord {
                Type: byte,
                Length: byte,
                Value: byte[Length]
            };
            select
                t.Type,
                t.Length,
                t.Value
            from #test.files() f
            cross apply Interpret(f.Content, 'TlvRecord') t";

        var data = new byte[]
        {
            0x01,
            0x03,
            0xAA, 0xBB, 0xCC
        };
        var entities = new[] { new BinaryEntity { Name = "tlv.bin", Content = data } };
        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0x01, table[0][0]);
        Assert.AreEqual((byte)0x03, table[0][1]);
        var value = (byte[])table[0][2];
        Assert.HasCount(3, value);
        Assert.AreEqual((byte)0xAA, value[0]);
    }

    /// <summary>
    ///     Tests parsing a log line with timestamp and message.
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_SimpleLogLine_ShouldParse()
    {
        var query = @"
            text LogLine {
                Timestamp: until ' ',
                Level: until ' ',
                Message: rest
            };
            select
                l.Timestamp,
                l.Level,
                l.Message
            from #test.lines() f
            cross apply Parse(f.Line, 'LogLine') l";

        var entities = new[]
            { new TextEntity { Name = "log.txt", Text = "2024-01-15T10:30:00 INFO Application started successfully" } };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("2024-01-15T10:30:00", table[0][0]);
        Assert.AreEqual("INFO", table[0][1]);
        Assert.AreEqual("Application started successfully", table[0][2]);
    }

    /// <summary>
    ///     Tests parsing environment variable format.
    /// </summary>
    [TestMethod]
    public void Query_RealWorld_EnvVariable_ShouldParse()
    {
        var query = @"
            text EnvVar {
                Name: until '=',
                Value: rest
            };
            select
                e.Name,
                e.Value
            from #test.lines() f
            cross apply Parse(f.Line, 'EnvVar') e";

        var entities = new[]
        {
            new TextEntity { Name = "env1", Text = "PATH=/usr/bin:/usr/local/bin" },
            new TextEntity { Name = "env2", Text = "HOME=/home/user" }
        };
        var schemaProvider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { { "#test", entities } });

        var vm = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, LoggerResolver,
            TestCompilationOptions);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(2, table.Count);
        var names = new HashSet<string> { (string)table[0][0], (string)table[1][0] };
        var values = new HashSet<string> { (string)table[0][1], (string)table[1][1] };
        Assert.Contains("PATH", names);
        Assert.Contains("HOME", names);
        Assert.Contains("/usr/bin:/usr/local/bin", values);
        Assert.Contains("/home/user", values);
    }

    #endregion

    #region Schema Inheritance E2E Tests

    [TestMethod]
    public void Query_SelectInterpret_WithSchemaInheritance_ShouldIncludeParentFields()
    {
        // Arrange: Child extends Parent
        var query = @"
            binary BaseHeader {
                Magic: int le,
                Version: byte
            };
            binary ExtendedHeader extends BaseHeader {
                Flags: byte,
                Length: short le
            };
            select
                h.Magic,
                h.Version,
                h.Flags,
                h.Length
            from #test.files() f
            cross apply Interpret(f.Content, 'ExtendedHeader') h";

        // Magic (4) + Version (1) + Flags (1) + Length (2) = 8 bytes
        var testData = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, // Magic = "PNG" signature bytes (0x474E5089 in LE)
            0x01, // Version = 1
            0xFF, // Flags = 0xFF
            0x00, 0x10 // Length = 4096 (little-endian)
        };
        var entities = new[] { new BinaryEntity { Name = "extended.bin", Content = testData } };

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
        Assert.AreEqual(0x474E5089, table[0][0]); // Magic (from parent)
        Assert.AreEqual((byte)1, table[0][1]); // Version (from parent)
        Assert.AreEqual((byte)0xFF, table[0][2]); // Flags (from child)
        Assert.AreEqual((short)4096, table[0][3]); // Length (from child)
    }

    [TestMethod]
    public void Query_SelectInterpret_WithMultiLevelInheritance_ShouldIncludeAllAncestors()
    {
        var query = @"
            binary Level1 {
                A: byte
            };
            binary Level2 extends Level1 {
                B: byte
            };
            binary Level3 extends Level2 {
                C: byte
            };
            select
                l.A,
                l.B,
                l.C
            from #test.files() f
            cross apply Interpret(f.Content, 'Level3') l";

        var testData = new byte[] { 0x01, 0x02, 0x03 };
        var entities = new[] { new BinaryEntity { Name = "levels.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });


        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);


        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual((byte)2, table[0][1]);
        Assert.AreEqual((byte)3, table[0][2]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithInheritanceAndComputedField_ShouldAccessParentFields()
    {
        var query = @"
            binary BaseValue {
                Value: int le
            };
            binary DerivedValue extends BaseValue {
                Doubled: Value * 2,
                IsPositive: Value > 0
            };
            select
                d.Value,
                d.Doubled,
                d.IsPositive
            from #test.files() f
            cross apply Interpret(f.Content, 'DerivedValue') d";

        var testData = BitConverter.GetBytes(25);
        var entities = new[] { new BinaryEntity { Name = "derived.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });


        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);


        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(25, table[0][0]);
        Assert.AreEqual(50, table[0][1]);
        Assert.IsTrue((bool?)table[0][2]);
    }

    #endregion

    #region Conditional Fields E2E Tests

    [TestMethod]
    public void Query_SelectInterpret_WithConditionalField_ShouldParseWhenTrue()
    {
        // Arrange: Optional field based on flag
        var query = @"
            binary OptionalData {
                HasExtra: byte,
                ExtraData: int le when HasExtra = 1,
                Value: int le
            };
            select
                o.HasExtra,
                o.ExtraData,
                o.Value
            from #test.files() f
            cross apply Interpret(f.Content, 'OptionalData') o";

        // HasExtra=1, ExtraData=0x12345678, Value=42
        var testData = new byte[9];
        testData[0] = 1; // HasExtra = 1
        BitConverter.GetBytes(0x12345678).CopyTo(testData, 1); // ExtraData
        BitConverter.GetBytes(42).CopyTo(testData, 5); // Value

        var entities = new[] { new BinaryEntity { Name = "with_extra.bin", Content = testData } };

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
        Assert.AreEqual((byte)1, table[0][0]); // HasExtra
        Assert.AreEqual(0x12345678, table[0][1]); // ExtraData is present
        Assert.AreEqual(42, table[0][2]); // Value
    }

    [TestMethod]
    public void Query_SelectInterpret_WithConditionalField_ShouldBeNullWhenFalse()
    {
        // Arrange: Optional field skipped when condition is false
        var query = @"
            binary OptionalData {
                HasExtra: byte,
                ExtraData: int le when HasExtra = 1,
                Value: int le
            };
            select
                o.HasExtra,
                o.ExtraData,
                o.Value
            from #test.files() f
            cross apply Interpret(f.Content, 'OptionalData') o";

        // HasExtra=0, Value=42 (no ExtraData)
        var testData = new byte[5];
        testData[0] = 0; // HasExtra = 0
        BitConverter.GetBytes(42).CopyTo(testData, 1); // Value immediately follows

        var entities = new[] { new BinaryEntity { Name = "no_extra.bin", Content = testData } };

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
        Assert.AreEqual((byte)0, table[0][0]); // HasExtra
        Assert.IsNull(table[0][1]); // ExtraData is null
        Assert.AreEqual(42, table[0][2]); // Value
    }

    [TestMethod]
    public void Query_SelectInterpret_WithConditionalField_ShouldFilterByCondition()
    {
        // Arrange: Filter records by whether optional field condition is true
        var query = @"
            binary Record {
                Type: byte,
                ExtendedInfo: short le when Type > 0,
                Data: int le
            };
            select
                f.Name,
                r.Type,
                r.ExtendedInfo
            from #test.files() f
            cross apply Interpret(f.Content, 'Record') r
            where r.Type > 0
            order by f.Name";

        var entities = new[]
        {
            // Type=0, Data only (5 bytes)
            new BinaryEntity { Name = "simple.bin", Content = [0x00, 0x01, 0x00, 0x00, 0x00] },
            // Type=1, ExtendedInfo=0x1234, Data (7 bytes)
            new BinaryEntity
                { Name = "extended.bin", Content = [0x01, 0x34, 0x12, 0x02, 0x00, 0x00, 0x00] }
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

        // Assert: Only extended.bin has ExtendedInfo
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("extended.bin", table[0][0]);
        Assert.AreEqual((byte)1, table[0][1]);
        Assert.AreEqual((short)0x1234, table[0][2]);
    }

    #endregion

    #region Check Constraints E2E Tests

    [TestMethod]
    public void Query_SelectInterpret_WithCheckConstraint_ShouldValidateMagicNumber()
    {
        // Arrange: File format with magic number validation
        var query = @"
            binary FileHeader {
                Magic: int le check Magic = 0x46495845,
                Version: byte,
                Size: int le
            };
            select
                h.Version,
                h.Size
            from #test.files() f
            cross apply Interpret(f.Content, 'FileHeader') h";

        // Magic = "EXIF" in little-endian (0x46495845)
        var testData = new byte[9];
        BitConverter.GetBytes(0x46495845).CopyTo(testData, 0); // Magic = "EXIF"
        testData[4] = 2; // Version = 2
        BitConverter.GetBytes(1024).CopyTo(testData, 5); // Size = 1024

        var entities = new[] { new BinaryEntity { Name = "valid.exif", Content = testData } };

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
        Assert.AreEqual((byte)2, table[0][0]); // Version
        Assert.AreEqual(1024, table[0][1]); // Size
    }

    [TestMethod]
    public void Query_SelectInterpret_WithCheckConstraint_ShouldThrowOnInvalidMagic()
    {
        // Arrange: File with invalid magic number
        var query = @"
            binary FileHeader {
                Magic: int le check Magic = 0x46495845,
                Version: byte
            };
            select
                h.Version
            from #test.files() f
            cross apply Interpret(f.Content, 'FileHeader') h";

        // Magic = wrong value
        var testData = new byte[5];
        BitConverter.GetBytes(0xDEADBEEF).CopyTo(testData, 0); // Wrong magic
        testData[4] = 1;

        var entities = new[] { new BinaryEntity { Name = "invalid.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        // Assert: Should throw validation exception
        Assert.Throws<Exception>(() => vm.Run(CancellationToken.None));
    }

    [TestMethod]
    public void Query_SelectInterpret_WithRangeCheck_ShouldValidateRange()
    {
        // Arrange: Version must be between 1 and 10
        var query = @"
            binary VersionedData {
                Version: byte check Version >= 1 and Version <= 10,
                Data: int le
            };
            select
                v.Version,
                v.Data
            from #test.files() f
            cross apply Interpret(f.Content, 'VersionedData') v";

        var testData = new byte[5];
        testData[0] = 5; // Version = 5 (valid: 1-10)
        BitConverter.GetBytes(12345).CopyTo(testData, 1);

        var entities = new[] { new BinaryEntity { Name = "v5.bin", Content = testData } };

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
        Assert.AreEqual((byte)5, table[0][0]);
        Assert.AreEqual(12345, table[0][1]);
    }

    #endregion

    #region At Positioning E2E Tests

    [TestMethod]
    public void Query_SelectInterpret_WithAtPosition_ShouldSeekToOffset()
    {
        // Arrange: Read field at specific offset
        var query = @"
            binary IndexedFile {
                HeaderSize: int le,
                DataOffset: int le,
                Data: int le at DataOffset
            };
            select
                i.HeaderSize,
                i.DataOffset,
                i.Data
            from #test.files() f
            cross apply Interpret(f.Content, 'IndexedFile') i";

        // Header: HeaderSize=8, DataOffset=16
        // ...padding...
        // At offset 16: Data=42
        var testData = new byte[20];
        BitConverter.GetBytes(8).CopyTo(testData, 0); // HeaderSize
        BitConverter.GetBytes(16).CopyTo(testData, 4); // DataOffset
        BitConverter.GetBytes(42).CopyTo(testData, 16); // Data at offset 16

        var entities = new[] { new BinaryEntity { Name = "indexed.bin", Content = testData } };

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
        Assert.AreEqual(8, table[0][0]); // HeaderSize
        Assert.AreEqual(16, table[0][1]); // DataOffset
        Assert.AreEqual(42, table[0][2]); // Data read from offset 16
    }

    [TestMethod]
    public void Query_SelectInterpret_WithAtPositionAndCondition_ShouldCombineModifiers()
    {
        // Arrange: Conditional field at specific position
        var query = @"
            binary ConditionalOffset {
                HasData: byte,
                DataOffset: int le,
                Data: int le at DataOffset when HasData = 1
            };
            select
                c.HasData,
                c.DataOffset,
                c.Data
            from #test.files() f
            cross apply Interpret(f.Content, 'ConditionalOffset') c";

        // HasData=1, DataOffset=10, ...padding..., Data=999 at offset 10
        var testData = new byte[14];
        testData[0] = 1; // HasData
        BitConverter.GetBytes(10).CopyTo(testData, 1); // DataOffset
        BitConverter.GetBytes(999).CopyTo(testData, 10); // Data at offset 10

        var entities = new[] { new BinaryEntity { Name = "conditional_offset.bin", Content = testData } };

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
        Assert.AreEqual((byte)1, table[0][0]); // HasData
        Assert.AreEqual(10, table[0][1]); // DataOffset
        Assert.AreEqual(999, table[0][2]); // Data
    }

    #endregion

    #region Aggregation and Grouping E2E Tests

    [TestMethod]
    public void Query_SelectInterpret_WithAggregation_ShouldSumValues()
    {
        var query = @"
            binary Amount {
                Value: int le
            };
            select
                Sum(a.Value) as TotalValue,
                Count(a.Value) as RecordCount,
                Avg(a.Value) as AvgValue
            from #test.files() f
            cross apply Interpret(f.Content, 'Amount') a";

        var entities = new[]
        {
            new BinaryEntity { Name = "a1.bin", Content = BitConverter.GetBytes(100) },
            new BinaryEntity { Name = "a2.bin", Content = BitConverter.GetBytes(200) },
            new BinaryEntity { Name = "a3.bin", Content = BitConverter.GetBytes(300) },
            new BinaryEntity { Name = "a4.bin", Content = BitConverter.GetBytes(400) }
        };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });


        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);


        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1000m, table[0][0]);
        Assert.AreEqual(4, table[0][1]);
        Assert.AreEqual(250m, table[0][2]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithGroupBy_ShouldGroupByField()
    {
        // Arrange: Group by category and sum values
        var query = @"
            binary Transaction {
                Category: byte,
                Amount: int le
            };
            select
                t.Category,
                Sum(t.Amount) as TotalAmount,
                Count(t.Amount) as TransactionCount
            from #test.files() f
            cross apply Interpret(f.Content, 'Transaction') t
            group by t.Category
            order by t.Category";

        var entities = new[]
        {
            // Category 1
            CreateTransactionEntity("t1.bin", 1, 100),
            CreateTransactionEntity("t2.bin", 1, 150),
            // Category 2
            CreateTransactionEntity("t3.bin", 2, 200),
            CreateTransactionEntity("t4.bin", 2, 250),
            CreateTransactionEntity("t5.bin", 2, 300),
            // Category 3
            CreateTransactionEntity("t6.bin", 3, 500)
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
        // Category 1: Sum=250, Count=2
        Assert.AreEqual((byte)1, table[0][0]);
        Assert.AreEqual(250m, table[0][1]);
        Assert.AreEqual(2, table[0][2]);
        // Category 2: Sum=750, Count=3
        Assert.AreEqual((byte)2, table[1][0]);
        Assert.AreEqual(750m, table[1][1]);
        Assert.AreEqual(3, table[1][2]);
        // Category 3: Sum=500, Count=1
        Assert.AreEqual((byte)3, table[2][0]);
        Assert.AreEqual(500m, table[2][1]);
        Assert.AreEqual(1, table[2][2]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithHaving_ShouldFilterGroups()
    {
        // Arrange: Filter groups by aggregate value
        var query = @"
            binary Sale {
                Region: byte,
                Amount: int le
            };
            select
                s.Region,
                Sum(s.Amount) as TotalSales
            from #test.files() f
            cross apply Interpret(f.Content, 'Sale') s
            group by s.Region
            having Sum(s.Amount) > 500
            order by Sum(s.Amount) desc";

        var entities = new[]
        {
            // Region 1: Total = 300 (excluded by HAVING)
            CreateTransactionEntity("s1.bin", 1, 100),
            CreateTransactionEntity("s2.bin", 1, 200),
            // Region 2: Total = 900
            CreateTransactionEntity("s3.bin", 2, 400),
            CreateTransactionEntity("s4.bin", 2, 500),
            // Region 3: Total = 600
            CreateTransactionEntity("s5.bin", 3, 250),
            CreateTransactionEntity("s6.bin", 3, 350)
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

        // Assert: Only regions with total > 500
        Assert.AreEqual(2, table.Count);
        // Region 2: 900
        Assert.AreEqual((byte)2, table[0][0]);
        Assert.AreEqual(900m, table[0][1]);
        // Region 3: 600
        Assert.AreEqual((byte)3, table[1][0]);
        Assert.AreEqual(600m, table[1][1]);
    }

    private static BinaryEntity CreateTransactionEntity(string name, byte category, int amount)
    {
        var data = new byte[5];
        data[0] = category;
        BitConverter.GetBytes(amount).CopyTo(data, 1);
        return new BinaryEntity { Name = name, Content = data };
    }

    #endregion
}
