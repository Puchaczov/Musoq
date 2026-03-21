using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Parser.Tests;

/// <summary>
///     Tests for the SchemaParser class - Text schema parsing.
/// </summary>
[TestClass]
public class SchemaParser_TextTests : SchemaParserTestsBase
{
    #region Text Schema - Token Field

    [TestMethod]
    public void TextSchema_TokenField_ShouldParse()
    {
        var schema = "text T { Word: token }";
        var result = ParseTextSchema(schema);

        var field = result.Fields[0];
        Assert.AreEqual(TextFieldType.Token, field.FieldType);
    }

    #endregion

    #region Text Schema - Rest Field

    [TestMethod]
    public void TextSchema_RestField_ShouldParse()
    {
        var schema = "text T { Remainder: rest }";
        var result = ParseTextSchema(schema);

        var field = result.Fields[0];
        Assert.AreEqual(TextFieldType.Rest, field.FieldType);
    }

    #endregion

    #region Text Schema - Basic Structure

    [TestMethod]
    public void TextSchema_EmptySchema_ShouldParse()
    {
        var schema = "text Empty { }";
        var result = ParseTextSchema(schema);

        Assert.IsNotNull(result);
        Assert.AreEqual("Empty", result.Name);
        Assert.IsEmpty(result.Fields);
    }

    [TestMethod]
    public void TextSchema_WithExtends_ShouldParse()
    {
        var schema = "text Derived extends Base { }";
        var result = ParseTextSchema(schema);

        Assert.AreEqual("Derived", result.Name);
        Assert.AreEqual("Base", result.Extends);
    }

    #endregion

    #region Text Schema - Whitespace Field

    [TestMethod]
    public void TextSchema_WhitespaceField_ShouldParse()
    {
        var schema = "text T { _: whitespace }";
        var result = ParseTextSchema(schema);

        var field = result.Fields[0];
        Assert.AreEqual(TextFieldType.Whitespace, field.FieldType);

        Assert.AreEqual("+", field.PrimaryValue);
    }

    [TestMethod]
    public void TextSchema_WhitespacePlus_ShouldParse()
    {
        var schema = "text T { _: whitespace+ }";
        var result = ParseTextSchema(schema);

        var field = result.Fields[0];
        Assert.AreEqual(TextFieldType.Whitespace, field.FieldType);
        Assert.AreEqual("+", field.PrimaryValue);
    }

    [TestMethod]
    public void TextSchema_WhitespaceStar_ShouldParse()
    {
        var schema = "text T { _: whitespace* }";
        var result = ParseTextSchema(schema);

        var field = result.Fields[0];
        Assert.AreEqual(TextFieldType.Whitespace, field.FieldType);
        Assert.AreEqual("*", field.PrimaryValue);
    }

    [TestMethod]
    public void TextSchema_WhitespaceQuestion_ShouldParse()
    {
        var schema = "text T { _: whitespace? }";
        var result = ParseTextSchema(schema);

        var field = result.Fields[0];
        Assert.AreEqual(TextFieldType.Whitespace, field.FieldType);
        Assert.AreEqual("?", field.PrimaryValue);
    }

    #endregion

    #region Text Switch Field Tests

    [TestMethod]
    public void TextSchema_SwitchField_SinglePattern_ShouldParse()
    {
        var schema = "text ConfigLine { Content: switch { pattern '\\\\s*\\\\[' => SectionHeader } }";
        var result = ParseTextSchema(schema);

        Assert.HasCount(1, result.Fields);
        var field = result.Fields[0];
        Assert.AreEqual("Content", field.Name);
        Assert.AreEqual(TextFieldType.Switch, field.FieldType);
        Assert.HasCount(1, field.SwitchCases);
        Assert.AreEqual("\\s*\\[", field.SwitchCases[0].Pattern);
        Assert.AreEqual("SectionHeader", field.SwitchCases[0].TypeName);
        Assert.IsFalse(field.SwitchCases[0].IsDefault);
    }

    [TestMethod]
    public void TextSchema_SwitchField_MultiplePatterns_ShouldParse()
    {
        var schema = @"text ConfigLine { 
            Content: switch { 
                pattern '\s*\[' => SectionHeader,
                pattern '\s*#' => Comment,
                pattern '\s*;' => Comment
            } 
        }";
        var result = ParseTextSchema(schema);

        var field = result.Fields[0];
        Assert.AreEqual(TextFieldType.Switch, field.FieldType);
        Assert.HasCount(3, field.SwitchCases);

        Assert.AreEqual("\\s*\\[", field.SwitchCases[0].Pattern);
        Assert.AreEqual("SectionHeader", field.SwitchCases[0].TypeName);

        Assert.AreEqual("\\s*#", field.SwitchCases[1].Pattern);
        Assert.AreEqual("Comment", field.SwitchCases[1].TypeName);

        Assert.AreEqual("\\s*;", field.SwitchCases[2].Pattern);
        Assert.AreEqual("Comment", field.SwitchCases[2].TypeName);
    }

    [TestMethod]
    public void TextSchema_SwitchField_WithDefaultCase_ShouldParse()
    {
        var schema = @"text ConfigLine { 
            Content: switch { 
                pattern '\s*\[' => SectionHeader,
                _ => KeyValue
            } 
        }";
        var result = ParseTextSchema(schema);

        var field = result.Fields[0];
        Assert.AreEqual(TextFieldType.Switch, field.FieldType);
        Assert.HasCount(2, field.SwitchCases);

        Assert.IsFalse(field.SwitchCases[0].IsDefault);
        Assert.AreEqual("SectionHeader", field.SwitchCases[0].TypeName);

        Assert.IsTrue(field.SwitchCases[1].IsDefault);
        Assert.IsNull(field.SwitchCases[1].Pattern);
        Assert.AreEqual("KeyValue", field.SwitchCases[1].TypeName);
    }

    [TestMethod]
    public void TextSchema_SwitchField_OnlyDefault_ShouldParse()
    {
        var schema = "text ConfigLine { Content: switch { _ => KeyValue } }";
        var result = ParseTextSchema(schema);

        var field = result.Fields[0];
        Assert.AreEqual(TextFieldType.Switch, field.FieldType);
        Assert.HasCount(1, field.SwitchCases);
        Assert.IsTrue(field.SwitchCases[0].IsDefault);
        Assert.AreEqual("KeyValue", field.SwitchCases[0].TypeName);
    }

    [TestMethod]
    public void TextSchema_SwitchField_WithoutTrailingComma_ShouldParse()
    {
        var schema = @"text ConfigLine { 
            Content: switch { 
                pattern '\s*\[' => SectionHeader
                pattern '\s*#' => Comment
            } 
        }";
        var result = ParseTextSchema(schema);

        var field = result.Fields[0];
        Assert.HasCount(2, field.SwitchCases);
    }

    [TestMethod]
    public void TextSchema_SwitchField_ToString_ShouldFormat()
    {
        var schema = @"text ConfigLine { 
            Content: switch { 
                pattern '\s*\[' => SectionHeader,
                _ => KeyValue
            } 
        }";
        var result = ParseTextSchema(schema);

        var field = result.Fields[0];
        var str = field.ToString();

        Assert.Contains("switch", str, "Should contain 'switch'");
        Assert.Contains("pattern", str, "Should contain 'pattern'");
        Assert.Contains("SectionHeader", str, "Should contain 'SectionHeader'");
        Assert.Contains("_ => KeyValue", str, "Should contain '_ => KeyValue'");
    }

    #endregion

    #region Optional Field Tests

    [TestMethod]
    public void TextSchema_OptionalPrefix_LiteralField_ShouldParse()
    {
        var schema = "text LogLine { _: optional literal '\\t' }";

        var result = ParseTextSchema(schema);

        Assert.HasCount(1, result.Fields);
        var field = result.Fields[0];
        Assert.AreEqual("_", field.Name);
        Assert.AreEqual(TextFieldType.Literal, field.FieldType);
        Assert.AreNotEqual((TextFieldModifier)0, field.Modifiers & TextFieldModifier.Optional,
            "Expected Optional modifier to be set");
    }

    [TestMethod]
    public void TextSchema_OptionalPrefix_PatternField_ShouldParse()
    {
        var schema = "text LogLine { TraceId: optional pattern '[a-f0-9]{32}' }";

        var result = ParseTextSchema(schema);

        Assert.HasCount(1, result.Fields);
        var field = result.Fields[0];
        Assert.AreEqual("TraceId", field.Name);
        Assert.AreEqual(TextFieldType.Pattern, field.FieldType);
        Assert.AreEqual("[a-f0-9]{32}", field.PrimaryValue);
        Assert.AreNotEqual((TextFieldModifier)0, field.Modifiers & TextFieldModifier.Optional,
            "Expected Optional modifier to be set");
    }

    [TestMethod]
    public void TextSchema_OptionalPrefix_UntilField_ShouldParse()
    {
        var schema = "text Line { Extra: optional until ',' }";

        var result = ParseTextSchema(schema);

        var field = result.Fields[0];
        Assert.AreEqual("Extra", field.Name);
        Assert.AreEqual(TextFieldType.Until, field.FieldType);
        Assert.AreNotEqual((TextFieldModifier)0, field.Modifiers & TextFieldModifier.Optional,
            "Expected Optional modifier to be set");
    }

    [TestMethod]
    public void TextSchema_OptionalPrefix_WithModifier_ShouldCombine()
    {
        var schema = "text Line { Data: optional until ',' trim }";

        var result = ParseTextSchema(schema);

        var field = result.Fields[0];
        Assert.AreEqual("Data", field.Name);
        Assert.AreNotEqual((TextFieldModifier)0, field.Modifiers & TextFieldModifier.Optional,
            "Expected Optional modifier");
        Assert.AreNotEqual((TextFieldModifier)0, field.Modifiers & TextFieldModifier.Trim, "Expected Trim modifier");
    }

    [TestMethod]
    public void TextSchema_OptionalPrefix_BetweenField_ShouldParse()
    {
        var schema = "text Config { Comment: optional between '/*' '*/' }";

        var result = ParseTextSchema(schema);

        var field = result.Fields[0];
        Assert.AreEqual("Comment", field.Name);
        Assert.AreEqual(TextFieldType.Between, field.FieldType);
        Assert.AreEqual("/*", field.PrimaryValue);
        Assert.AreEqual("*/", field.SecondaryValue);
        Assert.AreNotEqual((TextFieldModifier)0, field.Modifiers & TextFieldModifier.Optional,
            "Expected Optional modifier");
    }

    [TestMethod]
    public void TextSchema_OptionalPrefix_CharsField_ShouldParse()
    {
        var schema = "text Record { Suffix: optional chars[4] }";

        var result = ParseTextSchema(schema);

        var field = result.Fields[0];
        Assert.AreEqual("Suffix", field.Name);
        Assert.AreEqual(TextFieldType.Chars, field.FieldType);
        Assert.AreEqual("4", field.PrimaryValue);
        Assert.AreNotEqual((TextFieldModifier)0, field.Modifiers & TextFieldModifier.Optional,
            "Expected Optional modifier");
    }

    [TestMethod]
    public void TextSchema_NoOptionalPrefix_ShouldNotHaveOptionalModifier()
    {
        var schema = "text Line { Data: until ',' }";

        var result = ParseTextSchema(schema);

        var field = result.Fields[0];
        Assert.AreEqual((TextFieldModifier)0, field.Modifiers & TextFieldModifier.Optional,
            "Should not have Optional modifier");
    }

    [TestMethod]
    public void TextSchema_OptionalTrailingModifier_ShouldAlsoWork()
    {
        var schema = "text Line { Data: until ',' optional }";

        var result = ParseTextSchema(schema);

        var field = result.Fields[0];
        Assert.AreNotEqual((TextFieldModifier)0, field.Modifiers & TextFieldModifier.Optional,
            "Expected Optional modifier from trailing position");
    }

    #endregion

    #region Repeat Field Tests

    [TestMethod]
    public void TextSchema_RepeatField_WithUntilDelimiter_ShouldParse()
    {
        var schema = "text HttpHeaders { Headers: repeat HeaderLine until '\\r\\n' }";

        var result = ParseTextSchema(schema);

        Assert.AreEqual("HttpHeaders", result.Name);
        Assert.HasCount(1, result.Fields);

        var field = result.Fields[0];
        Assert.AreEqual("Headers", field.Name);
        Assert.AreEqual(TextFieldType.Repeat, field.FieldType);
        Assert.AreEqual("HeaderLine", field.PrimaryValue);
        Assert.AreEqual("\r\n", field.SecondaryValue);
    }

    [TestMethod]
    public void TextSchema_RepeatField_UntilEnd_ShouldParse()
    {
        var schema = "text AllLines { Lines: repeat Line until end }";

        var result = ParseTextSchema(schema);

        var field = result.Fields[0];
        Assert.AreEqual("Lines", field.Name);
        Assert.AreEqual(TextFieldType.Repeat, field.FieldType);
        Assert.AreEqual("Line", field.PrimaryValue);
        Assert.IsNull(field.SecondaryValue);
    }

    [TestMethod]
    public void TextSchema_RepeatField_NoUntilClause_ShouldDefaultToEnd()
    {
        var schema = "text AllLines { Lines: repeat Line }";

        var result = ParseTextSchema(schema);

        var field = result.Fields[0];
        Assert.AreEqual("Lines", field.Name);
        Assert.AreEqual(TextFieldType.Repeat, field.FieldType);
        Assert.AreEqual("Line", field.PrimaryValue);
        Assert.IsNull(field.SecondaryValue);
    }

    [TestMethod]
    public void TextSchema_RepeatField_WithSimpleDelimiter_ShouldParse()
    {
        var schema = "text CsvRow { Items: repeat Item until ',' }";

        var result = ParseTextSchema(schema);

        var field = result.Fields[0];
        Assert.AreEqual(TextFieldType.Repeat, field.FieldType);
        Assert.AreEqual("Item", field.PrimaryValue);
        Assert.AreEqual(",", field.SecondaryValue);
    }

    [TestMethod]
    public void TextSchema_RepeatField_ToString_WithDelimiter_ShouldFormat()
    {
        var schema = "text Test { Items: repeat Item until '\\n' }";

        var result = ParseTextSchema(schema);

        var field = result.Fields[0];
        var str = field.ToString();
        Assert.Contains("repeat Item", str, "Should contain 'repeat Item'");
        Assert.Contains("until", str, "Should contain 'until'");
    }

    [TestMethod]
    public void TextSchema_RepeatField_ToString_UntilEnd_ShouldFormat()
    {
        var schema = "text Test { Lines: repeat Line until end }";

        var result = ParseTextSchema(schema);

        var field = result.Fields[0];
        var str = field.ToString();
        Assert.Contains("repeat Line", str, "Should contain 'repeat Line'");
        Assert.Contains("until end", str, "Should contain 'until end'");
    }

    #endregion
}
