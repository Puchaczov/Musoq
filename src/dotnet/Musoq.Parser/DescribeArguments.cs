using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private DescNode ComposeDescArguments()
    {
        Consume(Current.TokenType);

        // A hash-qualified source can be written with or without a call
        // argument list. The latter is the inventory form and intentionally
        // keeps an empty ArgsListNode so semantic binding can inspect metadata
        // without evaluating a source.
        if (Current.TokenType == TokenType.MethodAccess)
        {
            var sourceAlias = Current.Value;
            var schemaName = EnsureHashPrefix(sourceAlias);
            var accessMethod = ComposeAccessMethod(sourceAlias, true);
            return new DescNode(
                new SchemaFromNode(schemaName, accessMethod.Name, accessMethod.Arguments, string.Empty, 1),
                DescForType.Arguments);
        }

        var targetToken = Current;
        var name = ComposeSchemaName();
        var startToken = targetToken;

        if (Current.TokenType == TokenType.Dot)
        {
            Consume(TokenType.Dot);

            if (Current is FunctionToken)
            {
                var accessMethod = ComposeAccessMethod(string.Empty, true);
                return new DescNode(
                    new SchemaFromNode(name, accessMethod.Name, accessMethod.Arguments, string.Empty, 1),
                    DescForType.Arguments);
            }

            // Method references without parentheses are accepted for the
            // inventory form, matching DESC FUNCTIONS and normal DESC output.
            var methodName = new WordNode(ConsumeAndGetToken(TokenType.Property).Value);
            return new DescNode(
                new SchemaFromNode(name, methodName.Value, ArgsListNode.Empty, string.Empty, startToken.Span.Start),
                DescForType.Arguments);
        }

        if (targetToken.Value.StartsWith("#", StringComparison.Ordinal))
            throw new SyntaxException(
                "DESC ARGUMENTS requires a source method or a coupled alias.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2030_UnsupportedSyntax,
                startToken.Span);

        return new DescNode(
            new AliasedFromNode(targetToken.Value, ArgsListNode.Empty, string.Empty, startToken.Span.Start),
            DescForType.Arguments);
    }
}