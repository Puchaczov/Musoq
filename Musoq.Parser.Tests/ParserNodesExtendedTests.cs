using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

/// <summary>
///     Tests for various Parser node classes to improve branch coverage.
/// </summary>
[TestClass]
public class ParserNodesExtendedTests
{
    #region GroupSelectNode Extended Tests

    [TestMethod]
    public void GroupSelectNode_ReturnType_IsNull()
    {
        var fields = Array.Empty<FieldNode>();
        var node = new GroupSelectNode(fields);


        Assert.IsNull(node.ReturnType);
    }

    #endregion

    #region Additional Token Tests

    [TestMethod]
    public void ColumnKeywordToken_Constructor_SetsProperties()
    {
        var token = new ColumnKeywordToken(new TextSpan(0, 6));

        Assert.AreEqual("column", token.Value);
        Assert.AreEqual(TokenType.ColumnKeyword, token.TokenType);
    }

    #endregion

    #region AccessMethodNode Tests

    [TestMethod]
    public void AccessMethodNode_Constructor_SetsProperties()
    {
        var token = new FunctionToken("TestFunc", new TextSpan(0, 8));
        var args = new ArgsListNode([]);
        var extraArgs = new ArgsListNode([]);

        var node = new AccessMethodNode(token, args, extraArgs, false, null, "alias");

        Assert.AreEqual("TestFunc", node.Name);
        Assert.AreEqual("alias", node.Alias);
        Assert.AreEqual(0, node.ArgsCount);
        Assert.IsFalse(node.CanSkipInjectSource);
        Assert.IsNull(node.Method);
    }

    [TestMethod]
    public void AccessMethodNode_CanSkipInjectSource_True()
    {
        var token = new FunctionToken("TestFunc", new TextSpan(0, 8));
        var args = new ArgsListNode([]);
        var extraArgs = new ArgsListNode([]);

        var node = new AccessMethodNode(token, args, extraArgs, true);

        Assert.IsTrue(node.CanSkipInjectSource);
    }

    [TestMethod]
    public void AccessMethodNode_ReturnType_NoMethod_ReturnsVoid()
    {
        var token = new FunctionToken("TestFunc", new TextSpan(0, 8));
        var args = new ArgsListNode([]);
        var extraArgs = new ArgsListNode([]);

        var node = new AccessMethodNode(token, args, extraArgs, false);

        Assert.AreEqual(typeof(void), node.ReturnType);
    }

    [TestMethod]
    public void AccessMethodNode_ToString_WithAlias()
    {
        var token = new FunctionToken("Func", new TextSpan(0, 4));
        var args = new ArgsListNode([]);
        var extraArgs = new ArgsListNode([]);

        var node = new AccessMethodNode(token, args, extraArgs, false, null, "myAlias");

        var str = node.ToString();
        Assert.IsTrue(str.Contains("myAlias"));
        Assert.IsTrue(str.Contains("Func"));
    }

    [TestMethod]
    public void AccessMethodNode_ToString_WithoutAlias()
    {
        var token = new FunctionToken("NoAliasFunc", new TextSpan(0, 11));
        var args = new ArgsListNode([]);
        var extraArgs = new ArgsListNode([]);

        var node = new AccessMethodNode(token, args, extraArgs, false);

        var str = node.ToString();
        Assert.AreEqual("NoAliasFunc()", str);
    }

    [TestMethod]
    public void AccessMethodNode_ChangeMethod_UpdatesMethod()
    {
        var token = new FunctionToken("TestFunc", new TextSpan(0, 8));
        var args = new ArgsListNode([]);
        var extraArgs = new ArgsListNode([]);

        var node = new AccessMethodNode(token, args, extraArgs, false);
        Assert.IsNull(node.Method);

        var method = typeof(string).GetMethod("ToUpper", Type.EmptyTypes);
        node.ChangeMethod(method);

        Assert.IsNotNull(node.Method);
        Assert.AreEqual(method, node.Method);
    }

    [TestMethod]
    public void AccessMethodNode_ReturnType_WithMethod_ReturnsMethodReturnType()
    {
        var token = new FunctionToken("ToUpper", new TextSpan(0, 7));
        var args = new ArgsListNode([]);
        var extraArgs = new ArgsListNode([]);

        var method = typeof(string).GetMethod("ToUpper", Type.EmptyTypes);
        var node = new AccessMethodNode(token, args, extraArgs, false, method);

        Assert.AreEqual(typeof(string), node.ReturnType);
    }

    [TestMethod]
    public void AccessMethodNode_ToString_WithArgs()
    {
        var token = new FunctionToken("TestFunc", new TextSpan(0, 8));
        var args = new ArgsListNode([new IntegerNode("1"), new IntegerNode("2")]);
        var extraArgs = new ArgsListNode([]);

        var node = new AccessMethodNode(token, args, extraArgs, false);

        var str = node.ToString();
        Assert.IsTrue(str.StartsWith("TestFunc("));
    }

    #endregion

    #region RefreshNode Tests

    [TestMethod]
    public void RefreshNode_Constructor_EmptyNodes_SetsProperties()
    {
        var node = new RefreshNode([]);

        Assert.AreEqual(0, node.Nodes.Length);
        Assert.IsNotNull(node.Id);
        Assert.IsTrue(node.Id.StartsWith("RefreshNode"));
    }

    [TestMethod]
    public void RefreshNode_Constructor_WithNodes_SetsProperties()
    {
        var token = new FunctionToken("TestMethod", new TextSpan(0, 10));
        var args = new ArgsListNode([]);
        var accessMethod = new AccessMethodNode(token, args, args, false);

        var node = new RefreshNode([accessMethod]);

        Assert.AreEqual(1, node.Nodes.Length);
        Assert.AreSame(accessMethod, node.Nodes[0]);
    }

    [TestMethod]
    public void RefreshNode_ReturnType_IsNull()
    {
        var node = new RefreshNode([]);

        Assert.IsNull(node.ReturnType);
    }

    [TestMethod]
    public void RefreshNode_ToString_EmptyNodes_Works()
    {
        var node = new RefreshNode([]);

        var str = node.ToString();
        Assert.IsNotNull(str);
        Assert.AreEqual("refresh ()", str);
    }

    [TestMethod]
    public void RefreshNode_ToString_WithNodes_Works()
    {
        var token = new FunctionToken("SomeMethod", new TextSpan(0, 10));
        var args = new ArgsListNode([]);
        var accessMethod = new AccessMethodNode(token, args, args, false);

        var node = new RefreshNode([accessMethod]);

        var str = node.ToString();
        Assert.IsNotNull(str);
        Assert.IsTrue(str.StartsWith("refresh ("));
    }

    #endregion

    #region RenameTableNode Tests

    [TestMethod]
    public void RenameTableNode_Constructor_SetsProperties()
    {
        var node = new RenameTableNode("oldName", "newName");

        Assert.AreEqual("oldName", node.TableSourceName);
        Assert.AreEqual("newName", node.TableDestinationName);
    }

    [TestMethod]
    public void RenameTableNode_ToString_Works()
    {
        var node = new RenameTableNode("source", "dest");

        var str = node.ToString();
        Assert.IsNotNull(str);
    }

    #endregion

    #region InternalQueryNode Tests

    [TestMethod]
    public void InternalQueryNode_Constructor_SetsProperties()
    {
        var select = new SelectNode([]);
        var dual = new SchemaFromNode("system", "dual", new ArgsListNode([]), "d", typeof(object), 0);
        var refresh = new RefreshNode([]);

        var node = new InternalQueryNode(select, dual, null, null, null, null, null, refresh);

        Assert.AreSame(select, node.Select);
        Assert.AreSame(dual, node.From);
        Assert.AreSame(refresh, node.Refresh);
    }

    [TestMethod]
    public void InternalQueryNode_ToString_Works()
    {
        var select = new SelectNode([]);
        var dual = new SchemaFromNode("system", "dual", new ArgsListNode([]), "d", typeof(object), 0);
        var refresh = new RefreshNode([]);

        var node = new InternalQueryNode(select, dual, null, null, null, null, null, refresh);

        var str = node.ToString();
        Assert.IsNotNull(str);
    }

    [TestMethod]
    public void InternalQueryNode_ReturnType_IsNull()
    {
        var select = new SelectNode([]);
        var dual = new SchemaFromNode("system", "dual", new ArgsListNode([]), "d", typeof(object), 0);
        var refresh = new RefreshNode([]);

        var node = new InternalQueryNode(select, dual, null, null, null, null, null, refresh);

        Assert.IsNull(node.ReturnType);
    }

    #endregion

    #region CteExpressionNode Tests

    [TestMethod]
    public void CteExpressionNode_Constructor_SetsProperties()
    {
        var select = new SelectNode([]);
        var dual = new SchemaFromNode("system", "dual", new ArgsListNode([]), "d", typeof(object), 0);
        var query = new QueryNode(select, dual, null, null, null, null, null);
        var cteInner = new CteInnerExpressionNode(query, "cte1");
        var innerNodes = new[] { cteInner };

        var node = new CteExpressionNode(innerNodes, query);

        Assert.AreSame(innerNodes, node.InnerExpression);
        Assert.AreSame(query, node.OuterExpression);
    }

    [TestMethod]
    public void CteExpressionNode_ToString_Works()
    {
        var select = new SelectNode([]);
        var dual = new SchemaFromNode("system", "dual", new ArgsListNode([]), "d", typeof(object), 0);
        var query = new QueryNode(select, dual, null, null, null, null, null);
        var cteInner = new CteInnerExpressionNode(query, "myCte");
        var innerNodes = new[] { cteInner };

        var node = new CteExpressionNode(innerNodes, query);

        var str = node.ToString();
        Assert.IsNotNull(str);
        Assert.IsTrue(str.StartsWith("with"));
    }

    [TestMethod]
    public void CteExpressionNode_MultipleInnerExpressions_ToString()
    {
        var select = new SelectNode([]);
        var dual = new SchemaFromNode("system", "dual", new ArgsListNode([]), "d", typeof(object), 0);
        var query = new QueryNode(select, dual, null, null, null, null, null);
        var cteInner1 = new CteInnerExpressionNode(query, "cte1");
        var cteInner2 = new CteInnerExpressionNode(query, "cte2");
        var innerNodes = new[] { cteInner1, cteInner2 };

        var node = new CteExpressionNode(innerNodes, query);

        var str = node.ToString();
        Assert.IsNotNull(str);
        Assert.IsTrue(str.Contains("cte1"));
        Assert.IsTrue(str.Contains("cte2"));
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

    #region PutTrueNode Tests

    [TestMethod]
    public void PutTrueNode_Constructor_SetsReturnType()
    {
        var node = new PutTrueNode();

        Assert.AreEqual(typeof(bool), node.ReturnType);
    }

    [TestMethod]
    public void PutTrueNode_Id_IsNotEmpty()
    {
        var node = new PutTrueNode();

        Assert.IsFalse(string.IsNullOrEmpty(node.Id));
    }

    [TestMethod]
    public void PutTrueNode_ToString_Works()
    {
        var node = new PutTrueNode();

        var str = node.ToString();
        Assert.IsNotNull(str);
    }

    #endregion

    #region RootNode Tests

    [TestMethod]
    public void RootNode_Constructor_SetsQuery()
    {
        var select = new SelectNode([]);
        var dual = new SchemaFromNode("system", "dual", new ArgsListNode([]), "d", typeof(object), 0);
        var query = new QueryNode(select, dual, null, null, null, null, null);

        var root = new RootNode(query);

        Assert.AreSame(query, root.Expression);
    }

    [TestMethod]
    public void RootNode_ToString_Works()
    {
        var select = new SelectNode([]);
        var dual = new SchemaFromNode("system", "dual", new ArgsListNode([]), "d", typeof(object), 0);
        var query = new QueryNode(select, dual, null, null, null, null, null);

        var root = new RootNode(query);

        var str = root.ToString();
        Assert.IsNotNull(str);
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
        Assert.IsTrue(str.Contains("alias"));
    }

    [TestMethod]
    public void SchemaFromNode_ToString_WithoutAlias_Works()
    {
        var parameters = new ArgsListNode([]);
        var node = new SchemaFromNode("schema", "method", parameters, "", typeof(object), 0);

        var str = node.ToString();
        Assert.IsNotNull(str);
        Assert.IsFalse(str.Contains(" "));
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

    #region QueryScope Tests

    [TestMethod]
    public void QueryScope_Constructor_SetsStatements()
    {
        var statements = new Node[] { new IntegerNode("1"), new StringNode("test") };
        var scope = new QueryScope(statements);

        Assert.AreSame(statements, scope.Statements);
    }

    [TestMethod]
    public void QueryScope_ReturnType_IsDefault()
    {
        var scope = new QueryScope([]);


        var _ = scope.ReturnType;
    }

    [TestMethod]
    public void QueryScope_Id_IsDefault()
    {
        var scope = new QueryScope([]);

        var _ = scope.Id;
    }

    [TestMethod]
    public void QueryScope_ToString_ReturnsNull()
    {
        var scope = new QueryScope([]);

        Assert.IsNull(scope.ToString());
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

        Assert.IsTrue(node.Id.StartsWith("SingleSetNode"));
    }

    #endregion

    #region CteInnerExpressionNode Tests

    [TestMethod]
    public void CteInnerExpressionNode_Constructor_SetsProperties()
    {
        var intNode = new IntegerNode("42");
        var cteInner = new CteInnerExpressionNode(intNode, "myInnerCte");

        Assert.AreSame(intNode, cteInner.Value);
        Assert.AreEqual("myInnerCte", cteInner.Name);
    }

    [TestMethod]
    public void CteInnerExpressionNode_ReturnType_IsVoid()
    {
        var cteInner = new CteInnerExpressionNode(new IntegerNode("1"), "test");

        Assert.AreEqual(typeof(void), cteInner.ReturnType);
    }

    [TestMethod]
    public void CteInnerExpressionNode_Id_ContainsValueId()
    {
        var intNode = new IntegerNode("99");
        var cteInner = new CteInnerExpressionNode(intNode, "test");

        Assert.IsTrue(cteInner.Id.StartsWith("CteInnerExpressionNode"));
    }

    [TestMethod]
    public void CteInnerExpressionNode_ToString_IncludesNameAndAs()
    {
        var cteInner = new CteInnerExpressionNode(new IntegerNode("1"), "myCte");

        var str = cteInner.ToString();
        Assert.IsTrue(str.Contains("myCte"));
        Assert.IsTrue(str.Contains("as"));
    }

    #endregion

    #region Additional Node Tests

    [TestMethod]
    public void RenameTableNode_ReturnType_IsVoid()
    {
        var node = new RenameTableNode("old", "new");

        Assert.AreEqual(typeof(void), node.ReturnType);
    }

    [TestMethod]
    public void RenameTableNode_Id_IsNotNull()
    {
        var node = new RenameTableNode("old", "new");

        Assert.IsNotNull(node.Id);
    }

    [TestMethod]
    public void RootNode_ReturnType_IsNotNull()
    {
        var select = new SelectNode([]);
        var dual = new SchemaFromNode("system", "dual", new ArgsListNode([]), "d", typeof(object), 0);
        var query = new QueryNode(select, dual, null, null, null, null, null);

        var root = new RootNode(query);


        var _ = root.ReturnType;
    }

    [TestMethod]
    public void RootNode_Id_IsNotNull()
    {
        var select = new SelectNode([]);
        var dual = new SchemaFromNode("system", "dual", new ArgsListNode([]), "d", typeof(object), 0);
        var query = new QueryNode(select, dual, null, null, null, null, null);

        var root = new RootNode(query);

        Assert.IsNotNull(root.Id);
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
        Assert.IsTrue(str.Contains("except"));
        Assert.IsTrue(str.Contains("id"));
        Assert.IsTrue(str.Contains("name"));
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
        Assert.IsTrue(str.Contains("intersect"));
        Assert.IsTrue(str.Contains("key"));
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
        Assert.IsTrue(str.Contains("union"));
        Assert.IsTrue(str.Contains("x"));
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
        Assert.IsTrue(str.Contains("union all"));
    }

    #endregion
}
