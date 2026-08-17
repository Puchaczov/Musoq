using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class BinaryInterpretationPlanBuilderTests
{
    private static BoundBinaryInterpretationPlan BuildPlan(BinarySchemaNode schema, params BinarySchemaNode[] dependencies)
    {
        var registry = new SchemaRegistry();
        foreach (var dependency in dependencies)
            registry.Register(dependency.Name, dependency);
        registry.Register(schema.Name, schema);

        return new InterpreterCodeGenerator(registry).BuildBinaryPlan(schema);
    }

    private static FieldDefinitionNode PrimitiveField(string name, PrimitiveTypeName typeName = PrimitiveTypeName.Int)
    {
        return new FieldDefinitionNode(name, new PrimitiveTypeNode(typeName, Endianness.LittleEndian));
    }

    [TestMethod]
    public void BuildBinaryPlan_WhenSchemaHasPrimitiveField_ShouldEmitTypedProperty()
    {
        var schema = new BinarySchemaNode("Header", [PrimitiveField("Length")]);

        var plan = BuildPlan(schema);

        var field = plan.Fields.Single();
        Assert.AreEqual("int", field.PropertyClrType);
    }

    [TestMethod]
    public void BuildBinaryPlan_WhenFieldIsConditional_ShouldMarkValueTypePropertyNullable()
    {
        var flag = PrimitiveField("Flag", PrimitiveTypeName.Byte);
        var condition = new DiffNode(new IdentifierNode("Flag"), new IntegerNode(0));
        var payload = new FieldDefinitionNode(
            "Payload",
            new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian),
            null,
            null,
            condition);
        var schema = new BinarySchemaNode("Message", [flag, payload]);

        var plan = BuildPlan(schema);

        var payloadField = plan.Fields.Single(f => f.Name == "Payload");
        Assert.AreEqual("int?", payloadField.PropertyClrType);
    }

    [TestMethod]
    public void BuildBinaryPlan_WhenFieldIsDiscard_ShouldNotEmitProperty()
    {
        var schema = new BinarySchemaNode("Padding", [PrimitiveField("_")]);

        var plan = BuildPlan(schema);

        Assert.IsFalse(plan.Fields.Single().EmitsProperty);
    }

    [TestMethod]
    public void BuildBinaryPlan_WhenFieldIsAlignment_ShouldNotEmitProperty()
    {
        var alignment = new FieldDefinitionNode("Pad", new AlignmentNode(32));
        var schema = new BinarySchemaNode("Aligned", [alignment]);

        var plan = BuildPlan(schema);

        Assert.IsTrue(plan.Fields.Single().IsAlignment);
    }

    [TestMethod]
    public void BuildBinaryPlan_WhenSchemaExtendsParent_ShouldFlattenInheritedFieldsInOrder()
    {
        var parent = new BinarySchemaNode("Base", [PrimitiveField("Inherited")]);
        var child = new BinarySchemaNode("Derived", [PrimitiveField("Own")], "Base");

        var plan = BuildPlan(child, parent);

        CollectionAssert.AreEqual(
            new[] { "Inherited", "Own" },
            plan.Fields.Select(f => f.Name).ToArray());
    }

    [TestMethod]
    public void BuildBinaryPlan_WhenSchemaIsGeneric_ShouldCaptureTypeParameters()
    {
        var schema = new BinarySchemaNode("Wrapper", [PrimitiveField("Length")], null, ["T"]);

        var plan = BuildPlan(schema);

        CollectionAssert.AreEqual(new[] { "T" }, plan.TypeParameters.ToArray());
    }

    [TestMethod]
    public void BuildBinaryPlan_WhenSchemaIsNull_ShouldThrowArgumentNullException()
    {
        var generator = new InterpreterCodeGenerator(new SchemaRegistry());

        Assert.ThrowsExactly<System.ArgumentNullException>(() => generator.BuildBinaryPlan(null!));
    }

    [TestMethod]
    public void BuildBinaryPlan_WhenFieldIsSwitch_ShouldBindSelectorAndBranches()
    {
        var plan = BuildPlan(SwitchSchema());

        var switchField = plan.Fields.Single(f => f.Name == "Payload");
        Assert.IsNotNull(switchField.Switch);
        Assert.AreEqual("Type", switchField.Switch.Selector);
        CollectionAssert.AreEqual(
            new[] { "Login", "Raw" },
            switchField.Switch.Branches.Select(b => b.BranchAlias).ToArray());
    }

    [TestMethod]
    public void BuildBinaryPlan_WhenSwitchHasDefault_ShouldExposeDefaultBranch()
    {
        var plan = BuildPlan(SwitchSchema());

        var switchField = plan.Fields.Single(f => f.Name == "Payload");
        Assert.IsNotNull(switchField.Switch!.DefaultBranch);
        Assert.AreEqual("Raw", switchField.Switch.DefaultBranch.BranchAlias);
        Assert.IsTrue(switchField.Switch.DefaultBranch.IsDefault);
    }

    [TestMethod]
    public void BuildBinaryPlan_WhenFieldIsSwitch_ShouldEmitSwitchClassProperty()
    {
        var plan = BuildPlan(SwitchSchema());

        var switchField = plan.Fields.Single(f => f.Name == "Payload");
        Assert.AreEqual("Switch_Payload", switchField.PropertyClrType);
    }

    [TestMethod]
    public void BuildBinaryPlan_WhenFieldIsNotSwitch_ShouldHaveNullSwitchBinding()
    {
        var schema = new BinarySchemaNode("Header", [PrimitiveField("Length")]);

        var plan = BuildPlan(schema);

        Assert.IsNull(plan.Fields.Single().Switch);
    }

    private static BinarySchemaNode SwitchSchema()
    {
        var login = new BinarySchemaNode("LoginPayload", [PrimitiveField("UserId")]);
        var switchType = new BinarySwitchTypeNode(
            "Type",
            [
                new BinarySwitchCaseNode(
                    new IntegerNode("1", ""),
                    "Login",
                    new SchemaReferenceTypeNode("LoginPayload")),
                new BinarySwitchCaseNode(
                    null,
                    "Raw",
                    new ByteArrayTypeNode(new IntegerNode("4", "")))
            ]);
        var type = PrimitiveField("Type", PrimitiveTypeName.Byte);
        var payload = new FieldDefinitionNode("Payload", switchType);
        return new BinarySchemaNode("Packet", [type, payload]);
    }
}
