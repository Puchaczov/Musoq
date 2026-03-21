using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

/// <summary>
///     Tests for miscellaneous Parser nodes: Token, RefreshNode, RenameTableNode, PutTrueNode, RootNode, and additional node coverage.
/// </summary>
[TestClass]
public class ParserNodesExtended_MiscNodeTests
{
    #region Additional Token Tests

    [TestMethod]
    public void ColumnKeywordToken_Constructor_SetsProperties()
    {
        var token = new ColumnKeywordToken(new TextSpan(0, 6));

        Assert.AreEqual("column", token.Value);
        Assert.AreEqual(TokenType.ColumnKeyword, token.TokenType);
    }

    #endregion

    #region RefreshNode Tests

    [TestMethod]
    public void RefreshNode_Constructor_EmptyNodes_SetsProperties()
    {
        var node = new RefreshNode([]);

        Assert.IsEmpty(node.Nodes);
        Assert.IsNotNull(node.Id);
        Assert.StartsWith("RefreshNode", node.Id);
    }

    [TestMethod]
    public void RefreshNode_Constructor_WithNodes_SetsProperties()
    {
        var token = new FunctionToken("TestMethod", new TextSpan(0, 10));
        var args = new ArgsListNode([]);
        var accessMethod = new AccessMethodNode(token, args, args, false);

        var node = new RefreshNode([accessMethod]);

        Assert.HasCount(1, node.Nodes);
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
        Assert.StartsWith("refresh (", str);
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
}
