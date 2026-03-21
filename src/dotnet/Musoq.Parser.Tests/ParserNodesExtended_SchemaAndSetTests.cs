using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Parser.Tests;

/// <summary>
///     Tests for SchemaFromNode, SetOperator nodes, SingleSetNode, and GroupSelectNode.
/// </summary>
[TestClass]
public class ParserNodesExtended_SchemaAndSetTests
{
    #region SchemaFromNode Tests

    [TestMethod]
    public void SchemaFromNode_Constructor_SetsProperties()
    {
        var parameters = new ArgsListNode([]);
        var node = new SchemaFromNode("mySchema", "myMethod", parameters, "myAlias", typeof(string), 1);

        Assert.AreEqual("mySchema", node.Schema);
        Assert.AreEqual("myMethod", node.Method);
        Assert.AreSame(parameters, node.Parameters);
        Assert.AreEqual("myAlias", node.Alias);
        Assert.AreEqual(typeof(string), node.ReturnType);
        Assert.AreEqual(1, node.QueryId);
    }

    [TestMethod]
    public void SchemaFromNode_ToString_WithAlias_Works()
    {
        var parameters = new ArgsListNode([]);
        var node = new SchemaFromNode("schema", "method", parameters, "alias", typeof(object), 0);

        var str = node.ToString();
        Assert.IsNotNull(str);
        Assert.Contains("alias", str);
    }

    [TestMethod]
    public void SchemaFromNode_ToString_WithoutAlias_Works()
    {
        var parameters = new ArgsListNode([]);
        var node = new SchemaFromNode("schema", "method", parameters, "", typeof(object), 0);

        var str = node.ToString();
        Assert.IsNotNull(str);
        Assert.DoesNotContain(" ", str);
    }

    [TestMethod]
    public void SchemaFromNode_Equals_SameId_ReturnsTrue()
    {
        var parameters = new ArgsListNode([]);
        var node1 = new SchemaFromNode("schema", "method", parameters, "alias", typeof(object), 0);
        var node2 = new SchemaFromNode("schema", "method", parameters, "alias", typeof(object), 0);

        Assert.IsTrue(node1.Equals(node2));
    }

    [TestMethod]
    public void SchemaFromNode_Equals_DifferentId_ReturnsFalse()
    {
        var parameters = new ArgsListNode([]);
        var node1 = new SchemaFromNode("schema1", "method", parameters, "alias", typeof(object), 0);
        var node2 = new SchemaFromNode("schema2", "method", parameters, "alias", typeof(object), 0);

        Assert.IsFalse(node1.Equals(node2));
    }

    [TestMethod]
    public void SchemaFromNode_Equals_NonSchemaFromNode_ReturnsFalse()
    {
        var parameters = new ArgsListNode([]);
        var node = new SchemaFromNode("schema", "method", parameters, "alias", typeof(object), 0);

        Assert.IsFalse(node.Equals("not a node"));
    }

    [TestMethod]
    public void SchemaFromNode_GetHashCode_ReturnsIdHashCode()
    {
        var parameters = new ArgsListNode([]);
        var node = new SchemaFromNode("schema", "method", parameters, "alias", typeof(object), 0);

        Assert.AreEqual(node.Id.GetHashCode(), node.GetHashCode());
    }

    #endregion

    #region GroupSelectNode Extended Tests

    [TestMethod]
    public void GroupSelectNode_ReturnType_IsNull()
    {
        var fields = Array.Empty<FieldNode>();
        var node = new GroupSelectNode(fields);

        Assert.IsNull(node.ReturnType);
    }

    #endregion

    #region GroupSelectNode Tests

    [TestMethod]
    public void GroupSelectNode_Constructor_SetsFields()
    {
        var fields = Array.Empty<FieldNode>();
        var node = new GroupSelectNode(fields);

        Assert.AreSame(fields, node.Fields);
    }

    [TestMethod]
    public void GroupSelectNode_ToString_Works()
    {
        var fields = Array.Empty<FieldNode>();
        var node = new GroupSelectNode(fields);

        var str = node.ToString();
        Assert.IsNotNull(str);
    }

    #endregion

    #region SingleSetNode Tests

    [TestMethod]
    public void SingleSetNode_Constructor_SetsProperties()
    {
        var select = new SelectNode([]);
        var dual = new SchemaFromNode("system", "dual", new ArgsListNode([]), "d", typeof(object), 0);
        var query = new QueryNode(select, dual, null, null, null, null, null);

        var node = new SingleSetNode(query);

        Assert.AreSame(query, node.Query);
    }

    [TestMethod]
    public void SingleSetNode_ToString_Works()
    {
        var select = new SelectNode([]);
        var dual = new SchemaFromNode("system", "dual", new ArgsListNode([]), "d", typeof(object), 0);
        var query = new QueryNode(select, dual, null, null, null, null, null);

        var node = new SingleSetNode(query);

        var str = node.ToString();
        Assert.IsNotNull(str);
    }

    #endregion

    #region SingleSetNode Extended Tests

    [TestMethod]
    public void SingleSetNode_ReturnType_IsVoid()
    {
        var select = new SelectNode([]);
        var dual = new SchemaFromNode("system", "dual", new ArgsListNode([]), "d", typeof(object), 0);
        var query = new QueryNode(select, dual, null, null, null, null, null);

        var node = new SingleSetNode(query);

        Assert.AreEqual(typeof(void), node.ReturnType);
    }

    [TestMethod]
    public void SingleSetNode_Id_ContainsQueryId()
    {
        var select = new SelectNode([]);
        var dual = new SchemaFromNode("system", "dual", new ArgsListNode([]), "d", typeof(object), 0);
        var query = new QueryNode(select, dual, null, null, null, null, null);

        var node = new SingleSetNode(query);

        Assert.StartsWith("SingleSetNode", node.Id);
    }

    #endregion

    #region SetOperator Node Tests

    [TestMethod]
    public void ExceptNode_Constructor_SetsProperties()
    {
        var left = new IntegerNode("1");
        var right = new IntegerNode("2");
        var keys = new[] { "col1", "col2" };

        var node = new ExceptNode("resultTable", keys, left, right, false, true);

        Assert.AreEqual("resultTable", node.ResultTableName);
        Assert.AreSame(keys, node.Keys);
        Assert.AreSame(left, node.Left);
        Assert.AreSame(right, node.Right);
        Assert.IsFalse(node.IsNested);
        Assert.IsTrue(node.IsTheLastOne);
    }

    [TestMethod]
    public void ExceptNode_ToString_IncludesKeys()
    {
        var left = new IntegerNode("1");
        var right = new IntegerNode("2");
        var keys = new[] { "id", "name" };

        var node = new ExceptNode("result", keys, left, right, false, false);

        var str = node.ToString();
        Assert.Contains("except", str);
        Assert.Contains("id", str);
        Assert.Contains("name", str);
    }

    [TestMethod]
    public void ExceptNode_ReturnType_IsVoid()
    {
        var left = new IntegerNode("1");
        var right = new IntegerNode("2");

        var node = new ExceptNode("result", [], left, right, false, false);

        Assert.AreEqual(typeof(void), node.ReturnType);
    }

    [TestMethod]
    public void IntersectNode_Constructor_SetsProperties()
    {
        var left = new IntegerNode("1");
        var right = new IntegerNode("2");
        var keys = new[] { "col1" };

        var node = new IntersectNode("resultTable", keys, left, right, true, false);

        Assert.AreEqual("resultTable", node.ResultTableName);
        Assert.AreSame(keys, node.Keys);
        Assert.IsTrue(node.IsNested);
        Assert.IsFalse(node.IsTheLastOne);
    }

    [TestMethod]
    public void IntersectNode_ToString_IncludesKeys()
    {
        var left = new IntegerNode("1");
        var right = new IntegerNode("2");
        var keys = new[] { "key" };

        var node = new IntersectNode("result", keys, left, right, false, false);

        var str = node.ToString();
        Assert.Contains("intersect", str);
        Assert.Contains("key", str);
    }

    [TestMethod]
    public void IntersectNode_ReturnType_IsVoid()
    {
        var left = new IntegerNode("1");
        var right = new IntegerNode("2");

        var node = new IntersectNode("result", [], left, right, false, false);

        Assert.AreEqual(typeof(void), node.ReturnType);
    }

    [TestMethod]
    public void UnionNode_Constructor_SetsProperties()
    {
        var left = new IntegerNode("1");
        var right = new IntegerNode("2");
        var keys = new[] { "a", "b" };

        var node = new UnionNode("resultTable", keys, left, right, false, true);

        Assert.AreEqual("resultTable", node.ResultTableName);
        Assert.IsFalse(node.IsNested);
        Assert.IsTrue(node.IsTheLastOne);
    }

    [TestMethod]
    public void UnionNode_ToString_IncludesKeys()
    {
        var left = new IntegerNode("1");
        var right = new IntegerNode("2");
        var keys = new[] { "x" };

        var node = new UnionNode("result", keys, left, right, false, false);

        var str = node.ToString();
        Assert.Contains("union", str);
        Assert.Contains("x", str);
    }

    [TestMethod]
    public void UnionNode_ReturnType_IsVoid()
    {
        var left = new IntegerNode("1");
        var right = new IntegerNode("2");

        var node = new UnionNode("result", [], left, right, false, false);

        Assert.AreEqual(typeof(void), node.ReturnType);
    }

    [TestMethod]
    public void UnionAllNode_Constructor_SetsProperties()
    {
        var left = new IntegerNode("1");
        var right = new IntegerNode("2");

        var node = new UnionAllNode("resultTable", [], left, right, false, false);

        Assert.AreEqual("resultTable", node.ResultTableName);
    }

    [TestMethod]
    public void UnionAllNode_ToString_IncludesUnionAll()
    {
        var left = new IntegerNode("1");
        var right = new IntegerNode("2");

        var node = new UnionAllNode("result", [], left, right, false, false);

        var str = node.ToString();
        Assert.Contains("union all", str);
    }

    #endregion
}
