using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private StatementNode ComposeStatement()
    {
        if (IsParameterBlockStart())
            return ComposeAndSkipIfPresent(p => new StatementNode(p.ComposeParameterBlock()), TokenType.Semicolon);

        if (IsScriptVariableDeclarationStart())
            return ComposeAndSkipIfPresent(p => new StatementNode(p.ComposeScriptVariableDeclaration()), TokenType.Semicolon);

        if (Current.TokenType is TokenType.Identifier or TokenType.Word or TokenType.Function &&
            Current.Value.Equals("def", StringComparison.OrdinalIgnoreCase))
        {
            throw new SyntaxException(
                "Python-style function declarations are not supported for script parameters. Use Musoq syntax: param(author: string) select ...",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2032_UnsupportedScriptParameterSyntax,
                Current.Span);
        }

        if (Current.TokenType is TokenType.Identifier or TokenType.Word or TokenType.Function &&
            Current.Value.Equals("declare", StringComparison.OrdinalIgnoreCase))
        {
            throw new SyntaxException(
                "SQL variable declarations are not supported for script parameters. Use Musoq syntax: param(author: string) select ...",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2032_UnsupportedScriptParameterSyntax,
                Current.Span);
        }

        if (IsDiagnosticCommandStart())
            return ComposeAndSkipIfPresent(p => new StatementNode(p.ComposeDiagnosticCommand()), TokenType.Semicolon);
        if (Current.TokenType == TokenType.Identifier &&
            (Current.Value.Equals("binary", StringComparison.OrdinalIgnoreCase) ||
             Current.Value.Equals("text", StringComparison.OrdinalIgnoreCase)))
        {
            _lexer.IsSchemaContext = true;
            return ComposeAndSkipIfPresent(p => new StatementNode(p.ComposeInterpretationSchema()),
                TokenType.Semicolon);
        }

        switch (Current.TokenType)
        {
            case TokenType.Desc:
                return ComposeAndSkipIfPresent(p => new StatementNode(p.ComposeDesc()), TokenType.Semicolon);
            case TokenType.Select:
                return ComposeAndSkipIfPresent(p => new StatementNode(p.ComposeSetOperators(0)), TokenType.Semicolon);
            case TokenType.From:
                return ComposeAndSkipIfPresent(p => new StatementNode(p.ComposeSetOperators(0)), TokenType.Semicolon);
            case TokenType.Pivot:
                return ComposeAndSkipIfPresent(p => new StatementNode(p.ComposeSetOperators(0)), TokenType.Semicolon);
            case TokenType.Unpivot:
                return ComposeAndSkipIfPresent(p => new StatementNode(p.ComposeSetOperators(0)), TokenType.Semicolon);
            case TokenType.With:
                return ComposeAndSkipIfPresent(p => new StatementNode(p.ComposeCteExpression()), TokenType.Semicolon);
            case TokenType.Table:
                return ComposeAndSkipIfPresent(p => new StatementNode(p.ComposeTable()), TokenType.Semicolon);
            case TokenType.Couple:
                return ComposeAndSkipIfPresent(p => new StatementNode(p.ComposeCouple()), TokenType.Semicolon);
            case TokenType.Binary:
            case TokenType.Text:
                return ComposeAndSkipIfPresent(p => new StatementNode(p.ComposeInterpretationSchema()),
                    TokenType.Semicolon);

            default:
                throw new SyntaxException(
                    $"Cannot compose statement, {Current.TokenType} is not expected here",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ2001_UnexpectedToken,
                    Current.Span);
        }
    }


    private Node ComposeInterpretationSchema()
    {
        var schemaParser = new SchemaParser(_lexer);


        return schemaParser.ParseSchemaFromCurrentPosition();
    }


    private CteExpressionNode ComposeCteExpression()
    {
        Consume(TokenType.With);

        PushFromAliasesScope();
        try
        {
            var expressions = new List<CteInnerExpressionNode>();

            if (ComposeBaseTypes() is not IdentifierNode col)
                throw new SyntaxException($"Expected token is {TokenType.Identifier} but received {Current.TokenType}",
                    _lexer.AlreadyResolvedQueryPart);

            RegisterFromAlias(col.Name);

            Consume(TokenType.As);
            Consume(TokenType.LeftParenthesis);
            var innerSets = ComposeSetOperators(0);
            expressions.Add(new CteInnerExpressionNode(innerSets, col.Name));
            Consume(TokenType.RightParenthesis);

            while (Current.TokenType == TokenType.Comma)
            {
                Consume(TokenType.Comma);

                if (ComposeBaseTypes() is not IdentifierNode nextColumn)
                    throw new SyntaxException(
                        $"Expected token is {TokenType.Identifier} but received {Current.TokenType}",
                        _lexer.AlreadyResolvedQueryPart);

                RegisterFromAlias(nextColumn.Name);

                Consume(TokenType.As);

                Consume(TokenType.LeftParenthesis);
                innerSets = ComposeSetOperators(0);
                Consume(TokenType.RightParenthesis);
                    expressions.Add(new CteInnerExpressionNode(innerSets, nextColumn.Name));
            }

            var outerSets = ComposeSetOperators(0);

            return new CteExpressionNode(expressions.ToArray(), outerSets);
        }
        finally
        {
            PopFromAliasesScope();
        }
    }

}
