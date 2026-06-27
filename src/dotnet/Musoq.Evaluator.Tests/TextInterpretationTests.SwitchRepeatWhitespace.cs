using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Build;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     End-to-end (Parse) coverage for text schema constructs that previously had
///     code-generation-level tests only: switch, repeat, and whitespace.
/// </summary>
public partial class TextInterpretationTests
{
    [TestMethod]
    public void Parse_SwitchField_WhenPatternMatches_ShouldDispatchToMatchingSchema()
    {
        var registry = new SchemaRegistry();
        registry.Register("SectionHeader", new TextSchemaNode("SectionHeader",
        [
            new TextFieldDefinitionNode("_", TextFieldType.Literal, "["),
            new TextFieldDefinitionNode("Name", TextFieldType.Until, "]")
        ]));
        registry.Register("KeyValue", new TextSchemaNode("KeyValue",
        [
            new TextFieldDefinitionNode("Key", TextFieldType.Until, "=")
        ]));
        registry.Register("ConfigLine", new TextSchemaNode("ConfigLine",
        [
            new TextFieldDefinitionNode("Content",
            [
                new TextSwitchCaseNode(@"\[", "SectionHeader"),
                new TextSwitchCaseNode(null, "KeyValue")
            ])
        ]));

        var interpreter = CompileFromRegistry(registry, "ConfigLine");

        var result = InvokeParse(interpreter, "[server]");

        var content = (IDictionary<string, object?>)GetPropertyValue<object>(result, "Content");
        Assert.AreEqual("server", content["Name"]);
    }

    [TestMethod]
    public void Parse_SwitchField_WhenNoPatternMatches_ShouldDispatchToDefaultSchema()
    {
        var registry = new SchemaRegistry();
        registry.Register("SectionHeader", new TextSchemaNode("SectionHeader",
        [
            new TextFieldDefinitionNode("_", TextFieldType.Literal, "["),
            new TextFieldDefinitionNode("Name", TextFieldType.Until, "]")
        ]));
        registry.Register("KeyValue", new TextSchemaNode("KeyValue",
        [
            new TextFieldDefinitionNode("Key", TextFieldType.Until, "=")
        ]));
        registry.Register("ConfigLine", new TextSchemaNode("ConfigLine",
        [
            new TextFieldDefinitionNode("Content",
            [
                new TextSwitchCaseNode(@"\[", "SectionHeader"),
                new TextSwitchCaseNode(null, "KeyValue")
            ])
        ]));

        var interpreter = CompileFromRegistry(registry, "ConfigLine");

        var result = InvokeParse(interpreter, "host=localhost");

        var content = (IDictionary<string, object?>)GetPropertyValue<object>(result, "Content");
        Assert.AreEqual("host", content["Key"]);
    }

    [TestMethod]
    public void Parse_RepeatField_UntilEnd_ShouldCaptureEveryElement()
    {
        var registry = new SchemaRegistry();
        registry.Register("LineItem", new TextSchemaNode("LineItem",
        [
            new TextFieldDefinitionNode("Content", TextFieldType.Until, "\n")
        ]));
        registry.Register("Document", new TextSchemaNode("Document",
        [
            new TextFieldDefinitionNode("Lines", TextFieldType.Repeat, "LineItem")
        ]));

        var interpreter = CompileFromRegistry(registry, "Document");

        var result = InvokeParse(interpreter, "alpha\nbeta\ngamma\n");

        var lines = (Array)GetPropertyValue<object>(result, "Lines");
        Assert.AreEqual(3, lines.Length);
        Assert.AreEqual("alpha", GetPropertyValue<string>(lines.GetValue(0)!, "Content"));
        Assert.AreEqual("beta", GetPropertyValue<string>(lines.GetValue(1)!, "Content"));
        Assert.AreEqual("gamma", GetPropertyValue<string>(lines.GetValue(2)!, "Content"));
    }

    [TestMethod]
    public void Parse_RepeatField_UntilDelimiter_ShouldStopAtTerminator()
    {
        var registry = new SchemaRegistry();
        registry.Register("LineItem", new TextSchemaNode("LineItem",
        [
            new TextFieldDefinitionNode("Content", TextFieldType.Until, "\n")
        ]));
        registry.Register("Block", new TextSchemaNode("Block",
        [
            new TextFieldDefinitionNode("Lines", TextFieldType.Repeat, "LineItem", "END")
        ]));

        var interpreter = CompileFromRegistry(registry, "Block");

        var result = InvokeParse(interpreter, "alpha\nbeta\nEND");

        var lines = (Array)GetPropertyValue<object>(result, "Lines");
        Assert.AreEqual(2, lines.Length);
        Assert.AreEqual("alpha", GetPropertyValue<string>(lines.GetValue(0)!, "Content"));
        Assert.AreEqual("beta", GetPropertyValue<string>(lines.GetValue(1)!, "Content"));
    }

    [TestMethod]
    public void Parse_RequiredWhitespace_ShouldConsumeLeadingSpaces()
    {
        var interpreter = CreateAndCompileInterpreter("Padded",
            CreateTextField("_", TextFieldType.Whitespace, "+"),
            CreateTextField("Value", TextFieldType.Rest));

        var result = InvokeParse(interpreter, "   payload");

        Assert.AreEqual("payload", GetPropertyValue<string>(result, "Value"));
    }

    [TestMethod]
    public void Parse_OptionalWhitespace_WhenAbsent_ShouldConsumeNothing()
    {
        var interpreter = CreateAndCompileInterpreter("MaybePadded",
            CreateTextField("_", TextFieldType.Whitespace, "*"),
            CreateTextField("Value", TextFieldType.Rest));

        var result = InvokeParse(interpreter, "payload");

        Assert.AreEqual("payload", GetPropertyValue<string>(result, "Value"));
    }

    private static object CompileFromRegistry(SchemaRegistry registry, string schemaName)
    {
        var generator = new InterpreterCodeGenerator(registry);
        var code = generator.GenerateAll();

        var compilationUnit = new InterpreterCompilationUnit(
            $"TestAssembly_{Guid.NewGuid():N}",
            code);

        if (!compilationUnit.Compile())
        {
            var errors = string.Join(Environment.NewLine, compilationUnit.GetErrorMessages());
            Assert.Fail($"Compilation failed: {errors}\n\nGenerated code:\n{code}");
        }

        var interpreterType = compilationUnit.GetInterpreterType(schemaName);
        Assert.IsNotNull(interpreterType, $"Interpreter type for '{schemaName}' not found");

        return Activator.CreateInstance(interpreterType)!;
    }
}
