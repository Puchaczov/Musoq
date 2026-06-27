using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Traversal;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class AstTraversalTests
{
    [TestMethod]
    public void AstChildren_ShouldReturnDirectNodeChildrenInDeclarationOrder()
    {
        var select = new SelectNode([new FieldNode(new IntegerNode(1), 0, "one")]);
        var from = new SchemaFromNode("test", "data", ArgsListNode.Empty, "d", typeof(object), 0);
        var where = new WhereNode(new BooleanNode(true));
        var query = new QueryNode(select, from, where, null, null, null, null);

        var children = AstChildren.Of(query);

        CollectionAssert.AreEqual(
            new[] { "Select", "From", "Where" },
            children.Select(static child => child.Path).ToArray());
        CollectionAssert.AreEqual(
            new Node[] { select, from, where },
            children.Select(static child => child.Node).ToArray());
    }

    [TestMethod]
    public void AstChildren_ShouldDescendThroughNodeLikeContainers()
    {
        var expression = new IntegerNode(42);
        var values = new ValuesFromNode(
            [new ValuesRowNode([new ValuesFieldNode("answer", expression)])],
            "v");

        var children = AstChildren.Of(values);

        Assert.AreEqual(1, children.Count);
        Assert.AreEqual("Rows[0].Fields[0].Expression", children[0].Path);
        Assert.AreSame(expression, children[0].Node);
    }

    [TestMethod]
    public void AstWalker_ShouldVisitTreeDepthFirst()
    {
        var root = new RootNode(new AddNode(new IntegerNode(1), new IntegerNode(2)));
        var walker = new RecordingWalker();

        walker.Walk(root);

        CollectionAssert.AreEqual(
            new[] { nameof(RootNode), nameof(AddNode), nameof(IntegerNode), nameof(IntegerNode) },
            walker.VisitedTypes.ToArray());
    }

    [TestMethod]
    public void AstChildren_ShouldExposeTraversalMetadataForEveryConcreteNodeType()
    {
        var concreteNodeTypes = typeof(Node).Assembly
            .GetTypes()
            .Where(static type => type is { IsClass: true, IsAbstract: false } && typeof(Node).IsAssignableFrom(type))
            .OrderBy(static type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        var missingTraversalMetadata = concreteNodeTypes
            .Where(static type => HasPublicNodeLikeMembers(type))
            .Where(static type => AstChildren.GetTraversalMemberNames(type).Count == 0)
            .Select(static type => type.FullName)
            .ToArray();

        Assert.IsEmpty(
            missingTraversalMetadata,
            "Every concrete parser node with node-like public members should have AstChildren traversal metadata: " +
            string.Join(", ", missingTraversalMetadata));
    }

    private static bool HasPublicNodeLikeMembers(Type type)
    {
        return type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(static property => property.GetIndexParameters().Length == 0)
            .Any(static property => typeof(Node).IsAssignableFrom(property.PropertyType) ||
                                    property.PropertyType != typeof(string) &&
                                    typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType));
    }

    private sealed class RecordingWalker : AstWalker
    {
        public List<string> VisitedTypes { get; } = [];

        protected override bool Enter(Node node)
        {
            VisitedTypes.Add(node.GetType().Name);
            return true;
        }
    }

}
