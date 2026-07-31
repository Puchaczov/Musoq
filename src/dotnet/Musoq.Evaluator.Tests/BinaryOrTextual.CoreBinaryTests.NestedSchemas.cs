using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualCoreBinaryTests
{
    [TestMethod]
    public void Query_SelectInterpret_WithNestedSchema_ShouldParseNestedFields()
    {
        // Arrange: Vertex contains nested Point with deep property access (v.Position.X)
        var query = @"
            binary Point {
                X: float le,
                Y: float le
            };
            binary Vertex {
                Id: int le,
                Position: Point
            };
            select
                v.Id,
                v.Position.X,
                v.Position.Y
            from #test.files() f
            cross apply Interpret<Vertex>(f.Content) v";

        // Test data: Id (4 bytes) + Position.X (4 bytes) + Position.Y (4 bytes)
        using var ms = new MemoryStream();
        ms.Write(BitConverter.GetBytes(42)); // Id = 42
        ms.Write(BitConverter.GetBytes(1.5f)); // Position.X = 1.5
        ms.Write(BitConverter.GetBytes(2.5f)); // Position.Y = 2.5
        var testData = ms.ToArray();

        var entities = new[] { new BinaryEntity { Name = "vertex.bin", Content = testData } };

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
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("v.Id", typeof(int)),
            ("v.Position.X", typeof(float)),
            ("v.Position.Y", typeof(float)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [42, 1.5f, 2.5f]);
    }

    [TestMethod]
    public void Query_SelectInterpret_WithSchemaArray_ShouldParseArrayOfSchemas()
    {
        // Arrange: Mesh contains array of Points
        var query = @"
            binary Point {
                X: float le,
                Y: float le
            };
            binary Mesh {
                VertexCount: int le,
                Vertices: Point[VertexCount]
            };
            select
                m.VertexCount,
                m.Vertices[0].X,
                m.Vertices[0].Y,
                m.Vertices[1].X,
                m.Vertices[1].Y
            from #test.files() f
            cross apply Interpret<Mesh>(f.Content) m";

        // Test data: VertexCount = 2, followed by 2 Points
        using var ms = new MemoryStream();
        ms.Write(BitConverter.GetBytes(2)); // VertexCount = 2
        ms.Write(BitConverter.GetBytes(1.0f)); // Vertices[0].X
        ms.Write(BitConverter.GetBytes(2.0f)); // Vertices[0].Y
        ms.Write(BitConverter.GetBytes(3.0f)); // Vertices[1].X
        ms.Write(BitConverter.GetBytes(4.0f)); // Vertices[1].Y
        var testData = ms.ToArray();

        var entities = new[] { new BinaryEntity { Name = "mesh.bin", Content = testData } };

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
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("m.VertexCount", typeof(int)),
            ("m.Vertices[0].X", typeof(float)),
            ("m.Vertices[0].Y", typeof(float)),
            ("m.Vertices[1].X", typeof(float)),
            ("m.Vertices[1].Y", typeof(float)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [2, 1.0f, 2.0f, 3.0f, 4.0f]);
    }


}
