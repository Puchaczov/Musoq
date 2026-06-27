using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Build;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class TextInterpretationPlanBuilderTests
{
    private static BoundTextInterpretationPlan BuildPlan(TextSchemaNode schema)
    {
        var registry = new SchemaRegistry();
        registry.Register(schema.Name, schema);

        return new InterpreterCodeGenerator(registry).BuildTextPlan(schema);
    }

    [TestMethod]
    public void BuildTextPlan_WhenSchemaHasUntilField_ShouldEmitStringProperty()
    {
        var field = new TextFieldDefinitionNode("Name", TextFieldType.Until, ",");
        var schema = new TextSchemaNode("Record", [field]);

        var plan = BuildPlan(schema);

        Assert.AreEqual("string?", plan.Fields.Single().PropertyClrType);
    }

    [TestMethod]
    public void BuildTextPlan_WhenFieldIsRepeat_ShouldEmitArrayProperty()
    {
        var field = new TextFieldDefinitionNode("Lines", TextFieldType.Repeat, "Line", "\n\n");
        var schema = new TextSchemaNode("Document", [field]);

        var plan = BuildPlan(schema);

        Assert.AreEqual("Line[]?", plan.Fields.Single().PropertyClrType);
    }

    [TestMethod]
    public void BuildTextPlan_WhenFieldIsSwitch_ShouldEmitObjectProperty()
    {
        var field = new TextFieldDefinitionNode("Body", [new TextSwitchCaseNode("\\d+", "Numeric")]);
        var schema = new TextSchemaNode("Message", [field]);

        var plan = BuildPlan(schema);

        Assert.AreEqual("object?", plan.Fields.Single().PropertyClrType);
    }

    [TestMethod]
    public void BuildTextPlan_WhenFieldIsPatternWithCaptureGroups_ShouldEmitCaptureResultProperty()
    {
        var field = new TextFieldDefinitionNode(
            "Stamp",
            TextFieldType.Pattern,
            "(?<Year>\\d{4})",
            captureGroups: ["Year"]);
        var schema = new TextSchemaNode("Log", [field]);

        var plan = BuildPlan(schema);

        var bound = plan.Fields.Single();
        Assert.AreEqual("CaptureResult_Stamp?", bound.PropertyClrType);
        Assert.IsTrue(bound.IsCaptureResult);
        CollectionAssert.AreEqual(new[] { "Year" }, bound.CaptureGroups.ToArray());
    }

    [TestMethod]
    public void BuildTextPlan_WhenFieldIsDiscard_ShouldNotEmitProperty()
    {
        var field = new TextFieldDefinitionNode("_", TextFieldType.Whitespace, "+");
        var schema = new TextSchemaNode("Spaced", [field]);

        var plan = BuildPlan(schema);

        Assert.IsFalse(plan.Fields.Single().EmitsProperty);
    }

    [TestMethod]
    public void BuildTextPlan_WhenSchemaExtendsParent_ShouldCaptureExtends()
    {
        var field = new TextFieldDefinitionNode("Own", TextFieldType.Rest);
        var schema = new TextSchemaNode("Derived", [field], "Base");

        var plan = BuildPlan(schema);

        Assert.AreEqual("Base", plan.Extends);
    }

    [TestMethod]
    public void BuildTextPlan_WhenSchemaIsNull_ShouldThrowArgumentNullException()
    {
        var generator = new InterpreterCodeGenerator(new SchemaRegistry());

        Assert.ThrowsExactly<System.ArgumentNullException>(() => generator.BuildTextPlan(null!));
    }
}
