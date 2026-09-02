using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Tests;

public partial class TextInterpretationTests
{
    [TestMethod]
    public void Parse_TextPatternCapture_ShouldExposeUnicodeNamedGroups()
    {
        var interpreter = CreateAndCompileInterpreter(
            "UnicodeCapture",
            new TextFieldDefinitionNode(
                "Code",
                TextFieldType.Pattern,
                @"(?<Prefix>[A-Z]+)-(?<Word>\p{L}+)",
                captureGroups: ["Prefix", "Word"]),
            CreateTextField("Tail", TextFieldType.Rest));

        var result = InvokeParse(interpreter, "ABC-Łódź!");
        var capture = GetPropertyValue<object>(result, "Code");

        Assert.AreEqual("ABC", GetPropertyValue<string>(capture, "Prefix"));
        Assert.AreEqual("Łódź", GetPropertyValue<string>(capture, "Word"));
        Assert.AreEqual("!", GetPropertyValue<string>(result, "Tail"));
    }

    [TestMethod]
    public void Parse_TextPatternCapture_WithCSharpKeywordGroup_ShouldCompileAndExposeGroup()
    {
        var interpreter = CreateAndCompileInterpreter(
            "KeywordCapture",
            new TextFieldDefinitionNode(
                "Code",
                TextFieldType.Pattern,
                @"(?<class>[A-Z]+)",
                captureGroups: ["class"]));

        var result = InvokeParse(interpreter, "ABC");
        var capture = GetPropertyValue<object>(result, "Code");

        Assert.AreEqual("ABC", GetPropertyValue<string>(capture, "class"));
    }

    [TestMethod]
    public void Parse_TextBetweenEscaped_CustomDoubledEscape_ShouldDecodeAndContinue()
    {
        var interpreter = CreateAndCompileInterpreter(
            "EscapedDoubled",
            new TextFieldDefinitionNode("Value", TextFieldType.Between, "[", "]", TextFieldModifier.Escaped, "~"),
            CreateTextField("Tail", TextFieldType.Rest));

        var result = InvokeParse(interpreter, "[a~~]b]tail");

        Assert.AreEqual("a~", GetPropertyValue<string>(result, "Value"));
        Assert.AreEqual("b]tail", GetPropertyValue<string>(result, "Tail"));
    }

    [TestMethod]
    public void Parse_TextBetweenNested_ShouldBalanceRepeatedUnicodeDelimiters()
    {
        var interpreter = CreateAndCompileInterpreter(
            "NestedUnicode",
            new TextFieldDefinitionNode("Value", TextFieldType.Between, "{", "}", TextFieldModifier.Nested),
            CreateTextField("Tail", TextFieldType.Rest));

        var result = InvokeParse(interpreter, "{α{β}γ}tail");

        Assert.AreEqual("α{β}γ", GetPropertyValue<string>(result, "Value"));
        Assert.AreEqual("tail", GetPropertyValue<string>(result, "Tail"));
    }

    [TestMethod]
    public void Parse_TextPatternFailureAfterPrefix_ShouldPreserveExactRuntimeLocation()
    {
        var interpreter = CreateAndCompileInterpreter(
            "PatternFailure",
            CreateTextField("Prefix", TextFieldType.Chars, "2"),
            CreateTextField("Digits", TextFieldType.Pattern, @"\d+"));

        var wrapper = Assert.ThrowsExactly<TargetInvocationException>(
            () => InvokeParse(interpreter, "xxabc"));
        var exception = GetParseException(wrapper);

        Assert.AreEqual(ParseErrorCode.PatternMismatch, exception.ErrorCode);
        Assert.AreEqual("PatternFailure", exception.SchemaName);
        Assert.AreEqual("Digits", exception.FieldName);
        Assert.AreEqual(2, exception.Position);
        Assert.AreEqual("ISE0003", exception.FormattedErrorCode);
    }

    [TestMethod]
    public void Parse_TextLiteralFailureAfterPrefix_ShouldPreserveExactRuntimeLocation()
    {
        var interpreter = CreateAndCompileInterpreter(
            "LiteralFailure",
            CreateTextField("Prefix", TextFieldType.Chars, "2"),
            CreateTextField("Marker", TextFieldType.Literal, "OK"));

        var wrapper = Assert.ThrowsExactly<TargetInvocationException>(
            () => InvokeParse(interpreter, "xxok"));
        var exception = GetParseException(wrapper);

        Assert.AreEqual(ParseErrorCode.LiteralMismatch, exception.ErrorCode);
        Assert.AreEqual("LiteralFailure", exception.SchemaName);
        Assert.AreEqual("Marker", exception.FieldName);
        Assert.AreEqual(2, exception.Position);
        Assert.AreEqual("ISE0004", exception.FormattedErrorCode);
    }
}
