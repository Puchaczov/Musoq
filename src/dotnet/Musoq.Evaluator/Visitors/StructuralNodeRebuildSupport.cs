using System.Linq;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static class StructuralNodeRebuildSupport
{

    public static ParameterDeclarationNode RebuildParameter(ParameterDeclarationNode node, Node? defaultValue)
    {
        return node.TypeSyntax is { } typeSyntax
            ? new ParameterDeclarationNode(node.Name, typeSyntax, defaultValue, node.Span)
            : new ParameterDeclarationNode(node.Name, node.TypeName, node.IsNullable, defaultValue, node.Span);
    }

    public static ScriptVariableDeclarationNode RebuildScriptVariable(ScriptVariableDeclarationNode node, Node initializer)
    {
        return node.IsInferred
            ? new ScriptVariableDeclarationNode(node.Name, initializer, node.Span)
            : node.TypeSyntax is { } typeSyntax
                ? new ScriptVariableDeclarationNode(node.Name, typeSyntax, initializer, node.Span)
                : new ScriptVariableDeclarationNode(node.Name, node.TypeName, node.IsNullable, initializer, node.Span);
    }
    public static ArgsListNode Rebuild(ArgsListNode node, Node[] arguments)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(arguments);

        if (node is ArrayLiteralNode)
            return new ArrayLiteralNode(arguments, node.Span);

        if (node is RecordLiteralNode record)
        {
            var fields = record.Fields
                .Select((field, index) => new RecordFieldNode(
                    field.Name,
                    arguments[index],
                    field.NameSpan,
                    field.Span))
                .ToArray();
            return new RecordLiteralNode(fields, node.Span);
        }

        return new ArgsListNode(arguments, node.ArgumentNames, node.Span);
    }
}
