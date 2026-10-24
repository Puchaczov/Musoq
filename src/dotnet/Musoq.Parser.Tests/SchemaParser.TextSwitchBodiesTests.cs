using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class TextSwitchBodiesParserTests : SchemaParserTestsBase
{
    [TestMethod]
    [DataRow("chars[2]", TextFieldType.Chars)]
    [DataRow("pattern '(?<Name>[a-z]+)' capture (Name)", TextFieldType.Pattern)]
    [DataRow("repeat chars[1] until end", TextFieldType.Repeat)]
    [DataRow("optional literal 'x'", TextFieldType.Literal)]
    [DataRow("switch { _ => rest }", TextFieldType.Switch)]
    public void SwitchArm_ShouldKeepTheCompleteTypedBody(string type, TextFieldType expected)
    {
        var schema = ParseTextSchema($"text Rec {{ Content: switch {{ _ => {type} }} }}");
        var arm = schema.Fields[0].SwitchCases[0];
        Assert.AreEqual(expected, arm.Body.FieldType);
        Assert.ThrowsExactly<InvalidOperationException>(() => _ = arm.TypeName);
    }

    [TestMethod]
    public void NamedSchemaConstructor_ShouldPreserveItsSchemaNameAndTypedBody()
    {
        var arm = new TextSwitchCaseNode("x", "Record");
        Assert.AreEqual("Record", arm.TypeName);
        Assert.AreEqual(TextFieldType.SchemaReference, arm.Body.FieldType);
        Assert.AreEqual("pattern 'x' => Record", arm.ToString());
    }
}
