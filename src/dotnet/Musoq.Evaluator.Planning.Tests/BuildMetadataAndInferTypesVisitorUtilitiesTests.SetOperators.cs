using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Tests;

public partial class BuildMetadataAndInferTypesVisitorUtilitiesTests
{
    [TestMethod]
    public void CreateSetOperatorPositionIndexes_EmptyKeys_ReturnsAllIndexes()
    {
        var queryNode = CreateSetOperatorQueryNode(
            new FieldNode(new IntegerNode("1", "s"), 0, "Field1"),
            new FieldNode(new StringNode("text"), 1, "Field2"),
            new FieldNode(new DecimalNode("3.5"), 2, "Field3"));

        var result = BuildMetadataAndInferTypesVisitorUtilities.CreateSetOperatorPositionIndexes(queryNode, []);

        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, result);
    }

    [TestMethod]
    public void CreateSetOperatorPositionTypes_EmptyKeys_ReturnsAllTypes()
    {
        var queryNode = CreateSetOperatorQueryNode(
            new FieldNode(new IntegerNode("1", "s"), 0, "Field1"),
            new FieldNode(new StringNode("text"), 1, "Field2"),
            new FieldNode(new DecimalNode("3.5"), 2, "Field3"));

        var result = BuildMetadataAndInferTypesVisitorUtilities.CreateSetOperatorPositionTypes(queryNode, []);

        CollectionAssert.AreEqual(
            new[] { typeof(short), typeof(string), typeof(decimal) },
            result);
    }

    private static QueryNode CreateSetOperatorQueryNode(params FieldNode[] fields)
    {
        var selectNode = new SelectNode(fields);
        var fromNode = new SchemaFromNode("test", "table", new ArgsListNode([]), "t1", typeof(object), 1);
        return new QueryNode(selectNode, fromNode, null, null, null, null, null);
    }
}
