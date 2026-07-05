using System;
using System.Collections.Generic;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal sealed class SemanticTraversalFrame(
    Stack<Node> nodes,
    Stack<string> methods)
{
    public int NodeCount => nodes.Count;

    public int MethodCount => methods.Count;

    public void PushNode(Node node)
    {
        ArgumentNullException.ThrowIfNull(node);
        nodes.Push(node);
    }

    public Node PopNode(string visitorName, string operation)
    {
        if (nodes.Count == 0)
            throw VisitorException.CreateForStackUnderflow(visitorName, operation, 1, 0);

        return nodes.Pop();
    }

    public TNode PopNode<TNode>(string visitorName, string operation)
        where TNode : Node
    {
        var node = PopNode(visitorName, operation);
        if (node is TNode typed)
            return typed;

        throw VisitorException.CreateForInvalidNodeType(
            visitorName,
            operation,
            typeof(TNode).Name,
            node.GetType().Name);
    }

    public Node[] PopNodes(string visitorName, int count, string operation)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be non-negative");

        if (nodes.Count < count)
            throw VisitorException.CreateForStackUnderflow(visitorName, operation, count, nodes.Count);

        var result = new Node[count];
        for (var index = count - 1; index >= 0; index--)
            result[index] = nodes.Pop();

        return result;
    }

    public Node PeekNode(string visitorName, string operation)
    {
        if (nodes.Count == 0)
            throw VisitorException.CreateForStackUnderflow(visitorName, operation, 1, 0);

        return nodes.Peek();
    }

    public TNode PeekNode<TNode>(string visitorName, string operation)
        where TNode : Node
    {
        var node = PeekNode(visitorName, operation);
        if (node is TNode typed)
            return typed;

        throw VisitorException.CreateForInvalidNodeType(
            visitorName,
            operation,
            typeof(TNode).Name,
            node.GetType().Name);
    }

    public void PushMethod(string methodName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);
        methods.Push(methodName);
    }

    public string PopMethod(string visitorName, string operation)
    {
        if (methods.Count == 0)
            throw VisitorException.CreateForStackUnderflow(visitorName, operation, 1, 0);

        return methods.Pop();
    }
}
