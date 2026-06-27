using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

public partial class RewriteQueryTraverseVisitor : InterpretationSchemaDefinitionSkippingTraverseVisitor<IScopeAwareExpressionVisitor>
{
    public override void Visit(InterpretCallNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public override void Visit(ParseCallNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public override void Visit(TryInterpretCallNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public override void Visit(TryParseCallNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public override void Visit(PartialInterpretCallNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public override void Visit(PartialParseCallNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public override void Visit(InterpretAtCallNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    private static bool IsInterpretFunctionCall(string methodName)
    {
        return methodName.Equals("Interpret", StringComparison.OrdinalIgnoreCase) ||
               methodName.Equals("Parse", StringComparison.OrdinalIgnoreCase) ||
               methodName.Equals("InterpretAt", StringComparison.OrdinalIgnoreCase) ||
               methodName.Equals("TryInterpret", StringComparison.OrdinalIgnoreCase) ||
               methodName.Equals("TryParse", StringComparison.OrdinalIgnoreCase) ||
               methodName.Equals("PartialInterpret", StringComparison.OrdinalIgnoreCase) ||
               methodName.Equals("PartialParse", StringComparison.OrdinalIgnoreCase);
    }

    private static Node CreateInterpretCallNode(AccessMethodNode accessMethod)
    {
        var args = accessMethod.Arguments.Args;
        var schemaName = accessMethod.TypeParameter;

        if (string.IsNullOrEmpty(schemaName))
        {
            ThrowIfOldInterpretSyntax(accessMethod.Name, args);
            throw new InvalidOperationException(
                $"{accessMethod.Name} requires a type parameter. Use '{accessMethod.Name}<SchemaName>(data)' syntax.");
        }

        if (accessMethod.Name.Equals("InterpretAt", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length < 2)
                throw new InvalidOperationException(
                    "InterpretAt<SchemaName> requires 2 arguments: data and offset");

            return new InterpretAtCallNode(args[0], args[1], schemaName);
        }

        if (args.Length < 1)
            throw new InvalidOperationException(
                $"{accessMethod.Name}<SchemaName> requires 1 argument: data");

        var dataArg = args[0];

        if (accessMethod.Name.Equals("Parse", StringComparison.OrdinalIgnoreCase))
            return new ParseCallNode(dataArg, schemaName);

        if (accessMethod.Name.Equals("TryInterpret", StringComparison.OrdinalIgnoreCase))
            return new TryInterpretCallNode(dataArg, schemaName);

        if (accessMethod.Name.Equals("TryParse", StringComparison.OrdinalIgnoreCase))
            return new TryParseCallNode(dataArg, schemaName);

        if (accessMethod.Name.Equals("PartialInterpret", StringComparison.OrdinalIgnoreCase))
            return new PartialInterpretCallNode(dataArg, schemaName);

        if (accessMethod.Name.Equals("PartialParse", StringComparison.OrdinalIgnoreCase))
            return new PartialParseCallNode(dataArg, schemaName);

        return new InterpretCallNode(dataArg, schemaName);
    }

    private static Node CreateInterpretCallNodeFromAliasedFrom(AliasedFromNode node)
    {
        var schemaName = node.TypeParameter;

        if (string.IsNullOrEmpty(schemaName))
        {
            ThrowIfOldInterpretSyntaxInAliasedFrom(node);
            throw new InvalidOperationException(
                $"{node.Identifier} requires a type parameter. Use '{node.Identifier}<SchemaName>(data)' syntax.");
        }

        var dataSource = node.Args.Args[0];

        if (node.Identifier.Equals("InterpretAt", StringComparison.OrdinalIgnoreCase))
        {
            if (node.Args.Args.Length < 2)
                throw new InvalidOperationException(
                    $"InterpretAt<SchemaName> requires 2 arguments: data and offset, got {node.Args.Args.Length}");

            var offset = node.Args.Args[1];
            return new InterpretAtCallNode(dataSource, offset, schemaName, node.ReturnType);
        }

        if (node.Args.Args.Length < 1)
            throw new InvalidOperationException(
                $"{node.Identifier}<SchemaName> requires 1 argument: data, got {node.Args.Args.Length}");

        if (node.Identifier.Equals("Parse", StringComparison.OrdinalIgnoreCase))
            return new ParseCallNode(dataSource, schemaName, node.ReturnType);

        if (node.Identifier.Equals("TryInterpret", StringComparison.OrdinalIgnoreCase))
            return new TryInterpretCallNode(dataSource, schemaName, node.ReturnType);

        if (node.Identifier.Equals("TryParse", StringComparison.OrdinalIgnoreCase))
            return new TryParseCallNode(dataSource, schemaName, node.ReturnType);

        if (node.Identifier.Equals("PartialInterpret", StringComparison.OrdinalIgnoreCase))
            return new PartialInterpretCallNode(dataSource, schemaName, node.ReturnType);

        if (node.Identifier.Equals("PartialParse", StringComparison.OrdinalIgnoreCase))
            return new PartialParseCallNode(dataSource, schemaName, node.ReturnType);

        return new InterpretCallNode(dataSource, schemaName, node.ReturnType);
    }

    private static void ThrowIfOldInterpretSyntax(string functionName, Node[] args)
    {
        if (functionName.Equals("InterpretAt", StringComparison.OrdinalIgnoreCase))
        {
            if (args is [_, _, StringNode schemaArg, ..])
                throw new InvalidOperationException(
                    $"The syntax 'InterpretAt(data, offset, ''{schemaArg.Value}'')' is no longer supported. Use 'InterpretAt<{schemaArg.Value}>(data, offset)' instead.");
        }
        else
        {
            if (args is [_, StringNode schemaArg, ..])
                throw new InvalidOperationException(
                    $"The syntax '{functionName}(data, ''{schemaArg.Value}'')' is no longer supported. Use '{functionName}<{schemaArg.Value}>(data)' instead.");
        }
    }

    private static void ThrowIfOldInterpretSyntaxInAliasedFrom(AliasedFromNode node)
    {
        var args = node.Args.Args;

        if (node.Identifier.Equals("InterpretAt", StringComparison.OrdinalIgnoreCase))
        {
            if (args is [_, _, StringNode or WordNode, ..])
            {
                var name = args[2] is StringNode s ? s.Value : ((WordNode)args[2]).Value;
                throw new InvalidOperationException(
                    $"The syntax 'InterpretAt(data, offset, ''{name}'')' is no longer supported. Use 'InterpretAt<{name}>(data, offset)' instead.");
            }
        }
        else
        {
            if (args is [_, StringNode or WordNode, ..])
            {
                var name = args[1] is StringNode s ? s.Value : ((WordNode)args[1]).Value;
                throw new InvalidOperationException(
                    $"The syntax '{node.Identifier}(data, ''{name}'')' is no longer supported. Use '{node.Identifier}<{name}>(data)' instead.");
            }
        }
    }
}
