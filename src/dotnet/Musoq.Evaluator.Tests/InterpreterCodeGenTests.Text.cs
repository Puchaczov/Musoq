using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Build;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Tests for the InterpreterCodeGenerator and InterpreterCompilationUnit.
/// </summary>
public partial class InterpreterCodeGenTests
{

    [TestMethod]
    public void GenerateAll_SimpleTextSchema_ShouldGenerateClass()
    {
        // Arrange
        var registry = new SchemaRegistry();
        var fields = new[]
        {
            CreateTextField("Key", TextFieldType.Until, ":"),
            CreateTextField("_", TextFieldType.Literal, ": "),
            CreateTextField("Value", TextFieldType.Rest)
        };
        var schema = new TextSchemaNode("KeyValue", fields);
        registry.Register("KeyValue", schema);
        var generator = new InterpreterCodeGenerator(registry);

        // Act
        var code = generator.GenerateAll();

        // Assert
        Assert.Contains("public sealed class KeyValue : TextInterpreterBase<KeyValue>", code);
        Assert.Contains("public string? Key { get; init; }", code);
        Assert.Contains("public string? Value { get; init; }", code);
        Assert.Contains("ReadUntil(data, \":\"", code);
        Assert.Contains("ExpectLiteral(data, \": \"", code);
        Assert.Contains("ReadRest(data", code);
    }

    [TestMethod]
    public void GenerateAll_TextSchemaWithBetween_ShouldGenerateBetweenCall()
    {
        // Arrange
        var registry = new SchemaRegistry();
        var fields = new[]
        {
            CreateTextField("Bracketed", TextFieldType.Between, "[", "]")
        };
        var schema = new TextSchemaNode("BracketedValue", fields);
        registry.Register("BracketedValue", schema);
        var generator = new InterpreterCodeGenerator(registry);

        // Act
        var code = generator.GenerateAll();

        // Assert
        Assert.Contains("ReadBetween(data, \"[\", \"]\"", code);
    }

    [TestMethod]
    public void GenerateAll_TextSchemaWithChars_ShouldGenerateCharsCall()
    {
        // Arrange
        var registry = new SchemaRegistry();
        var fields = new[]
        {
            CreateTextField("Fixed", TextFieldType.Chars, "10", modifiers: TextFieldModifier.Trim)
        };
        var schema = new TextSchemaNode("FixedWidth", fields);
        registry.Register("FixedWidth", schema);
        var generator = new InterpreterCodeGenerator(registry);

        // Act
        var code = generator.GenerateAll();

        // Assert
        Assert.Contains("ReadChars(data, 10, trim: true)", code);
    }

    [TestMethod]
    public void GenerateAll_TextSchemaWithPattern_ShouldGeneratePatternCall()
    {
        // Arrange
        var registry = new SchemaRegistry();
        var fields = new[]
        {
            CreateTextField("IpAddress", TextFieldType.Pattern, @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}")
        };
        var schema = new TextSchemaNode("IpMatcher", fields);
        registry.Register("IpMatcher", schema);
        var generator = new InterpreterCodeGenerator(registry);

        // Act
        var code = generator.GenerateAll();

        // Assert
        Assert.Contains("ReadPattern(data", code);
    }

    [TestMethod]
    public void GenerateAll_TextSchemaWithToken_ShouldGenerateTokenCall()
    {
        // Arrange
        var registry = new SchemaRegistry();
        var fields = new[]
        {
            CreateTextField("Word1", TextFieldType.Token),
            CreateTextField("_", TextFieldType.Whitespace, "+"),
            CreateTextField("Word2", TextFieldType.Token)
        };
        var schema = new TextSchemaNode("TwoWords", fields);
        registry.Register("TwoWords", schema);
        var generator = new InterpreterCodeGenerator(registry);

        // Act
        var code = generator.GenerateAll();

        // Assert
        Assert.Contains("ReadToken(data", code);
        Assert.Contains("SkipWhitespace(data, true)", code);
    }

    [TestMethod]
    public void GenerateAll_TextSchema_ShouldCompile()
    {
        // Arrange
        var registry = new SchemaRegistry();
        var fields = new[]
        {
            CreateTextField("Key", TextFieldType.Until, ":"),
            CreateTextField("_", TextFieldType.Literal, ": "),
            CreateTextField("Value", TextFieldType.Rest)
        };
        var schema = new TextSchemaNode("SimpleKeyValue", fields);
        registry.Register("SimpleKeyValue", schema);

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
    public void GenerateAll_NestedSchema_ShouldCompile()
    {
        // Arrange
        var registry = new SchemaRegistry();


        var pointFields = new[]
        {
            CreatePrimitiveField("X", PrimitiveTypeName.Float, Endianness.LittleEndian),
            CreatePrimitiveField("Y", PrimitiveTypeName.Float, Endianness.LittleEndian)
        };
        var pointSchema = new BinarySchemaNode("Point", [.. pointFields]);
        registry.Register("Point", pointSchema);


        var vertexFields = new[]
        {
            CreatePrimitiveField("Id", PrimitiveTypeName.Int, Endianness.LittleEndian),
            new FieldDefinitionNode("Position", new SchemaReferenceTypeNode("Point"))
        };
        var vertexSchema = new BinarySchemaNode("Vertex", [.. vertexFields]);
        registry.Register("Vertex", vertexSchema);

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
    public void GenerateAll_ArithmeticSizeExpression_ShouldGenerateCorrectExpression()
    {
        // Arrange
        var registry = new SchemaRegistry();


        var totalField = CreatePrimitiveField("Total", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var headerSizeField = CreatePrimitiveField("HeaderSize", PrimitiveTypeName.Byte, Endianness.NotApplicable);


        var sizeExpr = new HyphenNode(
            new IdentifierNode("Total"),
            new IdentifierNode("HeaderSize"));
        var byteArrayType = new ByteArrayTypeNode(sizeExpr);

        var fields = new[]
        {
            totalField,
            headerSizeField,
            new FieldDefinitionNode("Data", byteArrayType)
        };
        var schema = new BinarySchemaNode("DynamicPacket", [.. fields]);
        registry.Register("DynamicPacket", schema);

        var generator = new InterpreterCodeGenerator(registry);

        // Act
        var code = generator.GenerateAll();

        // Assert - code generator now adds (int) casts for field references to handle nullable types
        Assert.Contains("ReadBytes(data, ((int)_total - (int)_headerSize))",
            code, $"Expected 'ReadBytes(data, ((int)_total - (int)_headerSize))' but generated code:\n{code}");
    }

    [TestMethod]
    public void GenerateAll_OptionalTextField_ShouldGenerateTryCatch()
    {
        // Arrange
        var registry = new SchemaRegistry();


        var fields = new[]
        {
            new TextFieldDefinitionNode("Required", TextFieldType.Until, ":"),
            new TextFieldDefinitionNode("Optional", TextFieldType.Until, ",", null,
                TextFieldModifier.Optional)
        };
        var schema = new TextSchemaNode("LogEntry", fields);
        registry.Register("LogEntry", schema);

        var generator = new InterpreterCodeGenerator(registry);

        // Act
        var code = generator.GenerateAll();

        // Assert

        Assert.Contains("string? _optional = null;",
            code, $"Expected 'string? _optional = null;' but got:\n{code}");
        Assert.Contains("_savedPos__optional",
            code, $"Expected position save for optional but got:\n{code}");
        Assert.Contains("try",
            code, $"Expected 'try' block for optional but got:\n{code}");
        Assert.Contains("catch",
            code, $"Expected 'catch' block for optional but got:\n{code}");
    }

    [TestMethod]
    public void GenerateAll_OptionalTextField_ShouldCompile()
    {
        // Arrange
        var registry = new SchemaRegistry();

        var fields = new[]
        {
            new TextFieldDefinitionNode("Key", TextFieldType.Until, ":"),
            new TextFieldDefinitionNode("Value", TextFieldType.Until, "\t", null,
                TextFieldModifier.Optional | TextFieldModifier.Trim),
            new TextFieldDefinitionNode("Extra", TextFieldType.Rest)
        };
        var schema = new TextSchemaNode("KeyValue", fields);
        registry.Register("KeyValue", schema);

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
    public void GenerateAll_WhitespacePlus_ShouldGenerateRequiredSkip()
    {
        // Arrange
        var registry = new SchemaRegistry();
        var fields = new[]
        {
            CreateTextField("Word1", TextFieldType.Token),
            CreateTextField("_", TextFieldType.Whitespace, "+"),
            CreateTextField("Word2", TextFieldType.Token)
        };
        var schema = new TextSchemaNode("Test", fields);
        registry.Register("Test", schema);
        var generator = new InterpreterCodeGenerator(registry);

        // Act
        var code = generator.GenerateAll();

        // Assert
        Assert.Contains("SkipWhitespace(data, true)", code);
    }

    [TestMethod]
    public void GenerateAll_WhitespaceStar_ShouldGenerateOptionalSkip()
    {
        // Arrange
        var registry = new SchemaRegistry();
        var fields = new[]
        {
            CreateTextField("Word1", TextFieldType.Token),
            CreateTextField("_", TextFieldType.Whitespace, "*"),
            CreateTextField("Word2", TextFieldType.Token)
        };
        var schema = new TextSchemaNode("Test", fields);
        registry.Register("Test", schema);
        var generator = new InterpreterCodeGenerator(registry);

        // Act
        var code = generator.GenerateAll();

        // Assert
        Assert.Contains("SkipWhitespace(data, false)", code);
    }

    [TestMethod]
    public void GenerateAll_WhitespaceQuestion_ShouldGenerateOptionalSingleSkip()
    {
        // Arrange
        var registry = new SchemaRegistry();
        var fields = new[]
        {
            CreateTextField("Word1", TextFieldType.Token),
            CreateTextField("_", TextFieldType.Whitespace, "?"),
            CreateTextField("Word2", TextFieldType.Token)
        };
        var schema = new TextSchemaNode("Test", fields);
        registry.Register("Test", schema);
        var generator = new InterpreterCodeGenerator(registry);

        // Act
        var code = generator.GenerateAll();

        // Assert
        Assert.Contains("SkipOptionalWhitespace(data)", code);
    }

    [TestMethod]
    public void GenerateAll_RepeatTextField_ShouldGenerateLoopWithDelimiter()
    {
        // Arrange
        var registry = new SchemaRegistry();


        var lineFields = new[]
        {
            new TextFieldDefinitionNode("Value", TextFieldType.Rest)
        };
        var lineSchema = new TextSchemaNode("Line", lineFields);
        registry.Register("Line", lineSchema);


        var headerFields = new[]
        {
            new TextFieldDefinitionNode("Lines", TextFieldType.Repeat, "Line", "\n\n")
        };
        var headerSchema = new TextSchemaNode("AllLines", headerFields);
        registry.Register("AllLines", headerSchema);

        var generator = new InterpreterCodeGenerator(registry);

        // Act
        var code = generator.GenerateAll();

        // Assert
        Assert.Contains("List<Line>",
            code, $"Expected 'List<Line>' but got:\n{code}");
        Assert.Contains("while (!IsAtEnd(data) && !LookaheadMatches",
            code, $"Expected loop with LookaheadMatches but got:\n{code}");
        Assert.Contains("Line[]? Lines",
            code, $"Expected 'Line[]? Lines' property but got:\n{code}");
    }

    [TestMethod]
    public void GenerateAll_RepeatTextField_UntilEnd_ShouldGenerateSimpleLoop()
    {
        // Arrange
        var registry = new SchemaRegistry();


        var lineFields = new[]
        {
            new TextFieldDefinitionNode("Content", TextFieldType.Until, "\n")
        };
        var lineSchema = new TextSchemaNode("TextLine", lineFields);
        registry.Register("TextLine", lineSchema);


        var allLinesFields = new[]
        {
            new TextFieldDefinitionNode("Items", TextFieldType.Repeat, "TextLine")
        };
        var allLinesSchema = new TextSchemaNode("TextBlock", allLinesFields);
        registry.Register("TextBlock", allLinesSchema);

        var generator = new InterpreterCodeGenerator(registry);

        // Act
        var code = generator.GenerateAll();

        // Assert
        Assert.Contains("while (!IsAtEnd(data))",
            code, $"Expected 'while (!IsAtEnd(data))' but got:\n{code}");
        Assert.Contains("List<TextLine>",
            code, $"Expected 'List<TextLine>' but got:\n{code}");
    }

    [TestMethod]
    public void GenerateAll_RepeatTextField_ShouldCompile()
    {
        // Arrange
        var registry = new SchemaRegistry();


        var lineFields = new[]
        {
            new TextFieldDefinitionNode("Value", TextFieldType.Until, "\n")
        };
        var lineSchema = new TextSchemaNode("SimpleLine", lineFields);
        registry.Register("SimpleLine", lineSchema);


        var headerFields = new[]
        {
            new TextFieldDefinitionNode("Lines", TextFieldType.Repeat, "SimpleLine")
        };
        var headerSchema = new TextSchemaNode("Document", headerFields);
        registry.Register("Document", headerSchema);

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
    public void GenerateAll_SwitchTextField_ShouldGenerateLookahead()
    {
        // Arrange
        var registry = new SchemaRegistry();


        var sectionFields = new[]
        {
            new TextFieldDefinitionNode("Name", TextFieldType.Until, "]")
        };
        var sectionSchema = new TextSchemaNode("SectionHeader", sectionFields);
        registry.Register("SectionHeader", sectionSchema);

        var commentFields = new[]
        {
            new TextFieldDefinitionNode("Text", TextFieldType.Rest)
        };
        var commentSchema = new TextSchemaNode("Comment", commentFields);
        registry.Register("Comment", commentSchema);


        var switchCases = new[]
        {
            new TextSwitchCaseNode(@"\s*\[", "SectionHeader"),
            new TextSwitchCaseNode(@"\s*#", "Comment")
        };
        var configFields = new[]
        {
            new TextFieldDefinitionNode("Content", switchCases)
        };
        var configSchema = new TextSchemaNode("ConfigLine", configFields);
        registry.Register("ConfigLine", configSchema);

        var generator = new InterpreterCodeGenerator(registry);

        // Act
        var code = generator.GenerateAll();

        // Assert
        Assert.Contains("LookaheadMatchesPattern",
            code, $"Expected 'LookaheadMatchesPattern' but got:\n{code}");
        Assert.Contains("SectionHeader",
            code, $"Expected 'SectionHeader' but got:\n{code}");
        Assert.Contains("Comment",
            code, $"Expected 'Comment' but got:\n{code}");
    }

    [TestMethod]
    public void GenerateAll_SwitchTextField_WithDefaultCase_ShouldGenerateElse()
    {
        // Arrange
        var registry = new SchemaRegistry();


        var sectionFields = new[]
        {
            new TextFieldDefinitionNode("Name", TextFieldType.Until, "]")
        };
        var sectionSchema = new TextSchemaNode("SectionHeader", sectionFields);
        registry.Register("SectionHeader", sectionSchema);

        var keyValueFields = new[]
        {
            new TextFieldDefinitionNode("Value", TextFieldType.Rest)
        };
        var keyValueSchema = new TextSchemaNode("KeyValue", keyValueFields);
        registry.Register("KeyValue", keyValueSchema);


        var switchCases = new[]
        {
            new TextSwitchCaseNode(@"\s*\[", "SectionHeader"),
            new TextSwitchCaseNode(null, "KeyValue")
        };
        var configFields = new[]
        {
            new TextFieldDefinitionNode("Content", switchCases)
        };
        var configSchema = new TextSchemaNode("ConfigLine", configFields);
        registry.Register("ConfigLine", configSchema);

        var generator = new InterpreterCodeGenerator(registry);

        // Act
        var code = generator.GenerateAll();

        // Assert
        Assert.Contains("else",
            code, $"Expected 'else' for default case but got:\n{code}");
        Assert.Contains("KeyValue",
            code, $"Expected 'KeyValue' but got:\n{code}");
    }

    [TestMethod]
    public void GenerateAll_SwitchTextField_ShouldCompile()
    {
        // Arrange
        var registry = new SchemaRegistry();


        var sectionFields = new[]
        {
            new TextFieldDefinitionNode("_", TextFieldType.Literal, "["),
            new TextFieldDefinitionNode("Name", TextFieldType.Until, "]"),
            new TextFieldDefinitionNode("_", TextFieldType.Literal, "]")
        };
        var sectionSchema = new TextSchemaNode("SectionHeader", sectionFields);
        registry.Register("SectionHeader", sectionSchema);

        var commentFields = new[]
        {
            new TextFieldDefinitionNode("_", TextFieldType.Literal, "#"),
            new TextFieldDefinitionNode("Text", TextFieldType.Rest)
        };
        var commentSchema = new TextSchemaNode("Comment", commentFields);
        registry.Register("Comment", commentSchema);


        var switchCases = new[]
        {
            new TextSwitchCaseNode(@"\[", "SectionHeader"),
            new TextSwitchCaseNode(null, "Comment")
        };
        var configFields = new[]
        {
            new TextFieldDefinitionNode("Content", switchCases)
        };
        var configSchema = new TextSchemaNode("ConfigLine", configFields);
        registry.Register("ConfigLine", configSchema);

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

}
