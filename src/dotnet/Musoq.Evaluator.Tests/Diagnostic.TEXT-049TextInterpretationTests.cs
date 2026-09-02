using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Build;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Tests;

public partial class TextInterpretationTests
{
    [TestMethod]
    public void Parse_OptionalPatternWithCaptureGroups_ShouldRestoreCursorAndUseCaptureShape()
    {
        var interpreter = CreateAndCompileInterpreter(
            "OptionalCapture",
            new TextFieldDefinitionNode(
                "Code",
                TextFieldType.Pattern,
                @"(?<Digits>\d+)",
                modifiers: TextFieldModifier.Optional,
                captureGroups: ["Digits"]),
            CreateTextField("Tail", TextFieldType.Rest));

        var present = InvokeParse(interpreter, "123tail");
        var capture = GetPropertyValue<object>(present, "Code");
        Assert.AreEqual("123", GetPropertyValue<string>(capture, "Digits"));
        Assert.AreEqual("tail", GetPropertyValue<string>(present, "Tail"));

        var absent = InvokeParse(interpreter, "tail");
        Assert.IsNull(absent.GetType().GetProperty("Code")!.GetValue(absent));
        Assert.AreEqual("tail", GetPropertyValue<string>(absent, "Tail"));
    }

    [TestMethod]
    public void Parse_OptionalRepeatWithReferencedSchema_ShouldRestoreCursorOnElementFailure()
    {
        var registry = new SchemaRegistry();
        registry.Register(
            "RequiredItem",
            new TextSchemaNode(
                "RequiredItem",
                [new TextFieldDefinitionNode("Value", TextFieldType.Literal, "X")]));
        registry.Register(
            "Container",
            new TextSchemaNode(
                "Container",
                [
                    new TextFieldDefinitionNode(
                        "Items",
                        TextFieldType.Repeat,
                        "RequiredItem",
                        "\n",
                        TextFieldModifier.Optional),
                    new TextFieldDefinitionNode("Tail", TextFieldType.Rest)
                ]));

        var interpreter = CompileFromRegistry(registry, "Container");

        var present = InvokeParse(interpreter, "X\n tail");
        var items = (Array)GetPropertyValue<object>(present, "Items");
        Assert.HasCount(1, items);
        Assert.AreEqual("X", GetPropertyValue<string>(items.GetValue(0)!, "Value"));
        Assert.AreEqual(" tail", GetPropertyValue<string>(present, "Tail"));

        var absent = InvokeParse(interpreter, "tail");
        Assert.IsNull(absent.GetType().GetProperty("Items")!.GetValue(absent));
        Assert.AreEqual("tail", GetPropertyValue<string>(absent, "Tail"));
    }

    [TestMethod]
    public void Parse_OptionalSwitch_ShouldPreserveCasesAndRestoreCursorOnNoMatch()
    {
        var registry = new SchemaRegistry();
        registry.Register(
            "Branch",
            new TextSchemaNode(
                "Branch",
                [
                    new TextFieldDefinitionNode("Marker", TextFieldType.Literal, "X"),
                    new TextFieldDefinitionNode("Value", TextFieldType.Rest)
                ]));
        registry.Register(
            "Container",
            new TextSchemaNode(
                "Container",
                [
                    new TextFieldDefinitionNode(
                        "Content",
                        [new TextSwitchCaseNode("X", "Branch")],
                        TextFieldModifier.Optional),
                    new TextFieldDefinitionNode("Tail", TextFieldType.Rest)
                ]));

        var interpreter = CompileFromRegistry(registry, "Container");

        var present = InvokeParse(interpreter, "Xpayload");
        var content = (IDictionary<string, object?>)GetPropertyValue<object>(present, "Content");
        Assert.AreEqual("payload", content["Value"]);
        Assert.AreEqual(string.Empty, GetPropertyValue<string>(present, "Tail"));

        var absent = InvokeParse(interpreter, "payload");
        Assert.IsNull(absent.GetType().GetProperty("Content")!.GetValue(absent));
        Assert.AreEqual("payload", GetPropertyValue<string>(absent, "Tail"));
    }

    [TestMethod]
    public void Parse_RepeatWithDelimiterAtCurrentPosition_ShouldReturnEmptyArrayAndConsumeDelimiter()
    {
        var registry = new SchemaRegistry();
        registry.Register(
            "Item",
            new TextSchemaNode("Item", [new TextFieldDefinitionNode("Value", TextFieldType.Token)]));
        registry.Register(
            "Container",
            new TextSchemaNode(
                "Container",
                [
                    new TextFieldDefinitionNode("Items", TextFieldType.Repeat, "Item", "|"),
                    new TextFieldDefinitionNode("Tail", TextFieldType.Rest)
                ]));

        var interpreter = CompileFromRegistry(registry, "Container");
        var result = InvokeParse(interpreter, "|tail");

        Assert.IsEmpty((Array)GetPropertyValue<object>(result, "Items"));
        Assert.AreEqual("tail", GetPropertyValue<string>(result, "Tail"));
    }

    [TestMethod]
    public void Parse_RepeatZeroWidthElement_ShouldRaiseFieldSpecificProgressError()
    {
        var registry = new SchemaRegistry();
        registry.Register("Empty", new TextSchemaNode("Empty", []));
        registry.Register(
            "Container",
            new TextSchemaNode(
                "Container",
                [new TextFieldDefinitionNode("Items", TextFieldType.Repeat, "Empty")]));

        var interpreter = CompileFromRegistry(registry, "Container");
        var wrapper = Assert.ThrowsExactly<TargetInvocationException>(
            () => InvokeParse(interpreter, "x"));
        var exception = GetParseException(wrapper);

        Assert.AreEqual(ParseErrorCode.MaxIterationsExceeded, exception.ErrorCode);
        Assert.AreEqual("Container", exception.SchemaName);
        Assert.AreEqual("Items", exception.FieldName);
        Assert.AreEqual(0, exception.Position);
        Assert.AreEqual("ISE0009", exception.FormattedErrorCode);
        StringAssert.Contains(exception.Details, "made no progress");
    }

    [TestMethod]
    public void Parse_RepeatIterationLimit_ShouldRaiseFieldSpecificLimitError()
    {
        var registry = new SchemaRegistry();
        registry.Register(
            "Character",
            new TextSchemaNode("Character", [new TextFieldDefinitionNode("Value", TextFieldType.Chars, "1")]));
        registry.Register(
            "Container",
            new TextSchemaNode(
                "Container",
                [new TextFieldDefinitionNode("Items", TextFieldType.Repeat, "Character")]));

        var interpreter = CompileFromRegistry(registry, "Container");
        var wrapper = Assert.ThrowsExactly<TargetInvocationException>(
            () => InvokeParse(interpreter, new string('x', 10_001)));
        var exception = GetParseException(wrapper);

        Assert.AreEqual(ParseErrorCode.MaxIterationsExceeded, exception.ErrorCode);
        Assert.AreEqual("Container", exception.SchemaName);
        Assert.AreEqual("Items", exception.FieldName);
        Assert.AreEqual(10_000, exception.Position);
        Assert.AreEqual("ISE0009", exception.FormattedErrorCode);
        StringAssert.Contains(exception.Details, "maximum of 10000 iterations");
    }

    [TestMethod]
    public void Parse_SwitchPatterns_ShouldUseFirstMatchingCaseInSourceOrder()
    {
        var registry = new SchemaRegistry();
        registry.Register(
            "First",
            new TextSchemaNode(
                "First",
                [
                    new TextFieldDefinitionNode("Marker", TextFieldType.Literal, "a"),
                    new TextFieldDefinitionNode("Value", TextFieldType.Rest)
                ]));
        registry.Register(
            "Second",
            new TextSchemaNode(
                "Second",
                [
                    new TextFieldDefinitionNode("Marker", TextFieldType.Literal, "a"),
                    new TextFieldDefinitionNode("Other", TextFieldType.Rest)
                ]));
        registry.Register(
            "Container",
            new TextSchemaNode(
                "Container",
                [new TextFieldDefinitionNode(
                    "Content",
                    [
                        new TextSwitchCaseNode("a", "First"),
                        new TextSwitchCaseNode("a", "Second")
                    ])]));

        var interpreter = CompileFromRegistry(registry, "Container");
        var result = InvokeParse(interpreter, "adata");
        var content = (IDictionary<string, object?>)GetPropertyValue<object>(result, "Content");

        Assert.AreEqual("data", content["Value"]);
        Assert.IsNull(content["Other"]);
    }

    [TestMethod]
    public void Parse_SwitchWithoutDefault_WhenNoPatternMatches_ShouldRaiseAlternativeError()
    {
        var registry = new SchemaRegistry();
        registry.Register(
            "Branch",
            new TextSchemaNode("Branch", [new TextFieldDefinitionNode("Value", TextFieldType.Rest)]));
        registry.Register(
            "Container",
            new TextSchemaNode(
                "Container",
                [new TextFieldDefinitionNode(
                    "Content",
                    [new TextSwitchCaseNode("x", "Branch")])]));

        var interpreter = CompileFromRegistry(registry, "Container");
        var wrapper = Assert.ThrowsExactly<TargetInvocationException>(
            () => InvokeParse(interpreter, "y"));
        var exception = GetParseException(wrapper);

        Assert.AreEqual(ParseErrorCode.NoAlternativeMatched, exception.ErrorCode);
        Assert.AreEqual("Container", exception.SchemaName);
        Assert.AreEqual("Content", exception.FieldName);
        Assert.AreEqual(0, exception.Position);
        Assert.AreEqual("ISE0012", exception.FormattedErrorCode);
    }

    private static ParseException GetParseException(TargetInvocationException wrapper)
    {
        Assert.IsNotNull(wrapper.InnerException);
        Assert.IsInstanceOfType<ParseException>(wrapper.InnerException);
        return (ParseException)wrapper.InnerException!;
    }
}
