#nullable enable

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Build;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Integration tests that generate text interpreter code and actually parse text data.
/// </summary>
[TestClass]
public class TextInterpretationTests
{
    #region Rest Field Tests

    [TestMethod]
    public void Parse_Rest_ShouldCaptureRemainingText()
    {
        // Arrange: text Schema { Prefix: chars[3], Suffix: rest }
        var interpreter = CreateAndCompileInterpreter("TestSchema",
            CreateTextField("Prefix", TextFieldType.Chars, "3"),
            CreateTextField("Suffix", TextFieldType.Rest));

        var data = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghij";

        // Act
        var result = InvokeParse(interpreter, data);
        var prefix = GetPropertyValue<string>(result, "Prefix");
        var suffix = GetPropertyValue<string>(result, "Suffix");

        // Assert
        Assert.AreEqual("ABC", prefix);
        Assert.AreEqual("DEFGHIJKLMNOPQRSTUVWXYZabcdefghij", suffix);
    }

    #endregion

    #region Until Field Tests

    [TestMethod]
    public void Parse_UntilDelimiter_ShouldParseCorrectly()
    {
        // Arrange: text Schema { Key: until ':' }
        var interpreter = CreateAndCompileInterpreter("TestSchema",
            CreateTextField("Key", TextFieldType.Until, ":"));

        var data = "Hello:World";

        // Act
        var result = InvokeParse(interpreter, data);
        var value = GetPropertyValue<string>(result, "Key");

        // Assert
        Assert.AreEqual("Hello", value);
    }

    [TestMethod]
    public void Parse_UntilWithLiteral_ShouldParseSequentially()
    {
        // Arrange: text Schema { Key: until ':', Value: rest trim }
        // Note: ReadUntil consumes the delimiter ':', so we just need to skip the space after
        var interpreter = CreateAndCompileInterpreter("KeyValue",
            CreateTextField("Key", TextFieldType.Until, ":"),
            CreateTextField("Value", TextFieldType.Rest, null, null, TextFieldModifier.Trim));

        var data = "Name: John Doe";

        // Act
        var result = InvokeParse(interpreter, data);
        var key = GetPropertyValue<string>(result, "Key");
        var value = GetPropertyValue<string>(result, "Value");

        // Assert
        Assert.AreEqual("Name", key);
        Assert.AreEqual("John Doe", value); // Value is trimmed, so leading space is removed
    }

    #endregion

    #region Between Field Tests

    [TestMethod]
    public void Parse_BetweenDelimiters_ShouldParseCorrectly()
    {
        // Arrange: text Schema { Value: between '[' ']' }
        var interpreter = CreateAndCompileInterpreter("TestSchema",
            CreateTextField("Value", TextFieldType.Between, "[", "]"));

        var data = "[Hello World]";

        // Act
        var result = InvokeParse(interpreter, data);
        var value = GetPropertyValue<string>(result, "Value");

        // Assert
        Assert.AreEqual("Hello World", value);
    }

    [TestMethod]
    public void Parse_BetweenWithQuotes_ShouldParseCorrectly()
    {
        // Arrange: text Schema { Value: between '"' '"' }
        var interpreter = CreateAndCompileInterpreter("TestSchema",
            CreateTextField("Value", TextFieldType.Between, "\"", "\""));

        var data = "\"quoted text\"";

        // Act
        var result = InvokeParse(interpreter, data);
        var value = GetPropertyValue<string>(result, "Value");

        // Assert
        Assert.AreEqual("quoted text", value);
    }

    #endregion

    #region Chars Field Tests

    [TestMethod]
    public void Parse_FixedChars_ShouldParseCorrectly()
    {
        // Arrange: text Schema { Code: chars[5] }
        var interpreter = CreateAndCompileInterpreter("TestSchema",
            CreateTextField("Code", TextFieldType.Chars, "5"));

        var data = "ABCDEFGHIJ";

        // Act
        var result = InvokeParse(interpreter, data);
        var value = GetPropertyValue<string>(result, "Code");

        // Assert
        Assert.AreEqual("ABCDE", value);
    }

    [TestMethod]
    public void Parse_FixedCharsWithTrim_ShouldTrimResult()
    {
        // Arrange: text Schema { Code: chars[10] trim }
        var interpreter = CreateAndCompileInterpreter("TestSchema",
            CreateTextField("Code", TextFieldType.Chars, "10", modifiers: TextFieldModifier.Trim));

        var data = "  ABC     ";

        // Act
        var result = InvokeParse(interpreter, data);
        var value = GetPropertyValue<string>(result, "Code");

        // Assert
        Assert.AreEqual("ABC", value);
    }

    #endregion

    #region Token Field Tests

    [TestMethod]
    public void Parse_Token_ShouldParseCorrectly()
    {
        // Arrange: text Schema { Word: token }
        var interpreter = CreateAndCompileInterpreter("TestSchema",
            CreateTextField("Word", TextFieldType.Token));

        var data = "Hello World";

        // Act
        var result = InvokeParse(interpreter, data);
        var value = GetPropertyValue<string>(result, "Word");

        // Assert
        Assert.AreEqual("Hello", value);
    }

    [TestMethod]
    public void Parse_TokenWithWhitespace_ShouldParseMultipleTokens()
    {
        // Arrange: text Schema { Word1: token, _: whitespace, Word2: token }
        var interpreter = CreateAndCompileInterpreter("TwoWords",
            CreateTextField("Word1", TextFieldType.Token),
            CreateTextField("_", TextFieldType.Whitespace),
            CreateTextField("Word2", TextFieldType.Token));

        var data = "Hello World";

        // Act
        var result = InvokeParse(interpreter, data);
        var word1 = GetPropertyValue<string>(result, "Word1");
        var word2 = GetPropertyValue<string>(result, "Word2");

        // Assert
        Assert.AreEqual("Hello", word1);
        Assert.AreEqual("World", word2);
    }

    #endregion

    #region Complex Schema Tests

    [TestMethod]
    public void Parse_LogEntry_ShouldParseAllFields()
    {
        // Arrange: text LogEntry {
        //     Timestamp: between '[' ']',
        //     _: literal ' ',       // Space after ']'
        //     Level: until ':',     // Consumes the ':'
        //     _: literal ' ',       // Space after ':'
        //     Message: rest
        // }
        var interpreter = CreateAndCompileInterpreter("LogEntry",
            CreateTextField("Timestamp", TextFieldType.Between, "[", "]"),
            CreateTextField("_", TextFieldType.Literal, " "),
            CreateTextField("Level", TextFieldType.Until, ":"),
            CreateTextField("_", TextFieldType.Literal, " "), // Only space, colon was consumed by Until
            CreateTextField("Message", TextFieldType.Rest));

        var data = "[2024-01-15 10:30:45] INFO: Application started successfully";

        // Act
        var result = InvokeParse(interpreter, data);
        var timestamp = GetPropertyValue<string>(result, "Timestamp");
        var level = GetPropertyValue<string>(result, "Level");
        var message = GetPropertyValue<string>(result, "Message");

        // Assert
        Assert.AreEqual("2024-01-15 10:30:45", timestamp);
        Assert.AreEqual("INFO", level);
        Assert.AreEqual("Application started successfully", message);
    }

    [TestMethod]
    public void Parse_CsvRow_ShouldParseCsvFormat()
    {
        // Arrange: text CsvRow {
        //     Name: until ',',    // Consumes the ','
        //     Age: until ',',     // Consumes the ','
        //     City: rest
        // }
        // Note: No literal needed after 'until' since it consumes the delimiter
        var interpreter = CreateAndCompileInterpreter("CsvRow",
            CreateTextField("Name", TextFieldType.Until, ","),
            CreateTextField("Age", TextFieldType.Until, ","),
            CreateTextField("City", TextFieldType.Rest));

        var data = "John Doe,30,New York";

        // Act
        var result = InvokeParse(interpreter, data);
        var name = GetPropertyValue<string>(result, "Name");
        var age = GetPropertyValue<string>(result, "Age");
        var city = GetPropertyValue<string>(result, "City");

        // Assert
        Assert.AreEqual("John Doe", name);
        Assert.AreEqual("30", age);
        Assert.AreEqual("New York", city);
    }

    #endregion

    #region Optional Field Tests

    [TestMethod]
    public void Parse_OptionalField_WhenPresent_ShouldCaptureValue()
    {
        // Arrange: text Schema { Key: until ':', _: literal ' ', Value: optional until '\t', Extra: rest }
        var interpreter = CreateAndCompileInterpreter("LogEntry",
            CreateTextField("Key", TextFieldType.Until, ":"),
            CreateTextField("_", TextFieldType.Literal, " "),
            CreateTextField("Value", TextFieldType.Until, "\t", null, TextFieldModifier.Optional),
            CreateTextField("Extra", TextFieldType.Rest));

        // Tab exists, so Value should capture "Hello"
        var data = "Name: Hello\tWorld";

        // Act
        var result = InvokeParse(interpreter, data);
        var key = GetPropertyValue<string>(result, "Key");
        var value = GetPropertyValue<string?>(result, "Value");
        var extra = GetPropertyValue<string>(result, "Extra");

        // Assert
        Assert.AreEqual("Name", key);
        Assert.AreEqual("Hello", value);
        Assert.AreEqual("World", extra);
    }

    [TestMethod]
    public void Parse_OptionalField_WhenAbsent_ShouldReturnNull()
    {
        // Arrange: text Schema { Key: until ':', _: literal ' ', Value: optional until '\t', Extra: rest }
        var interpreter = CreateAndCompileInterpreter("LogEntry",
            CreateTextField("Key", TextFieldType.Until, ":"),
            CreateTextField("_", TextFieldType.Literal, " "),
            CreateTextField("Value", TextFieldType.Until, "\t", null, TextFieldModifier.Optional),
            CreateTextField("Extra", TextFieldType.Rest));

        // No tab exists, so Value should be null and cursor should not advance
        var data = "Name: Hello World";

        // Act
        var result = InvokeParse(interpreter, data);
        var key = GetPropertyValue<string>(result, "Key");
        var value = GetNullablePropertyValue<string>(result, "Value");
        var extra = GetPropertyValue<string>(result, "Extra");

        // Assert
        Assert.AreEqual("Name", key);
        Assert.IsNull(value, "Value should be null when optional field fails");
        Assert.AreEqual("Hello World", extra, "Extra should contain remaining text since optional field failed");
    }

    [TestMethod]
    public void Parse_OptionalLiteral_WhenPresent_ShouldConsumeLiteral()
    {
        // Arrange: text Schema { Key: until ':', _: optional literal '\t', Value: rest }
        var interpreter = CreateAndCompileInterpreter("TabSeparated",
            CreateTextField("Key", TextFieldType.Until, ":"),
            CreateTextField("_", TextFieldType.Literal, "\t", null, TextFieldModifier.Optional),
            CreateTextField("Value", TextFieldType.Rest));

        // Tab exists after colon
        var data = "Name:\tJohn";

        // Act
        var result = InvokeParse(interpreter, data);
        var key = GetPropertyValue<string>(result, "Key");
        var value = GetPropertyValue<string>(result, "Value");

        // Assert
        Assert.AreEqual("Name", key);
        Assert.AreEqual("John", value, "Value should start after the tab");
    }

    [TestMethod]
    public void Parse_OptionalLiteral_WhenAbsent_ShouldNotConsumeLiteral()
    {
        // Arrange: text Schema { Key: until ':', _: optional literal '\t', Value: rest }
        var interpreter = CreateAndCompileInterpreter("TabSeparated",
            CreateTextField("Key", TextFieldType.Until, ":"),
            CreateTextField("_", TextFieldType.Literal, "\t", null, TextFieldModifier.Optional),
            CreateTextField("Value", TextFieldType.Rest));

        // No tab after colon (space instead)
        var data = "Name: John";

        // Act
        var result = InvokeParse(interpreter, data);
        var key = GetPropertyValue<string>(result, "Key");
        var value = GetPropertyValue<string>(result, "Value");

        // Assert
        Assert.AreEqual("Name", key);
        Assert.AreEqual(" John", value, "Value should include space since optional tab was not found");
    }

    [TestMethod]
    public void Parse_OptionalPattern_WhenPresent_ShouldCaptureMatch()
    {
        // Arrange: text Schema { Prefix: until ' ', TraceId: optional pattern '[a-f0-9]{8}', Rest: rest }
        var interpreter = CreateAndCompileInterpreter("TraceLog",
            CreateTextField("Prefix", TextFieldType.Until, " "),
            CreateTextField("TraceId", TextFieldType.Pattern, "[a-f0-9]{8}", null, TextFieldModifier.Optional),
            CreateTextField("Rest", TextFieldType.Rest));

        // Hex trace ID present
        var data = "INFO deadbeef remaining";

        // Act
        var result = InvokeParse(interpreter, data);
        var prefix = GetPropertyValue<string>(result, "Prefix");
        var traceId = GetNullablePropertyValue<string>(result, "TraceId");
        var rest = GetPropertyValue<string>(result, "Rest");

        // Assert
        Assert.AreEqual("INFO", prefix);
        Assert.AreEqual("deadbeef", traceId);
        Assert.AreEqual(" remaining", rest);
    }

    [TestMethod]
    public void Parse_OptionalPattern_WhenAbsent_ShouldReturnNull()
    {
        // Arrange: text Schema { Prefix: until ' ', TraceId: optional pattern '[a-f0-9]{8}', Rest: rest }
        var interpreter = CreateAndCompileInterpreter("TraceLog",
            CreateTextField("Prefix", TextFieldType.Until, " "),
            CreateTextField("TraceId", TextFieldType.Pattern, "[a-f0-9]{8}", null, TextFieldModifier.Optional),
            CreateTextField("Rest", TextFieldType.Rest));

        // No valid hex trace ID
        var data = "INFO xyz12345";

        // Act
        var result = InvokeParse(interpreter, data);
        var prefix = GetPropertyValue<string>(result, "Prefix");
        var traceId = GetNullablePropertyValue<string>(result, "TraceId");
        var rest = GetPropertyValue<string>(result, "Rest");

        // Assert
        Assert.AreEqual("INFO", prefix);
        Assert.IsNull(traceId, "TraceId should be null when pattern doesn't match");
        Assert.AreEqual("xyz12345", rest, "Rest should contain remaining text");
    }

    #endregion

    #region Helper Methods

    private object CreateAndCompileInterpreter(string schemaName, params TextFieldDefinitionNode[] fields)
    {
        var registry = new SchemaRegistry();
        var schema = new TextSchemaNode(schemaName, fields);
        registry.Register(schemaName, schema);


        var generator = new InterpreterCodeGenerator(registry);
        var code = generator.GenerateAll();

        var compilationUnit = new InterpreterCompilationUnit(
            $"TestAssembly_{Guid.NewGuid():N}",
            code);

        var success = compilationUnit.Compile();
        if (!success)
        {
            var errors = string.Join(Environment.NewLine, compilationUnit.GetErrorMessages());
            Assert.Fail($"Compilation failed: {errors}");
        }


        var interpreterType = compilationUnit.GetInterpreterType(schemaName);
        Assert.IsNotNull(interpreterType, $"Interpreter type for '{schemaName}' not found");

        return Activator.CreateInstance(interpreterType)!;
    }

    private object InvokeParse(object interpreter, string data)
    {
        var parseMethod = interpreter.GetType().GetMethod("Parse", new[] { typeof(string) });
        if (parseMethod == null) throw new InvalidOperationException("Parse(string) method not found on interpreter");

        return parseMethod.Invoke(interpreter, new object[] { data })!;
    }

    private T GetPropertyValue<T>(object obj, string propertyName)
    {
        var property = obj.GetType().GetProperty(propertyName);
        if (property == null) Assert.Fail($"Property '{propertyName}' not found on type {obj.GetType().Name}");
        return (T)property.GetValue(obj)!;
    }

    private T? GetNullablePropertyValue<T>(object obj, string propertyName) where T : class
    {
        var property = obj.GetType().GetProperty(propertyName);
        if (property == null) Assert.Fail($"Property '{propertyName}' not found on type {obj.GetType().Name}");
        return (T?)property.GetValue(obj);
    }

    private static TextFieldDefinitionNode CreateTextField(
        string name,
        TextFieldType fieldType,
        string? primaryValue = null,
        string? secondaryValue = null,
        TextFieldModifier modifiers = TextFieldModifier.None)
    {
        return new TextFieldDefinitionNode(name, fieldType, primaryValue, secondaryValue, modifiers);
    }

    #endregion

    #region Session 7: Text Schema Expansion

    /// <summary>
    ///     Tests parsing with multiple trim modifiers.
    /// </summary>
    [TestMethod]
    public void Parse_Rest_WithTrim_ShouldTrimWhitespace()
    {
        var interpreter = CreateAndCompileInterpreter("TestSchema",
            CreateTextField("Value", TextFieldType.Rest, null, null, TextFieldModifier.Trim));

        var data = "   Hello World   ";

        var result = InvokeParse(interpreter, data);
        var value = GetPropertyValue<string>(result, "Value");

        Assert.AreEqual("Hello World", value);
    }

    /// <summary>
    ///     Tests parsing with fixed-width characters.
    /// </summary>
    [TestMethod]
    public void Parse_Chars_ExactWidth_ShouldParseCorrectly()
    {
        var interpreter = CreateAndCompileInterpreter("TestSchema",
            CreateTextField("Code", TextFieldType.Chars, "5"),
            CreateTextField("Name", TextFieldType.Rest));

        var data = "ABCDEJohn Doe";

        var result = InvokeParse(interpreter, data);
        var code = GetPropertyValue<string>(result, "Code");
        var name = GetPropertyValue<string>(result, "Name");

        Assert.AreEqual("ABCDE", code);
        Assert.AreEqual("John Doe", name);
    }

    /// <summary>
    ///     Tests parsing with multiple delimited fields.
    /// </summary>
    [TestMethod]
    public void Parse_MultipleUntilFields_ShouldParseSequentially()
    {
        var interpreter = CreateAndCompileInterpreter("TestSchema",
            CreateTextField("Field1", TextFieldType.Until, "|"),
            CreateTextField("Field2", TextFieldType.Until, "|"),
            CreateTextField("Field3", TextFieldType.Rest));

        var data = "A|B|C";

        var result = InvokeParse(interpreter, data);

        Assert.AreEqual("A", GetPropertyValue<string>(result, "Field1"));
        Assert.AreEqual("B", GetPropertyValue<string>(result, "Field2"));
        Assert.AreEqual("C", GetPropertyValue<string>(result, "Field3"));
    }

    /// <summary>
    ///     Tests parsing nested brackets with between - captures up to first end delimiter.
    /// </summary>
    [TestMethod]
    public void Parse_Between_NestedDelimiters_CapturesUpToFirstEndDelimiter()
    {
        var interpreter = CreateAndCompileInterpreter("TestSchema",
            CreateTextField("Content", TextFieldType.Between, "(", ")"));

        var data = "(outer (inner) value)";

        var result = InvokeParse(interpreter, data);
        var content = GetPropertyValue<string>(result, "Content");


        Assert.AreEqual("outer (inner", content);
    }

    /// <summary>
    ///     Tests parsing with empty field values.
    /// </summary>
    [TestMethod]
    public void Parse_Until_EmptyValue_ShouldReturnEmptyString()
    {
        var interpreter = CreateAndCompileInterpreter("TestSchema",
            CreateTextField("Key", TextFieldType.Until, "="),
            CreateTextField("Value", TextFieldType.Rest));

        var data = "=SomeValue";

        var result = InvokeParse(interpreter, data);

        Assert.AreEqual("", GetPropertyValue<string>(result, "Key"));
        Assert.AreEqual("SomeValue", GetPropertyValue<string>(result, "Value"));
    }

    /// <summary>
    ///     Tests parsing CSV-like format with multiple columns.
    /// </summary>
    [TestMethod]
    public void Parse_CsvLine_MultipleColumns_ShouldParseAll()
    {
        var interpreter = CreateAndCompileInterpreter("CsvLine",
            CreateTextField("Col1", TextFieldType.Until, ","),
            CreateTextField("Col2", TextFieldType.Until, ","),
            CreateTextField("Col3", TextFieldType.Until, ","),
            CreateTextField("Col4", TextFieldType.Rest));

        var data = "John,Doe,30,Engineer";

        var result = InvokeParse(interpreter, data);

        Assert.AreEqual("John", GetPropertyValue<string>(result, "Col1"));
        Assert.AreEqual("Doe", GetPropertyValue<string>(result, "Col2"));
        Assert.AreEqual("30", GetPropertyValue<string>(result, "Col3"));
        Assert.AreEqual("Engineer", GetPropertyValue<string>(result, "Col4"));
    }

    /// <summary>
    ///     Tests parsing key-value with colon separator and trimming.
    /// </summary>
    [TestMethod]
    public void Parse_KeyValueWithTrim_ShouldTrimBothParts()
    {
        var interpreter = CreateAndCompileInterpreter("KeyValue",
            CreateTextField("Key", TextFieldType.Until, ":", null, TextFieldModifier.Trim),
            CreateTextField("Value", TextFieldType.Rest, null, null, TextFieldModifier.Trim));

        var data = "  Name  :  John Doe  ";

        var result = InvokeParse(interpreter, data);

        Assert.AreEqual("Name", GetPropertyValue<string>(result, "Key"));
        Assert.AreEqual("John Doe", GetPropertyValue<string>(result, "Value"));
    }

    /// <summary>
    ///     Tests parsing with multi-character delimiter.
    /// </summary>
    [TestMethod]
    public void Parse_Until_MultiCharDelimiter_ShouldWork()
    {
        var interpreter = CreateAndCompileInterpreter("TestSchema",
            CreateTextField("Before", TextFieldType.Until, "::"),
            CreateTextField("After", TextFieldType.Rest));

        var data = "Key::Value";

        var result = InvokeParse(interpreter, data);

        Assert.AreEqual("Key", GetPropertyValue<string>(result, "Before"));
        Assert.AreEqual("Value", GetPropertyValue<string>(result, "After"));
    }

    #endregion
}
