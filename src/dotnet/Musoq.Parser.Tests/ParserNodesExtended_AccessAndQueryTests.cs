using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

/// <summary>
///     Tests for AccessMethodNode, InternalQueryNode, CteExpressionNode, CteInnerExpressionNode, and QueryScope.
/// </summary>
[TestClass]
public class ParserNodesExtended_AccessAndQueryTests
{
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
        Assert.Contains("myAlias", str);
        Assert.Contains("Func", str);
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
        Assert.StartsWith("TestFunc(", str);
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
        Assert.StartsWith("with", str);
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
        Assert.Contains("cte1", str);
        Assert.Contains("cte2", str);
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

        Assert.StartsWith("CteInnerExpressionNode", cteInner.Id);
    }

    [TestMethod]
    public void CteInnerExpressionNode_ToString_IncludesNameAndAs()
    {
        var cteInner = new CteInnerExpressionNode(new IntegerNode("1"), "myCte");

        var str = cteInner.ToString();
        Assert.Contains("myCte", str);
        Assert.Contains("as", str);
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
}
