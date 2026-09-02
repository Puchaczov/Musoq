using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Build;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Tests for the InterpreterCodeGenerator and InterpreterCompilationUnit.
/// </summary>
public partial class InterpreterCodeGenTests
{

    [TestMethod]
    public void GenerateAll_SimpleBinarySchema_ShouldGenerateClass()
    {
        // Arrange
        var registry = new SchemaRegistry();
        var fields = new[]
        {
            CreatePrimitiveField("Magic", PrimitiveTypeName.Int, Endianness.LittleEndian),
            CreatePrimitiveField("Version", PrimitiveTypeName.Short, Endianness.LittleEndian)
        };
        var schema = new BinarySchemaNode("Header", [.. fields]);
        registry.Register("Header", schema);
        var generator = new InterpreterCodeGenerator(registry);

        // Act
        var code = generator.GenerateAll();

        // Assert
        Assert.Contains("public sealed class Header : BytesInterpreterBase<Header>", code);
        Assert.Contains("public int Magic { get; init; }", code);
        Assert.Contains("public short Version { get; init; }", code);
        Assert.Contains("ReadInt32Le(data)", code);
        Assert.Contains("ReadInt16Le(data)", code);
    }

    [TestMethod]
    public void GenerateAll_SupportedBinarySchema_ShouldNotEmitUnsupportedTodoMarkers()
    {
        // Arrange
        var registry = new SchemaRegistry();
        var fields = new[]
        {
            CreatePrimitiveField("Magic", PrimitiveTypeName.Int, Endianness.LittleEndian),
            new FieldDefinitionNode(
                "Payload",
                new ArrayTypeNode(
                    new PrimitiveTypeNode(PrimitiveTypeName.Byte, Endianness.NotApplicable),
                    new IntegerNode(4)))
        };
        registry.Register("Packet", new BinarySchemaNode("Packet", [.. fields]));
        var generator = new InterpreterCodeGenerator(registry);

        // Act
        var code = generator.GenerateAll();

        // Assert
        Assert.DoesNotContain("TODO: Unsupported", code);
    }

    [TestMethod]
    public void GenerateAll_UnsupportedBinaryTypeAnnotation_ShouldFailFastWithDiagnosticContext()
    {
        // Arrange
        var registry = new SchemaRegistry();
        var fields = new[]
        {
            new FieldDefinitionNode("Payload", new UnsupportedTypeAnnotationNode())
        };
        registry.Register("Packet", new BinarySchemaNode("Packet", [.. fields]));
        var generator = new InterpreterCodeGenerator(registry);

        // Act
        var exception = Assert.Throws<ConstructionNotYetSupported>(() => generator.GenerateAll());

        // Assert
        Assert.AreEqual(DiagnosticCode.MQ4016_UnsupportedSchemaConstruction, exception.Code);
        Assert.Contains("Packet", exception.Message);
        Assert.Contains("Payload", exception.Message);
        Assert.Contains(nameof(UnsupportedTypeAnnotationNode), exception.Message);
    }

    [TestMethod]
    public void GenerateAll_ByteField_ShouldNotHaveEndianness()
    {
        // Arrange
        var registry = new SchemaRegistry();
        var fields = new[]
        {
            CreatePrimitiveField("Flags", PrimitiveTypeName.Byte, Endianness.NotApplicable)
        };
        var schema = new BinarySchemaNode("SimpleHeader", [.. fields]);
        registry.Register("SimpleHeader", schema);
        var generator = new InterpreterCodeGenerator(registry);

        // Act
        var code = generator.GenerateAll();

        // Assert
        Assert.Contains("public byte Flags { get; init; }", code);
        Assert.Contains("ReadByte(data)", code);
        Assert.DoesNotContain("ReadByteLE", code);
        Assert.DoesNotContain("ReadByteBE", code);
    }

    [TestMethod]
    public void GenerateAll_BigEndianField_ShouldUseBigEndianMethod()
    {
        // Arrange
        var registry = new SchemaRegistry();
        var fields = new[]
        {
            CreatePrimitiveField("NetworkOrder", PrimitiveTypeName.Int, Endianness.BigEndian)
        };
        var schema = new BinarySchemaNode("NetworkPacket", [.. fields]);
        registry.Register("NetworkPacket", schema);
        var generator = new InterpreterCodeGenerator(registry);

        // Act
        var code = generator.GenerateAll();

        // Assert
        Assert.Contains("ReadInt32Be(data)", code);
    }

    [TestMethod]
    public void GenerateAll_AllPrimitiveTypes_ShouldGenerateCorrectMethods()
    {
        // Arrange
        var registry = new SchemaRegistry();
        var fields = new[]
        {
            CreatePrimitiveField("F1", PrimitiveTypeName.Byte, Endianness.NotApplicable),
            CreatePrimitiveField("F2", PrimitiveTypeName.SByte, Endianness.NotApplicable),
            CreatePrimitiveField("F3", PrimitiveTypeName.Short, Endianness.LittleEndian),
            CreatePrimitiveField("F4", PrimitiveTypeName.UShort, Endianness.LittleEndian),
            CreatePrimitiveField("F5", PrimitiveTypeName.Int, Endianness.LittleEndian),
            CreatePrimitiveField("F6", PrimitiveTypeName.UInt, Endianness.LittleEndian),
            CreatePrimitiveField("F7", PrimitiveTypeName.Long, Endianness.LittleEndian),
            CreatePrimitiveField("F8", PrimitiveTypeName.ULong, Endianness.LittleEndian),
            CreatePrimitiveField("F9", PrimitiveTypeName.Float, Endianness.LittleEndian),
            CreatePrimitiveField("F10", PrimitiveTypeName.Double, Endianness.LittleEndian)
        };
        var schema = new BinarySchemaNode("AllTypes", [.. fields]);
        registry.Register("AllTypes", schema);
        var generator = new InterpreterCodeGenerator(registry);

        // Act
        var code = generator.GenerateAll();

        // Assert
        Assert.Contains("ReadByte(data)", code);
        Assert.Contains("ReadSByte(data)", code);
        Assert.Contains("ReadInt16Le(data)", code);
        Assert.Contains("ReadUInt16Le(data)", code);
        Assert.Contains("ReadInt32Le(data)", code);
        Assert.Contains("ReadUInt32Le(data)", code);
        Assert.Contains("ReadInt64Le(data)", code);
        Assert.Contains("ReadUInt64Le(data)", code);
        Assert.Contains("ReadSingleLe(data)", code);
        Assert.Contains("ReadDoubleLe(data)", code);
    }

    [TestMethod]
    public void GenerateAll_DiscardField_ShouldNotCreateProperty()
    {
        // Arrange
        var registry = new SchemaRegistry();
        var fields = new[]
        {
            CreatePrimitiveField("Magic", PrimitiveTypeName.Int, Endianness.LittleEndian),
            CreatePrimitiveField("_", PrimitiveTypeName.Byte, Endianness.NotApplicable),
            CreatePrimitiveField("Value", PrimitiveTypeName.Int, Endianness.LittleEndian)
        };
        var schema = new BinarySchemaNode("WithDiscard", [.. fields]);
        registry.Register("WithDiscard", schema);
        var generator = new InterpreterCodeGenerator(registry);

        // Act
        var code = generator.GenerateAll();

        // Assert
        Assert.Contains("public int Magic { get; init; }", code);
        Assert.Contains("public int Value { get; init; }", code);

        Assert.DoesNotContain("public byte _ { get; init; }", code);
    }

    [TestMethod]
    public void GenerateAll_ByteArrayField_ShouldGenerateReadBytes()
    {
        // Arrange
        var registry = new SchemaRegistry();
        var sizeExpr = new IntegerNode(16);
        var byteArrayType = new ByteArrayTypeNode(sizeExpr);
        var fields = new[]
        {
            new FieldDefinitionNode("Data", byteArrayType)
        };
        var schema = new BinarySchemaNode("WithByteArray", [.. fields]);
        registry.Register("WithByteArray", schema);
        var generator = new InterpreterCodeGenerator(registry);

        // Act
        var code = generator.GenerateAll();

        // Assert
        Assert.Contains("public byte[] Data { get; init; }", code);
        Assert.Contains("ReadBytes(data, 16)", code);
    }

    [TestMethod]
    public void GenerateAll_MultipleSchemas_ShouldGenerateAllClasses()
    {
        // Arrange
        var registry = new SchemaRegistry();

        var fields1 = new[] { CreatePrimitiveField("X", PrimitiveTypeName.Float, Endianness.LittleEndian) };
        var schema1 = new BinarySchemaNode("Point", [.. fields1]);
        registry.Register("Point", schema1);

        var fields2 = new[] { CreatePrimitiveField("Value", PrimitiveTypeName.Int, Endianness.LittleEndian) };
        var schema2 = new BinarySchemaNode("Data", [.. fields2]);
        registry.Register("Data", schema2);

        var generator = new InterpreterCodeGenerator(registry);

        // Act
        var code = generator.GenerateAll();

        // Assert
        Assert.Contains("public sealed class Point", code);
        Assert.Contains("public sealed class Data", code);
    }

    [TestMethod]
    public void Compile_SimpleBinarySchema_ShouldCompileSuccessfully()
    {
        // Arrange
        var registry = new SchemaRegistry();
        var fields = new[]
        {
            CreatePrimitiveField("Magic", PrimitiveTypeName.Int, Endianness.LittleEndian),
            CreatePrimitiveField("Version", PrimitiveTypeName.Short, Endianness.LittleEndian)
        };
        var schema = new BinarySchemaNode("Header", [.. fields]);
        registry.Register("Header", schema);

        var generator = new InterpreterCodeGenerator(registry);
        var code = generator.GenerateAll();

        using var compilationUnit = new InterpreterCompilationUnit(
            $"TestAssembly_{Guid.NewGuid():N}",
            code);

        // Act
        var result = compilationUnit.Compile();

        // Assert
        Assert.IsTrue(result, $"Compilation failed: {string.Join(", ", compilationUnit.GetErrorMessages())}");
        Assert.IsTrue(compilationUnit.IsSuccess);
        Assert.IsNotNull(compilationUnit.CompiledAssembly);
    }

    [TestMethod]
    public void Compile_GetInterpreterType_ShouldReturnType()
    {
        // Arrange
        var registry = new SchemaRegistry();
        var fields = new[]
        {
            CreatePrimitiveField("Value", PrimitiveTypeName.Int, Endianness.LittleEndian)
        };
        var schema = new BinarySchemaNode("TestSchema", [.. fields]);
        registry.Register("TestSchema", schema);

        var generator = new InterpreterCodeGenerator(registry);
        var code = generator.GenerateAll();

        using var compilationUnit = new InterpreterCompilationUnit(
            $"TestAssembly_{Guid.NewGuid():N}",
            code);
        compilationUnit.Compile();

        // Act
        var type = compilationUnit.GetInterpreterType("TestSchema");

        // Assert
        Assert.IsNotNull(type);
        Assert.AreEqual("TestSchema", type.Name);
        Assert.IsTrue(typeof(IBytesInterpreter<>).MakeGenericType(type).IsAssignableFrom(type));
    }

    [TestMethod]
    public void Compile_AllPrimitiveTypes_ShouldCompileSuccessfully()
    {
        // Arrange
        var registry = new SchemaRegistry();
        var fields = new[]
        {
            CreatePrimitiveField("F1", PrimitiveTypeName.Byte, Endianness.NotApplicable),
            CreatePrimitiveField("F2", PrimitiveTypeName.SByte, Endianness.NotApplicable),
            CreatePrimitiveField("F3", PrimitiveTypeName.Short, Endianness.LittleEndian),
            CreatePrimitiveField("F4", PrimitiveTypeName.UShort, Endianness.BigEndian),
            CreatePrimitiveField("F5", PrimitiveTypeName.Int, Endianness.LittleEndian),
            CreatePrimitiveField("F6", PrimitiveTypeName.UInt, Endianness.BigEndian),
            CreatePrimitiveField("F7", PrimitiveTypeName.Long, Endianness.LittleEndian),
            CreatePrimitiveField("F8", PrimitiveTypeName.ULong, Endianness.BigEndian),
            CreatePrimitiveField("F9", PrimitiveTypeName.Float, Endianness.LittleEndian),
            CreatePrimitiveField("F10", PrimitiveTypeName.Double, Endianness.BigEndian)
        };
        var schema = new BinarySchemaNode("AllTypesSchema", [.. fields]);
        registry.Register("AllTypesSchema", schema);

        var generator = new InterpreterCodeGenerator(registry);
        var code = generator.GenerateAll();

        using var compilationUnit = new InterpreterCompilationUnit(
            $"TestAssembly_{Guid.NewGuid():N}",
            code);

        // Act
        var result = compilationUnit.Compile();

        // Assert
        Assert.IsTrue(result,
            $"Compilation failed: {string.Join(Environment.NewLine, compilationUnit.GetErrorMessages())}");
    }

    [TestMethod]
    public void GenerateAll_RepeatUntilWithPrimitive_ShouldGenerateDoWhileLoop()
    {
        var registry = new SchemaRegistry();
        var primitiveType = new PrimitiveTypeNode(PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var condition = new EqualityNode(
            new AccessColumnNode("Bytes", string.Empty, TextSpan.Empty),
            new IntegerNode("0"));
        var repeatUntilType = new RepeatUntilTypeNode(primitiveType, condition, "Bytes");
        var fields = new[]
        {
            new FieldDefinitionNode("Bytes", repeatUntilType)
        };
        var schema = new BinarySchemaNode("ByteList", [.. fields]);
        registry.Register("ByteList", schema);
        var generator = new InterpreterCodeGenerator(registry);

        // Act
        var code = generator.GenerateAll();

        // Assert
        Assert.Contains("public byte[] Bytes { get; init; }",
            code, $"Expected byte[] property but got:\n{code}");
        Assert.Contains("List<byte>",
            code, $"Expected List<byte> but got:\n{code}");
        Assert.Contains("do",
            code, $"Expected do-while loop but got:\n{code}");
        Assert.Contains("while (!",
            code, $"Expected while condition but got:\n{code}");
        Assert.Contains("ToArray()",
            code, $"Expected ToArray() but got:\n{code}");
    }

    [TestMethod]
    public void GenerateAll_RepeatUntilWithSchemaRef_ShouldGenerateInterpreterLoop()
    {
        var registry = new SchemaRegistry();


        var recordFields = new[]
        {
            CreatePrimitiveField("Type", PrimitiveTypeName.Byte, Endianness.NotApplicable),
            CreatePrimitiveField("Value", PrimitiveTypeName.Int, Endianness.LittleEndian)
        };
        var recordSchema = new BinarySchemaNode("Record", [.. recordFields]);
        registry.Register("Record", recordSchema);


        var schemaRefType = new SchemaReferenceTypeNode("Record");

        var condition = new EqualityNode(
            new DotNode(
                new AccessColumnNode("Records", string.Empty, TextSpan.Empty),
                new IdentifierNode("Type"),
                "Records.Type"),
            new IntegerNode("0"));
        var repeatUntilType = new RepeatUntilTypeNode(schemaRefType, condition, "Records");
        var fields = new[]
        {
            new FieldDefinitionNode("Records", repeatUntilType)
        };
        var schema = new BinarySchemaNode("RecordList", [.. fields]);
        registry.Register("RecordList", schema);
        var generator = new InterpreterCodeGenerator(registry);

        // Act
        var code = generator.GenerateAll();

        // Assert
        Assert.Contains("public Record[] Records { get; init; }",
            code, $"Expected Record[] property but got:\n{code}");
        Assert.Contains("List<Record>",
            code, $"Expected List<Record> but got:\n{code}");
        Assert.Contains("new Record()",
            code, $"Expected Record interpreter creation but got:\n{code}");
        Assert.Contains("InterpretAt",
            code, $"Expected InterpretAt call but got:\n{code}");
        Assert.Contains(".Type",
            code, $"Expected .Type property access but got:\n{code}");
    }

    [TestMethod]
    public void GenerateAll_RepeatUntilWithSchemaRef_ShouldCompile()
    {
        // Arrange
        var registry = new SchemaRegistry();


        var recordFields = new[]
        {
            CreatePrimitiveField("Type", PrimitiveTypeName.Byte, Endianness.NotApplicable),
            CreatePrimitiveField("Value", PrimitiveTypeName.Int, Endianness.LittleEndian)
        };
        var recordSchema = new BinarySchemaNode("Record", [.. recordFields]);
        registry.Register("Record", recordSchema);


        var schemaRefType = new SchemaReferenceTypeNode("Record");
        var condition = new EqualityNode(
            new DotNode(
                new AccessColumnNode("Records", string.Empty, TextSpan.Empty),
                new IdentifierNode("Type"),
                "Records.Type"),
            new IntegerNode("0"));
        var repeatUntilType = new RepeatUntilTypeNode(schemaRefType, condition, "Records");
        var fields = new[]
        {
            new FieldDefinitionNode("Records", repeatUntilType)
        };
        var schema = new BinarySchemaNode("RecordList", [.. fields]);
        registry.Register("RecordList", schema);

        var generator = new InterpreterCodeGenerator(registry);
        var code = generator.GenerateAll();

        using var compilationUnit = new InterpreterCompilationUnit(
            $"TestAssembly_{Guid.NewGuid():N}",
            code);

        // Act
        var result = compilationUnit.Compile();

        // Assert
        Assert.IsTrue(result,
            $"Compilation failed: {string.Join(Environment.NewLine, compilationUnit.GetErrorMessages())}\n\nGenerated code:\n{code}");
    }

    [TestMethod]
    public void GenerateAll_OptionalDiscardField_ShouldGenerateTryCatchWithPositionRestore()
    {
        // Arrange
        var registry = new SchemaRegistry();


        var fields = new[]
        {
            new TextFieldDefinitionNode("Required", TextFieldType.Until, ":"),
            new TextFieldDefinitionNode("_", TextFieldType.Literal, "\t", null,
                TextFieldModifier.Optional),
            new TextFieldDefinitionNode("Extra", TextFieldType.Rest)
        };
        var schema = new TextSchemaNode("LogEntry", fields);
        registry.Register("LogEntry", schema);

        var generator = new InterpreterCodeGenerator(registry);

        // Act
        var code = generator.GenerateAll();

        // Assert

        Assert.Contains("_savedPos_",
            code, $"Expected position save for optional discard but got:\n{code}");
        Assert.Contains("try",
            code, $"Expected 'try' block for optional discard but got:\n{code}");
        Assert.Contains("ExpectLiteral(data, \"\\t\", fieldName: \"_\")",
            code, $"Expected 'ExpectLiteral' call but got:\n{code}");
    }

}
