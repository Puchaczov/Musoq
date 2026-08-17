using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class RawStringLiteralSchemaTests : SchemaParserTestsBase
{
    [TestMethod]
    public void BinarySchema_RawLiteralInRepeatUntilCondition_ShouldPreserveBackslashes()
    {
        const string schema = @"binary Stream {
            Names: string[32] ascii repeat until Names = R'C:\new\test'
        }";

        var result = ParseBinarySchema(schema);
        var field = (FieldDefinitionNode)result.Fields[0];
        var repeat = (RepeatUntilTypeNode)field.TypeAnnotation;
        var condition = (EqualityNode)repeat.Condition!;

        Assert.AreEqual(@"C:\new\test", ((ConstantValueNode)condition.Right).ObjValue);
    }
}
