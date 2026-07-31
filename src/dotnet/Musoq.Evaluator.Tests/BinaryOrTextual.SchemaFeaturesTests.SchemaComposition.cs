using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualSchemaFeaturesTests
{
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
            cross apply Interpret<ExtendedHeader>(f.Content) h";

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
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("h.Magic", typeof(int)),
            ("h.Version", typeof(byte)),
            ("h.Flags", typeof(byte)),
            ("h.Length", typeof(short)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [0x474E5089, (byte)1, (byte)0xFF, (short)4096]);
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
            cross apply Interpret<Level3>(f.Content) l";

        var testData = new byte[] { 0x01, 0x02, 0x03 };
        var entities = new[] { new BinaryEntity { Name = "levels.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });


        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);


        TableMaterializationTestHelper.AssertColumns(
            table,
            ("l.A", typeof(byte)),
            ("l.B", typeof(byte)),
            ("l.C", typeof(byte)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)1, (byte)2, (byte)3]);
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
            cross apply Interpret<DerivedValue>(f.Content) d";

        var testData = BitConverter.GetBytes(25);
        var entities = new[] { new BinaryEntity { Name = "derived.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });


        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);


        TableMaterializationTestHelper.AssertColumns(
            table,
            ("d.Value", typeof(int)),
            ("d.Doubled", typeof(int)),
            ("d.IsPositive", typeof(bool)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [25, 50, true]);
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
            cross apply Interpret<OptionalData>(f.Content) o";

        // HasExtra=1, ExtraData=0x12345678, Value=42
        var testData = new byte[9];
        testData[0] = 1; // HasExtra = 1
        BitConverter.GetBytes(0x12345678).CopyTo(testData, 1); // ExtraData
        BitConverter.GetBytes(42).CopyTo(testData, 5); // Value

        var entities = new[] { new BinaryEntity { Name = "with_extra.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("o.HasExtra", typeof(byte)),
            ("o.ExtraData", typeof(int?)),
            ("o.Value", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)1, 0x12345678, 42]);
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
            cross apply Interpret<OptionalData>(f.Content) o";

        // HasExtra=0, Value=42 (no ExtraData)
        var testData = new byte[5];
        testData[0] = 0; // HasExtra = 0
        BitConverter.GetBytes(42).CopyTo(testData, 1); // Value immediately follows

        var entities = new[] { new BinaryEntity { Name = "no_extra.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("o.HasExtra", typeof(byte)),
            ("o.ExtraData", typeof(int?)),
            ("o.Value", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)0, null, 42]);
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
            cross apply Interpret<Record>(f.Content) r
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
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("f.Name", typeof(string)),
            ("r.Type", typeof(byte)),
            ("r.ExtendedInfo", typeof(short?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["extended.bin", (byte)1, (short)0x1234]);
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
            cross apply Interpret<FileHeader>(f.Content) h";

        // Magic = "EXIF" in little-endian (0x46495845)
        var testData = new byte[9];
        BitConverter.GetBytes(0x46495845).CopyTo(testData, 0); // Magic = "EXIF"
        testData[4] = 2; // Version = 2
        BitConverter.GetBytes(1024).CopyTo(testData, 5); // Size = 1024

        var entities = new[] { new BinaryEntity { Name = "valid.exif", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("h.Version", typeof(byte)),
            ("h.Size", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)2, 1024]);
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
            cross apply Interpret<FileHeader>(f.Content) h";

        // Magic = wrong value
        var testData = new byte[5];
        BitConverter.GetBytes(0xDEADBEEF).CopyTo(testData, 0); // Wrong magic
        testData[4] = 1;

        var entities = new[] { new BinaryEntity { Name = "invalid.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        // Assert: Should throw validation exception
        Assert.Throws<Exception>(() => _ = vm.Run(CancellationToken.None).Count);
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
            cross apply Interpret<VersionedData>(f.Content) v";

        var testData = new byte[5];
        testData[0] = 5; // Version = 5 (valid: 1-10)
        BitConverter.GetBytes(12345).CopyTo(testData, 1);

        var entities = new[] { new BinaryEntity { Name = "v5.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("v.Version", typeof(byte)),
            ("v.Data", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)5, 12345]);
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
            cross apply Interpret<IndexedFile>(f.Content) i";

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
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("i.HeaderSize", typeof(int)),
            ("i.DataOffset", typeof(int)),
            ("i.Data", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [8, 16, 42]);
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
            cross apply Interpret<ConditionalOffset>(f.Content) c";

        // HasData=1, DataOffset=10, ...padding..., Data=999 at offset 10
        var testData = new byte[14];
        testData[0] = 1; // HasData
        BitConverter.GetBytes(10).CopyTo(testData, 1); // DataOffset
        BitConverter.GetBytes(999).CopyTo(testData, 10); // Data at offset 10

        var entities = new[] { new BinaryEntity { Name = "conditional_offset.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("c.HasData", typeof(byte)),
            ("c.DataOffset", typeof(int)),
            ("c.Data", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)1, 10, 999]);
    }

    #endregion
}
