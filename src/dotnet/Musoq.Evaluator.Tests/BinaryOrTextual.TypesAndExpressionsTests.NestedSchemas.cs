using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualTypesAndExpressionsTests
{
    #region Complex Nested Schema Tests

    [TestMethod]
    public void Query_SelectInterpret_WithNestedSchema_ShouldReturnNestedObject()
    {
        // Arrange: Nested schema - accessing the nested object itself
        var query = @"
            binary InnerData {
                Value: int le
            };
            binary OuterData {
                Id: byte,
                Child: InnerData
            };
            select
                h.Id,
                h.Child
            from #test.files() f
            cross apply Interpret<OuterData>(f.Content) h";

        // Id=0xAB, Value=0x12345678
        var testData = new byte[] { 0xAB, 0x78, 0x56, 0x34, 0x12 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        // Assert
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0xAB, table[0][0]);
        // h.Child is the InnerData interpreter object
        Assert.IsNotNull(table[0][1]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithDeepNestedPropertyAccess_ShouldWork()
    {
        // Arrange: Deep property access through nested schemas (h.Child.Value)
        var query = @"
            binary InnerData {
                Value: int le
            };
            binary OuterData {
                Id: byte,
                Child: InnerData
            };
            select
                h.Id,
                h.Child.Value
            from #test.files() f
            cross apply Interpret<OuterData>(f.Content) h";

        // Id=0xAB, Value=0x12345678
        var testData = new byte[] { 0xAB, 0x78, 0x56, 0x34, 0x12 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        // Assert
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0xAB, table[0][0]);
        Assert.AreEqual(0x12345678, table[0][1]); // The nested Value field
    }

    [TestMethod]
    public void Query_SelectInterpret_WithThreeLevelDeepPropertyAccess_ShouldWork()
    {
        // Arrange: Three levels deep property access (h.Middle.Inner.Value)
        var query = @"
            binary InnerData {
                Value: int le
            };
            binary MiddleData {
                Id: byte,
                Inner: InnerData
            };
            binary OuterData {
                Flags: byte,
                Middle: MiddleData
            };
            select
                h.Flags,
                h.Middle.Id,
                h.Middle.Inner.Value
            from #test.files() f
            cross apply Interpret<OuterData>(f.Content) h";

        // Flags=0xFF, Id=0xAB, Value=0x12345678
        var testData = new byte[] { 0xFF, 0xAB, 0x78, 0x56, 0x34, 0x12 };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        // Assert
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)0xFF, table[0][0]); // h.Flags
        Assert.AreEqual((byte)0xAB, table[0][1]); // h.Middle.Id
        Assert.AreEqual(0x12345678, table[0][2]); // h.Middle.Inner.Value
    }

    [TestMethod]
    public void Query_SelectInterpret_WithArrayOfNestedSchemas_ShouldParseAll()
    {
        // Arrange: Array of nested schemas
        var query = @"
            binary Item {
                Value: byte
            };
            binary Container {
                ItemCount: byte,
                Items: Item[ItemCount]
            };
            select
                h.ItemCount,
                h.Items
            from #test.files() f
            cross apply Interpret<Container>(f.Content) h";

        // ItemCount=3, Items: [0xAA, 0xBB, 0xCC]
        var testData = new byte[] { 0x03, 0xAA, 0xBB, 0xCC };
        var entities = new[] { new BinaryEntity { Name = "test.bin", Content = testData } };

        var schemaProvider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { { "#test", entities } });

        // Act
        var vm = CompileGeneratedQuery(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver, TestCompilationOptions);

        var table = vm.Run(CancellationToken.None);

        // Assert
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual((byte)3, table[0][0]);
        var items = (object[])table[0][1];
        Assert.HasCount(3, items);
    }

    #endregion
}
