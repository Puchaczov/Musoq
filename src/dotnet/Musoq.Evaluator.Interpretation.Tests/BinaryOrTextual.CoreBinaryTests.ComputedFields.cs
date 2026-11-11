using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualCoreBinaryTests
{
    [TestMethod]
    public void Query_SelectInterpret_WithComputedField_ShouldCalculateValue()
    {
        // Arrange: Rectangle with computed Area field
        var query = (
            "\n"
            + "            binary Rectangle {\n"
            + "                Width: int le,\n"
            + "                Height: int le,\n"
            + "                Area: Width * Height\n"
            + "            };\n"
            + "            select\n"
            + "                r.Width,\n"
            + "                r.Height,\n"
            + "                r.Area\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<Rectangle>(f.Content) r"
        );

        var testData = new byte[8];
        BitConverter.GetBytes(10).CopyTo(testData, 0); // Width = 10
        BitConverter.GetBytes(5).CopyTo(testData, 4); // Height = 5
        var entities = new[]
        {
            new BinaryEntity { Name = "rect.bin", Content = testData },
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
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Width", typeof(int)),
            ("Height", typeof(int)),
            ("Area", typeof(int))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, [10, 5, 50]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithComputedBoolField_ShouldFilterByComputed()
    {
        // Arrange: Packet with computed IsLarge field used in WHERE
        var query = (
            "\n"
            + "            binary Packet {\n"
            + "                Size: int le,\n"
            + "                IsLarge: Size > 1000\n"
            + "            };\n"
            + "            select\n"
            + "                f.Name,\n"
            + "                p.Size\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<Packet>(f.Content) p\n"
            + "            where p.IsLarge = true\n"
            + "            order by p.Size desc"
        );

        var entities = new[]
        {
            new BinaryEntity { Name = "small.bin", Content = BitConverter.GetBytes(100) },
            new BinaryEntity { Name = "medium.bin", Content = BitConverter.GetBytes(500) },
            new BinaryEntity { Name = "large.bin", Content = BitConverter.GetBytes(2000) },
            new BinaryEntity { Name = "huge.bin", Content = BitConverter.GetBytes(5000) },
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

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Size", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["huge.bin", 5000], ["large.bin", 2000]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithMultipleComputedFields_ShouldCalculateAll()
    {
        // Arrange: Multiple derived calculations
        var query = (
            "\n"
            + "            binary Metrics {\n"
            + "                ValueA: int le,\n"
            + "                ValueB: int le,\n"
            + "                Sum: ValueA + ValueB,\n"
            + "                Diff: ValueA - ValueB,\n"
            + "                Product: ValueA * ValueB\n"
            + "            };\n"
            + "            select\n"
            + "                m.ValueA,\n"
            + "                m.ValueB,\n"
            + "                m.Sum,\n"
            + "                m.Diff,\n"
            + "                m.Product\n"
            + "            from #test.files() f\n"
            + "            cross apply Interpret<Metrics>(f.Content) m"
        );

        var testData = new byte[8];
        BitConverter.GetBytes(15).CopyTo(testData, 0); // ValueA = 15
        BitConverter.GetBytes(7).CopyTo(testData, 4); // ValueB = 7
        var entities = new[]
        {
            new BinaryEntity { Name = "metrics.bin", Content = testData },
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
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("ValueA", typeof(int)),
            ("ValueB", typeof(int)),
            ("Sum", typeof(int)),
            ("Diff", typeof(int)),
            ("Product", typeof(int))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, [15, 7, 22, 8, 105]);
    }
}
