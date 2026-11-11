using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Tests;

public partial class InterpreterCodeGenTests
{
    [TestMethod]
    public void GenerateAll_OptionalDiscardField_ShouldGenerateTryCatchWithPositionRestore()
    {
        // Arrange
        var registry = new SchemaRegistry();

        var fields = new[]
        {
            new TextFieldDefinitionNode("Required", TextFieldType.Until, ":"),
            new TextFieldDefinitionNode("_", TextFieldType.Literal, "\t", null, TextFieldModifier.Optional),
            new TextFieldDefinitionNode("Extra", TextFieldType.Rest),
        };
        var schema = new TextSchemaNode("LogEntry", fields);
        registry.Register("LogEntry", schema);

        var generator = new InterpreterCodeGenerator(registry);

        // Act
        var code = generator.GenerateAll();

        // Assert

        Assert.Contains(
            "ParsePosition = __musoqOptionalPosition",
            code,
            $"Expected position save for optional discard but got:\n{code}"
        );
        Assert.Contains("try", code, $"Expected 'try' block for optional discard but got:\n{code}");
        Assert.Contains(
            "base.ExpectLiteralStatus(ref __musoqTextInput, \"\\t\", fieldName: \"_\")",
            code,
            $"Expected 'ExpectLiteral' call but got:\n{code}"
        );
        Assert.Contains("goto __musoqOptionalFailed", code);
        Assert.AreEqual(code, generator.GenerateAll(), "Optional fields must generate deterministic source.");
    }
}
