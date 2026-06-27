using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class BinaryInterpretationSwitchTests : BinaryInterpretationTestBase
{
    [TestMethod]
    public void Interpret_Switch_MatchingCase_ShouldParseSelectedBranch()
    {
        var interpreter = CompilePacketInterpreter();

        var data = new byte[] { 0x01, 0x04, 0x2A, 0x00, 0x00, 0x00 };

        var result = InvokeInterpret(interpreter, data);
        var payload = GetPropertyValue<object>(result, "Payload")!;

        Assert.AreEqual("Login", GetPropertyValue<string>(payload, "Case"));
        Assert.IsNull(GetPropertyValue<object>(payload, "Raw"));
        Assert.IsNotNull(GetPropertyValue<object>(payload, "Login"));
    }

    [TestMethod]
    public void Interpret_Switch_MatchingCase_ShouldExposeNestedBranchFields()
    {
        var interpreter = CompilePacketInterpreter();

        var data = new byte[] { 0x01, 0x04, 0x2A, 0x00, 0x00, 0x00 };

        var result = InvokeInterpret(interpreter, data);
        var payload = GetPropertyValue<object>(result, "Payload")!;
        var login = GetPropertyValue<object>(payload, "Login")!;

        var userId = GetPropertyValue<int>(login, "UserId");
        Assert.AreEqual(42, userId);
    }

    [TestMethod]
    public void Interpret_Switch_DefaultCase_ShouldParseRawBranch()
    {
        var interpreter = CompilePacketInterpreter();

        var data = new byte[] { 0x09, 0x03, 0xAA, 0xBB, 0xCC };

        var result = InvokeInterpret(interpreter, data);
        var payload = GetPropertyValue<object>(result, "Payload")!;

        Assert.AreEqual("Raw", GetPropertyValue<string>(payload, "Case"));
        Assert.IsNull(GetPropertyValue<object>(payload, "Login"));
        CollectionAssert.AreEqual(new byte[] { 0xAA, 0xBB, 0xCC }, GetPropertyValue<byte[]>(payload, "Raw")!);
    }

    private static object CompilePacketInterpreter()
    {
        var registry = new SchemaRegistry();

        var loginUserIdField = CreatePrimitiveField("UserId", PrimitiveTypeName.Int, Endianness.LittleEndian);
        registry.Register("LoginPayload", new BinarySchemaNode("LoginPayload", [loginUserIdField]));

        var typeField = CreatePrimitiveField("Type", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var lengthField = CreatePrimitiveField("Length", PrimitiveTypeName.Byte, Endianness.NotApplicable);

        var loginCase = new BinarySwitchCaseNode(
            new IntegerNode("1", string.Empty),
            "Login",
            new SchemaReferenceTypeNode("LoginPayload"));

        var rawCase = new BinarySwitchCaseNode(
            null,
            "Raw",
            new ByteArrayTypeNode(new IdentifierNode("Length")));

        var switchType = new BinarySwitchTypeNode("Type", [loginCase, rawCase]);
        var payloadField = new FieldDefinitionNode("Payload", switchType);

        registry.Register("Packet", new BinarySchemaNode("Packet", [typeField, lengthField, payloadField]));

        return CompileInterpreter(registry, "Packet");
    }
}
