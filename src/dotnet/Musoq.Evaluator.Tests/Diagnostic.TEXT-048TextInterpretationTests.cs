using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Tests;

public partial class TextInterpretationTests
{
    [TestMethod]
    public void Parse_TextFieldModifiers_ShouldApplyAcrossCaptureTypes()
    {
        var interpreter = CreateAndCompileInterpreter("ModifierRecord",
            CreateTextField("Code", TextFieldType.Chars, "4", modifiers: TextFieldModifier.Trim | TextFieldModifier.Upper),
            CreateTextField("Gap1", TextFieldType.Whitespace, "*"),
            CreateTextField("Word", TextFieldType.Token, modifiers: TextFieldModifier.Lower),
            CreateTextField("Gap2", TextFieldType.Whitespace, "*"),
            CreateTextField("Tail", TextFieldType.Rest, modifiers: TextFieldModifier.Upper));

        var result = InvokeParse(interpreter, " abc  DEF   tail");

        Assert.AreEqual("ABC", GetPropertyValue<string>(result, "Code"));
        Assert.AreEqual("def", GetPropertyValue<string>(result, "Word"));
        Assert.AreEqual("  ", GetPropertyValue<string>(result, "Gap1"));
        Assert.AreEqual("   ", GetPropertyValue<string>(result, "Gap2"));
        Assert.AreEqual("TAIL", GetPropertyValue<string>(result, "Tail"));
    }

    [TestMethod]
    public void Parse_NamedWhitespaceQuestion_ShouldCaptureOneOrZeroCharacters()
    {
        var interpreter = CreateAndCompileInterpreter("WhitespaceRecord",
            CreateTextField("Gap", TextFieldType.Whitespace, "?"),
            CreateTextField("Tail", TextFieldType.Rest));

        var result = InvokeParse(interpreter, "  value");

        Assert.AreEqual(" ", GetPropertyValue<string>(result, "Gap"));
        Assert.AreEqual(" value", GetPropertyValue<string>(result, "Tail"));
    }

    [TestMethod]
    public void Parse_TextUntilGreedyAndLazy_ShouldControlDelimiterSelection()
    {
        var greedy = CreateAndCompileInterpreter("GreedyRecord",
            CreateTextField("Value", TextFieldType.Until, ",", modifiers: TextFieldModifier.Greedy),
            CreateTextField("Tail", TextFieldType.Rest));
        var lazy = CreateAndCompileInterpreter("LazyRecord",
            CreateTextField("Value", TextFieldType.Until, ",", modifiers: TextFieldModifier.Lazy),
            CreateTextField("Tail", TextFieldType.Rest));

        var greedyResult = InvokeParse(greedy, "a,b,c");
        var lazyResult = InvokeParse(lazy, "a,b,c");

        Assert.AreEqual("a,b", GetPropertyValue<string>(greedyResult, "Value"));
        Assert.AreEqual("c", GetPropertyValue<string>(greedyResult, "Tail"));
        Assert.AreEqual("a", GetPropertyValue<string>(lazyResult, "Value"));
        Assert.AreEqual("b,c", GetPropertyValue<string>(lazyResult, "Tail"));
    }

    [TestMethod]
    public void Parse_TextPatternLazy_ShouldMinimizeQuantifierMatch()
    {
        var interpreter = CreateAndCompileInterpreter("LazyPatternRecord",
            CreateTextField("Value", TextFieldType.Pattern, ".*,", modifiers: TextFieldModifier.Lazy),
            CreateTextField("Tail", TextFieldType.Rest));

        var result = InvokeParse(interpreter, "a,b,c");

        Assert.AreEqual("a,", GetPropertyValue<string>(result, "Value"));
        Assert.AreEqual("b,c", GetPropertyValue<string>(result, "Tail"));
    }

    [TestMethod]
    public void Parse_CharsInsufficientData_ShouldReportFieldAndPosition()
    {
        var interpreter = CreateAndCompileInterpreter("FixedRecord",
            CreateTextField("Code", TextFieldType.Chars, "4"));

        var wrapper = Assert.ThrowsExactly<TargetInvocationException>(() => InvokeParse(interpreter, "abc"));
        Assert.IsNotNull(wrapper.InnerException);
        Assert.IsInstanceOfType<ParseException>(wrapper.InnerException);
        var exception = (ParseException)wrapper.InnerException!;

        Assert.AreEqual(ParseErrorCode.InsufficientData, exception.ErrorCode);
        Assert.AreEqual("Code", exception.FieldName);
        Assert.AreEqual(0, exception.Position);
    }
}
