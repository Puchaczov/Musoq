using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private DescNode ComposeDesc()
    {
        Consume(Current.TokenType);

        if (IsSettingsOption())
            return ComposeDescSettings();

        if (IsContextualKeyword("query"))
            return ComposeDescQuery();

        if (Current.TokenType == TokenType.Functions)
        {
            Consume(TokenType.Functions);

            if (Current.TokenType == TokenType.MethodAccess)
            {
                var sourceAlias = Current.Value;
                var schemaName = EnsureHashPrefix(sourceAlias);
                ComposeAccessMethod(sourceAlias);

                return new DescNode(
                    new SchemaFromNode(schemaName, string.Empty, ArgsListNode.Empty, string.Empty, 1),
                    DescForType.FunctionsForSchema);
            }

            var schemaNameForFunctions = ComposeSchemaName();
            var schemaToken = Current;

            if (Current.TokenType == TokenType.Dot)
            {
                Consume(TokenType.Dot);

                if (Current is FunctionToken)
                {
                    ComposeAccessMethod(string.Empty);
                    return new DescNode(
                        new SchemaFromNode(schemaNameForFunctions, string.Empty, ArgsListNode.Empty,
                            string.Empty, 1), DescForType.FunctionsForSchema);
                }

                ConsumeAndGetToken(TokenType.Property);
            }

            return new DescNode(
                new SchemaFromNode(schemaNameForFunctions, string.Empty, ArgsListNode.Empty, string.Empty,
                    schemaToken.Span.Start), DescForType.FunctionsForSchema);
        }

        if (Current.TokenType == TokenType.MethodAccess)
        {
            var sourceAlias = Current.Value;
            var schemaName = EnsureHashPrefix(sourceAlias);
            var accessMethod = ComposeAccessMethod(sourceAlias);

            var fromNode = new SchemaFromNode(schemaName, accessMethod.Name, accessMethod.Arguments, string.Empty, 1);
            return TryParseColumnClause(fromNode);
        }

        var name = ComposeSchemaName();
        var startToken = Current;

        if (Current.TokenType == TokenType.Dot)
        {
            Consume(TokenType.Dot);

            FromNode fromNode;
            if (Current is FunctionToken)
            {
                var accessMethod = ComposeAccessMethod(string.Empty);

                fromNode = new SchemaFromNode(name, accessMethod.Name, accessMethod.Arguments, string.Empty, 1);
                return TryParseColumnClause(fromNode);
            }

            var methodName = new WordNode(ConsumeAndGetToken(TokenType.Property).Value);

            fromNode = new SchemaFromNode(name, methodName.Value, ArgsListNode.Empty, string.Empty, 1);
            return new DescNode(fromNode, DescForType.Constructors);
        }

        return new DescNode(
            new SchemaFromNode(name, string.Empty, ArgsListNode.Empty, string.Empty, startToken.Span.Start),
            DescForType.Schema);
    }

    private DescNode ComposeDescQuery()
    {
        Consume(Current.TokenType);
        var opening = ConsumeAndGetToken(TokenType.LeftParenthesis);

        if (Current.TokenType is not (TokenType.Select or TokenType.From or TokenType.Pivot or TokenType.Unpivot or TokenType.With))
            throw new SyntaxException(
                "DESC QUERY requires a SELECT, FROM, PIVOT, UNPIVOT, or WITH query inside parentheses.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2024_InvalidSubquery,
                opening.Span);

        var query = Current.TokenType == TokenType.With
            ? ComposeCteExpression()
            : ComposeSetOperators(1);

        ConsumeAndGetToken(TokenType.RightParenthesis);

        return new DescNode(query);
    }


    private DescNode TryParseColumnClause(FromNode fromNode)
    {
        var isColumnKeyword = Current.TokenType is TokenType.Word or TokenType.Identifier &&
                              string.Equals(Current.Value, ColumnKeywordToken.TokenText,
                                  StringComparison.OrdinalIgnoreCase);

        if (!isColumnKeyword)
            return new DescNode(fromNode, DescForType.SpecificConstructor);

        Consume(Current.TokenType);
        var column = ParseColumnAccess();
        return new DescNode(fromNode, DescForType.SpecificColumn, column);
    }


    private Node ParseColumnAccess()
    {
        var node = ComposeArithmeticExpression(0);
        ValidateColumnAccessNode(node);
        return node;
    }


    private void ValidateColumnAccessNode(Node node)
    {
        switch (node)
        {
            case DotNode d:
                ValidateColumnAccessNode(d.Root);
                ValidateColumnAccessNode(d.Expression);
                break;
            case PropertyValueNode:
            case WordNode:
            case IdentifierNode:
                break;
            default:
                throw new SyntaxException(
                    $"Invalid column path. Expected property path but received {node.GetType().Name}",
                    _lexer.AlreadyResolvedQueryPart);
        }
    }


    private string ComposeSchemaName()
    {
        if (Current.TokenType == TokenType.Word)
            return ComposeWord().Value;

        if (Current.TokenType == TokenType.Identifier)
        {
            var identifier = ConsumeAndGetToken(TokenType.Identifier).Value;
            return EnsureHashPrefix(identifier);
        }

        throw new SyntaxException($"Expected schema name (Word or Identifier) but received {Current.TokenType}",
            _lexer.AlreadyResolvedQueryPart);
    }


    private static string EnsureHashPrefix(string name)
    {
        return name.StartsWith('#') ? name : $"#{name}";
    }


    private CoupleNode ComposeCouple()
    {
        Consume(TokenType.Couple);

        var from = ComposeSchemaMethod();

        Consume(TokenType.With);

        var (tableName, profileName) = ComposeCoupleOptions();

        Consume(TokenType.As);

        var identifierNode = (IdentifierNode)ComposeBaseTypes();

        return new CoupleNode(from, tableName, profileName, identifierNode.Name);
    }

}
