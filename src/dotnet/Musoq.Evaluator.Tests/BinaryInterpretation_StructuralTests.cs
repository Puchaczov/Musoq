#nullable enable annotations

using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class BinaryInterpretation_StructuralTests : BinaryInterpretationTestBase
{
    [TestMethod]
    public void Interpret_NestedSchema_ShouldParseInline()
    {
        var registry = new SchemaRegistry();

        var pointFields = new[]
        {
            CreatePrimitiveField("X", PrimitiveTypeName.Float, Endianness.LittleEndian),
            CreatePrimitiveField("Y", PrimitiveTypeName.Float, Endianness.LittleEndian)
        };
        var pointSchema = new BinarySchemaNode("Point", pointFields);
        registry.Register("Point", pointSchema);

        var vertexFields = new[]
        {
            new FieldDefinitionNode("Id", new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian)),
            new FieldDefinitionNode("Position", new SchemaReferenceTypeNode("Point"))
        };
        var vertexSchema = new BinarySchemaNode("Vertex", vertexFields);
        registry.Register("Vertex", vertexSchema);

        var interpreter = CompileInterpreter(registry, "Vertex");

        var ms = new MemoryStream();
        ms.Write(BitConverter.GetBytes(42));
        ms.Write(BitConverter.GetBytes(1.5f));
        ms.Write(BitConverter.GetBytes(2.5f));
        var data = ms.ToArray();

        var result = InvokeInterpret(interpreter, data);
        var id = GetPropertyValue<int>(result, "Id");
        var position = GetPropertyValue<object>(result, "Position");
        var posX = GetPropertyValue<float>(position, "X");
        var posY = GetPropertyValue<float>(position, "Y");

        Assert.AreEqual(42, id);
        Assert.AreEqual(1.5f, posX, 0.001f);
        Assert.AreEqual(2.5f, posY, 0.001f);
    }

    [TestMethod]
    public void Interpret_MultipleNestedSchemas_ShouldParseSequentially()
    {
        var registry = new SchemaRegistry();

        var colorFields = new[]
        {
            CreatePrimitiveField("R", PrimitiveTypeName.Byte, Endianness.NotApplicable),
            CreatePrimitiveField("G", PrimitiveTypeName.Byte, Endianness.NotApplicable),
            CreatePrimitiveField("B", PrimitiveTypeName.Byte, Endianness.NotApplicable)
        };
        var colorSchema = new BinarySchemaNode("Color", colorFields);
        registry.Register("Color", colorSchema);

        var pointFields = new[]
        {
            CreatePrimitiveField("X", PrimitiveTypeName.Float, Endianness.LittleEndian),
            CreatePrimitiveField("Y", PrimitiveTypeName.Float, Endianness.LittleEndian)
        };
        var pointSchema = new BinarySchemaNode("Point", pointFields);
        registry.Register("Point", pointSchema);

        var vertexFields = new[]
        {
            new FieldDefinitionNode("Position", new SchemaReferenceTypeNode("Point")),
            new FieldDefinitionNode("Color", new SchemaReferenceTypeNode("Color"))
        };
        var vertexSchema = new BinarySchemaNode("Vertex", vertexFields);
        registry.Register("Vertex", vertexSchema);

        var interpreter = CompileInterpreter(registry, "Vertex");

        var ms = new MemoryStream();
        ms.Write(BitConverter.GetBytes(3.0f));
        ms.Write(BitConverter.GetBytes(4.0f));
        ms.WriteByte(255);
        ms.WriteByte(128);
        ms.WriteByte(64);
        var data = ms.ToArray();

        var result = InvokeInterpret(interpreter, data);
        var position = GetPropertyValue<object>(result, "Position");
        var color = GetPropertyValue<object>(result, "Color");

        Assert.AreEqual(3.0f, GetPropertyValue<float>(position, "X"), 0.001f);
        Assert.AreEqual(4.0f, GetPropertyValue<float>(position, "Y"), 0.001f);
        Assert.AreEqual((byte)255, GetPropertyValue<byte>(color, "R"));
        Assert.AreEqual((byte)128, GetPropertyValue<byte>(color, "G"));
        Assert.AreEqual((byte)64, GetPropertyValue<byte>(color, "B"));
    }

    [TestMethod]
    public void Interpret_ArrayOfPrimitives_ShouldParseFixedCount()
    {
        var registry = new SchemaRegistry();

        var primitiveType = new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian);
        var arrayType = new ArrayTypeNode(primitiveType, new IntegerNode(3));

        var fields = new[]
        {
            new FieldDefinitionNode("Values", arrayType)
        };
        var schema = new BinarySchemaNode("IntArray", fields);
        registry.Register("IntArray", schema);

        var interpreter = CompileInterpreter(registry, "IntArray");

        var ms = new MemoryStream();
        ms.Write(BitConverter.GetBytes(10));
        ms.Write(BitConverter.GetBytes(20));
        ms.Write(BitConverter.GetBytes(30));
        var data = ms.ToArray();

        var result = InvokeInterpret(interpreter, data);
        var values = GetPropertyValue<int[]>(result, "Values");

        Assert.HasCount(3, values);
        Assert.AreEqual(10, values[0]);
        Assert.AreEqual(20, values[1]);
        Assert.AreEqual(30, values[2]);
    }

    [TestMethod]
    public void Interpret_ArrayOfPrimitives_WithDynamicCount_ShouldUsePreviousField()
    {
        var registry = new SchemaRegistry();

        var countField = CreatePrimitiveField("Count", PrimitiveTypeName.Byte, Endianness.NotApplicable);

        var primitiveType = new PrimitiveTypeNode(PrimitiveTypeName.Short, Endianness.LittleEndian);
        var sizeRef = new AccessColumnNode("Count", string.Empty, TextSpan.Empty);
        var arrayType = new ArrayTypeNode(primitiveType, sizeRef);
        var arrayField = new FieldDefinitionNode("Values", arrayType);

        var schema = new BinarySchemaNode("DynamicArray", [countField, arrayField]);
        registry.Register("DynamicArray", schema);

        var interpreter = CompileInterpreter(registry, "DynamicArray");

        var ms = new MemoryStream();
        ms.WriteByte(4);
        ms.Write(BitConverter.GetBytes((short)100));
        ms.Write(BitConverter.GetBytes((short)200));
        ms.Write(BitConverter.GetBytes((short)300));
        ms.Write(BitConverter.GetBytes((short)400));
        var data = ms.ToArray();

        var result = InvokeInterpret(interpreter, data);
        var count = GetPropertyValue<byte>(result, "Count");
        var values = GetPropertyValue<short[]>(result, "Values");

        Assert.AreEqual((byte)4, count);
        Assert.HasCount(4, values);
        Assert.AreEqual((short)100, values[0]);
        Assert.AreEqual((short)200, values[1]);
        Assert.AreEqual((short)300, values[2]);
        Assert.AreEqual((short)400, values[3]);
    }

    [TestMethod]
    public void Interpret_ArrayOfSchemas_ShouldParseAllElements()
    {
        var registry = new SchemaRegistry();

        var pointFields = new[]
        {
            CreatePrimitiveField("X", PrimitiveTypeName.Float, Endianness.LittleEndian),
            CreatePrimitiveField("Y", PrimitiveTypeName.Float, Endianness.LittleEndian)
        };
        var pointSchema = new BinarySchemaNode("Point", pointFields);
        registry.Register("Point", pointSchema);

        var countField = CreatePrimitiveField("VertexCount", PrimitiveTypeName.Int, Endianness.LittleEndian);
        var sizeRef = new AccessColumnNode("VertexCount", string.Empty, TextSpan.Empty);
        var arrayType = new ArrayTypeNode(new SchemaReferenceTypeNode("Point"), sizeRef);
        var verticesField = new FieldDefinitionNode("Vertices", arrayType);

        var meshSchema = new BinarySchemaNode("Mesh", [countField, verticesField]);
        registry.Register("Mesh", meshSchema);

        var interpreter = CompileInterpreter(registry, "Mesh");

        var ms = new MemoryStream();
        ms.Write(BitConverter.GetBytes(2));
        ms.Write(BitConverter.GetBytes(1.0f));
        ms.Write(BitConverter.GetBytes(2.0f));
        ms.Write(BitConverter.GetBytes(3.0f));
        ms.Write(BitConverter.GetBytes(4.0f));
        var data = ms.ToArray();

        var result = InvokeInterpret(interpreter, data);
        var vertexCount = GetPropertyValue<int>(result, "VertexCount");
        var vertices = GetPropertyValue<object[]>(result, "Vertices");

        Assert.AreEqual(2, vertexCount);
        Assert.HasCount(2, vertices);
        Assert.AreEqual(1.0f, GetPropertyValue<float>(vertices[0], "X"), 0.001f);
        Assert.AreEqual(2.0f, GetPropertyValue<float>(vertices[0], "Y"), 0.001f);
        Assert.AreEqual(3.0f, GetPropertyValue<float>(vertices[1], "X"), 0.001f);
        Assert.AreEqual(4.0f, GetPropertyValue<float>(vertices[1], "Y"), 0.001f);
    }

    [TestMethod]
    public void Interpret_ArrayOfSchemas_WithZeroCount_ShouldReturnEmptyArray()
    {
        var registry = new SchemaRegistry();

        var pointFields = new[]
        {
            CreatePrimitiveField("X", PrimitiveTypeName.Float, Endianness.LittleEndian),
            CreatePrimitiveField("Y", PrimitiveTypeName.Float, Endianness.LittleEndian)
        };
        var pointSchema = new BinarySchemaNode("Point", pointFields);
        registry.Register("Point", pointSchema);

        var countField = CreatePrimitiveField("VertexCount", PrimitiveTypeName.Int, Endianness.LittleEndian);
        var sizeRef = new AccessColumnNode("VertexCount", string.Empty, TextSpan.Empty);
        var arrayType = new ArrayTypeNode(new SchemaReferenceTypeNode("Point"), sizeRef);
        var verticesField = new FieldDefinitionNode("Vertices", arrayType);

        var meshSchema = new BinarySchemaNode("Mesh", [countField, verticesField]);
        registry.Register("Mesh", meshSchema);

        var interpreter = CompileInterpreter(registry, "Mesh");

        var data = BitConverter.GetBytes(0);

        var result = InvokeInterpret(interpreter, data);
        var vertexCount = GetPropertyValue<int>(result, "VertexCount");
        var vertices = GetPropertyValue<object[]>(result, "Vertices");

        Assert.AreEqual(0, vertexCount);
        Assert.IsEmpty(vertices);
    }
}
