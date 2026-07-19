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
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("r.Width", typeof(int)),
            ("r.Height", typeof(int)),
            ("r.Area", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [10, 5, 50]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("f.Name", typeof(string)),
            ("p.Size", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["huge.bin", 5000],
            ["large.bin", 2000]);
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
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("m.ValueA", typeof(int)),
            ("m.ValueB", typeof(int)),
            ("m.Sum", typeof(int)),
            ("m.Diff", typeof(int)),
            ("m.Product", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [15, 7, 22, 8, 105]);
    }
}
