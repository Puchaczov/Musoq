using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualCoreBinaryTests
{
    [TestMethod]
    public void Query_SelectInterpret_WithComputedField_ShouldCalculateValue()
    {
        // Arrange: Rectangle with computed Area field
        var query = @"
            binary Rectangle {
                Width: int le,
                Height: int le,
                Area: Width * Height
            };
            select
                r.Width,
                r.Height,
                r.Area
            from #test.files() f
            cross apply Interpret<Rectangle>(f.Content) r";

        var testData = new byte[8];
        BitConverter.GetBytes(10).CopyTo(testData, 0); // Width = 10
        BitConverter.GetBytes(5).CopyTo(testData, 4); // Height = 5
        var entities = new[] { new BinaryEntity { Name = "rect.bin", Content = testData } };

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
        Assert.AreEqual(10, table[0][0]); // Width
        Assert.AreEqual(5, table[0][1]); // Height
        Assert.AreEqual(50, table[0][2]); // Area = 10 * 5
    }

    [TestMethod]
    public void Query_SelectInterpret_WithComputedBoolField_ShouldFilterByComputed()
    {
        // Arrange: Packet with computed IsLarge field used in WHERE
        var query = @"
            binary Packet {
                Size: int le,
                IsLarge: Size > 1000
            };
            select
                f.Name,
                p.Size
            from #test.files() f
            cross apply Interpret<Packet>(f.Content) p
            where p.IsLarge = true
            order by p.Size desc";

        var entities = new[]
        {
            new BinaryEntity { Name = "small.bin", Content = BitConverter.GetBytes(100) },
            new BinaryEntity { Name = "medium.bin", Content = BitConverter.GetBytes(500) },
            new BinaryEntity { Name = "large.bin", Content = BitConverter.GetBytes(2000) },
            new BinaryEntity { Name = "huge.bin", Content = BitConverter.GetBytes(5000) }
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

        // Assert: Only packets with Size > 1000
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("huge.bin", table[0][0]);
        Assert.AreEqual(5000, table[0][1]);
        Assert.AreEqual("large.bin", table[1][0]);
        Assert.AreEqual(2000, table[1][1]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithMultipleComputedFields_ShouldCalculateAll()
    {
        // Arrange: Multiple derived calculations
        var query = @"
            binary Metrics {
                ValueA: int le,
                ValueB: int le,
                Sum: ValueA + ValueB,
                Diff: ValueA - ValueB,
                Product: ValueA * ValueB
            };
            select
                m.ValueA,
                m.ValueB,
                m.Sum,
                m.Diff,
                m.Product
            from #test.files() f
            cross apply Interpret<Metrics>(f.Content) m";

        var testData = new byte[8];
        BitConverter.GetBytes(15).CopyTo(testData, 0); // ValueA = 15
        BitConverter.GetBytes(7).CopyTo(testData, 4); // ValueB = 7
        var entities = new[] { new BinaryEntity { Name = "metrics.bin", Content = testData } };

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
        Assert.AreEqual(15, table[0][0]); // ValueA
        Assert.AreEqual(7, table[0][1]); // ValueB
        Assert.AreEqual(22, table[0][2]); // Sum = 15 + 7
        Assert.AreEqual(8, table[0][3]); // Diff = 15 - 7
        Assert.AreEqual(105, table[0][4]); // Product = 15 * 7
    }
}
