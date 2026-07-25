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
        var recursiveToken = IsContextualRecursiveKeyword() ? ConsumeAndGetToken() : null;
        var isRecursive = recursiveToken != null && Current.TokenType is not TokenType.As and not TokenType.LeftParenthesis;

        PushFromAliasesScope();
        try
        {
            var expressions = new List<CteInnerExpressionNode>();

            var col = recursiveToken != null && !isRecursive ? new IdentifierNode(recursiveToken.Value, null, recursiveToken.Span) : ComposeBaseTypes() as IdentifierNode;
            if (col == null)
                throw new SyntaxException($"Expected token is {TokenType.Identifier} but received {Current.TokenType}",
                    _lexer.AlreadyResolvedQueryPart);

            RegisterFromAlias(col.Name);
            var columns = ComposeOptionalCteColumnList();

            Consume(TokenType.As);
            Consume(TokenType.LeftParenthesis);
            var innerSets = ComposeSetOperators(0);
            var closingParenthesis = ConsumeAndGetToken(TokenType.RightParenthesis);
            expressions.Add((CteInnerExpressionNode)new CteInnerExpressionNode(innerSets, col.Name, columns)
                .WithSpan(col.Span.Through(closingParenthesis.Span)));

            while (Current.TokenType == TokenType.Comma)
            {
                Consume(TokenType.Comma);

                if (ComposeBaseTypes() is not IdentifierNode nextColumn)
                    throw new SyntaxException(
                        $"Expected token is {TokenType.Identifier} but received {Current.TokenType}",
                        _lexer.AlreadyResolvedQueryPart);

                RegisterFromAlias(nextColumn.Name);
                columns = ComposeOptionalCteColumnList();

                Consume(TokenType.As);

                Consume(TokenType.LeftParenthesis);
                innerSets = ComposeSetOperators(0);
                closingParenthesis = ConsumeAndGetToken(TokenType.RightParenthesis);
                expressions.Add((CteInnerExpressionNode)new CteInnerExpressionNode(
                        innerSets,
                        nextColumn.Name,
                        columns)
                    .WithSpan(nextColumn.Span.Through(closingParenthesis.Span)));
            }

            var outerSets = ComposeSetOperators(0);

            return new CteExpressionNode(expressions.ToArray(), outerSets, isRecursive);
        }
        finally
        {
            PopFromAliasesScope();
        }
    }

    private CteColumnName[] ComposeOptionalCteColumnList()
    {
        if (Current.TokenType != TokenType.LeftParenthesis)
            return [];

        var openingParenthesis = ConsumeAndGetToken(TokenType.LeftParenthesis);
        var columns = new List<CteColumnName>();

        while (true)
        {
            if (columns.Count == 0 && Current.TokenType is TokenType.Select or TokenType.From or
                TokenType.Pivot or TokenType.Unpivot or TokenType.With)
            {
                throw new SyntaxException(
                    $"Expected token is {TokenType.As} but received {openingParenthesis.TokenType}.",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ2001_UnexpectedToken,
                    openingParenthesis.Span);
            }

            if (ComposeBaseTypes() is not IdentifierNode column)
                throw new SyntaxException(
                    $"Expected token is {TokenType.Identifier} but received {Current.TokenType}",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ2013_InvalidCTE,
                    Current.Span);

            columns.Add(new CteColumnName(column.Name, column.Span));

            if (Current.TokenType != TokenType.Comma)
                break;

            Consume(TokenType.Comma);
        }

        Consume(TokenType.RightParenthesis);
        return columns.ToArray();
    }

    private bool IsContextualRecursiveKeyword()
    {
        return Current.TokenType is TokenType.Identifier or TokenType.Word or TokenType.Function &&
               Current.Value.Equals("recursive", StringComparison.OrdinalIgnoreCase);
    }

}
