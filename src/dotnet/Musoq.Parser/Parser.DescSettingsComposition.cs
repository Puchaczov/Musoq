using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private DescNode ComposeDescSettings()
    {
        Consume(Current.TokenType);

        if (Current.TokenType == TokenType.MethodAccess)
        {
            var sourceAlias = Current.Value;
            var schemaName = EnsureHashPrefix(sourceAlias);
            var accessMethod = ComposeAccessMethod(sourceAlias);
            var fromNode = new SchemaFromNode(schemaName, accessMethod.Name, accessMethod.Arguments, string.Empty, 1);
            return new DescNode(fromNode, DescForType.Settings);
        }

        var targetToken = Current;
        var targetName = targetToken.Value;
        var hasHashPrefix = targetName.StartsWith('#');
        var name = ComposeSchemaName();

        if (Current.TokenType == TokenType.Dot)
        {
            Consume(TokenType.Dot);

            if (Current is FunctionToken)
            {
                var accessMethod = ComposeAccessMethod(string.Empty);
                return new DescNode(
                    new SchemaFromNode(name, accessMethod.Name, accessMethod.Arguments, string.Empty, 1),
                    DescForType.Settings);
            }

            var methodName = new WordNode(ConsumeAndGetToken(TokenType.Property).Value);
            return new DescNode(
                new SchemaFromNode(name, methodName.Value, ArgsListNode.Empty, string.Empty, 1),
                DescForType.Settings);
        }

        if (hasHashPrefix)
            throw new SyntaxException(
                "DESC SETTINGS requires a source method or a coupled alias.",
                _lexer.AlreadyResolvedQueryPart);

        return new DescNode(
            new AliasedFromNode(targetName, ArgsListNode.Empty, string.Empty, targetToken.Span.Start),
            DescForType.Settings);
    }
}
