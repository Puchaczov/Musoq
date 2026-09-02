using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Tests;

public partial class TextInterpretationTests
{
    [TestMethod]
    public void Parse_TextPattern_ShouldMatchAtCurrentPositionAndAdvanceCursor()
    {
        var interpreter = CreateAndCompileInterpreter("PatternRecord",
            CreateTextField("Number", TextFieldType.Pattern, @"\d+"),
            CreateTextField("Marker", TextFieldType.Literal, ":"),
            CreateTextField("Tail", TextFieldType.Rest));

        var result = InvokeParse(interpreter, "123:tail");

        Assert.AreEqual("123", GetPropertyValue<string>(result, "Number"));
        Assert.AreEqual("tail", GetPropertyValue<string>(result, "Tail"));
    }

    [TestMethod]
    public void Parse_TextPatternMismatch_ShouldReportFieldAndPosition()
    {
        var interpreter = CreateAndCompileInterpreter("PatternRecord",
            CreateTextField("Number", TextFieldType.Pattern, @"\d+"));

        var wrapper = Assert.ThrowsExactly<TargetInvocationException>(
            () => InvokeParse(interpreter, "abc"));
        Assert.IsNotNull(wrapper.InnerException);
        Assert.IsInstanceOfType<ParseException>(wrapper.InnerException);
        var exception = (ParseException)wrapper.InnerException!;
        Assert.AreEqual(ParseErrorCode.PatternMismatch, exception.ErrorCode);
        Assert.AreEqual("Number", exception.FieldName);
        Assert.AreEqual(0, exception.Position);
    }

    [TestMethod]
    public void Parse_TextLiteral_ShouldBeCaseSensitiveAndAdvanceCursor()
    {
        var interpreter = CreateAndCompileInterpreter("LiteralRecord",
            CreateTextField("Marker", TextFieldType.Literal, "OK"),
            CreateTextField("Tail", TextFieldType.Rest));

        var result = InvokeParse(interpreter, "OKvalue");

        Assert.AreEqual("OK", GetPropertyValue<string>(result, "Marker"));
        Assert.AreEqual("value", GetPropertyValue<string>(result, "Tail"));
    }

    [TestMethod]
    public void Parse_TextLiteralMismatch_ShouldReportFieldAndPosition()
    {
        var interpreter = CreateAndCompileInterpreter("LiteralRecord",
            CreateTextField("Marker", TextFieldType.Literal, "OK"));

        var wrapper = Assert.ThrowsExactly<TargetInvocationException>(
            () => InvokeParse(interpreter, "ok"));
        Assert.IsNotNull(wrapper.InnerException);
        Assert.IsInstanceOfType<ParseException>(wrapper.InnerException);
        var exception = (ParseException)wrapper.InnerException!;
        Assert.AreEqual(ParseErrorCode.LiteralMismatch, exception.ErrorCode);
        Assert.AreEqual("Marker", exception.FieldName);
        Assert.AreEqual(0, exception.Position);
    }

    [TestMethod]
    public void Parse_TextUntil_ShouldConsumeDelimiterBeforeFollowingBetween()
    {
        var interpreter = CreateAndCompileInterpreter("DelimiterRecord",
            CreateTextField("Prefix", TextFieldType.Until, ":"),
            CreateTextField("Value", TextFieldType.Between, ":", ";"));

        var result = InvokeParse(interpreter, "key::body;");

        Assert.AreEqual("key", GetPropertyValue<string>(result, "Prefix"));
        Assert.AreEqual("body", GetPropertyValue<string>(result, "Value"));
    }

    [TestMethod]
    public void Parse_TextBetweenNested_ShouldCaptureExclusiveBalancedContent()
    {
        var interpreter = CreateAndCompileInterpreter("NestedRecord",
            CreateTextField("Value", TextFieldType.Between, "(", ")", TextFieldModifier.Nested),
            CreateTextField("Tail", TextFieldType.Rest));

        var result = InvokeParse(interpreter, "(a(b)c)tail");

        Assert.AreEqual("a(b)c", GetPropertyValue<string>(result, "Value"));
        Assert.AreEqual("tail", GetPropertyValue<string>(result, "Tail"));
    }

    [TestMethod]
    public void Parse_TextBetweenEscaped_ShouldUseCustomEscapeCharacter()
    {
        var interpreter = CreateAndCompileInterpreter("EscapedRecord",
            new TextFieldDefinitionNode("Value", TextFieldType.Between, "[", "]", TextFieldModifier.Escaped, "~"),
            CreateTextField("Tail", TextFieldType.Rest));

        var result = InvokeParse(interpreter, "[a~]b]tail");

        Assert.AreEqual("a~]b", GetPropertyValue<string>(result, "Value"));
        Assert.AreEqual("tail", GetPropertyValue<string>(result, "Tail"));
    }
}
