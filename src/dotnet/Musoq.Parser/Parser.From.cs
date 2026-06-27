using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private FromNode ComposeFrom(bool fromKeywordBefore = true, bool isApplyContext = false)
    {
        if (fromKeywordBefore)
            Consume(TokenType.From);

        if (IsValuesSource())
            return ComposeValuesFrom();

        if (Current.TokenType == TokenType.LeftParenthesis)
            return ComposeDerivedTableFrom(isApplyContext);

        string alias;
        TextSpan aliasSpan;
        if (Current.TokenType == TokenType.Word)
        {
            var name = ComposeWord();

            FromNode fromNode;
            if (Current.TokenType == TokenType.Dot)
            {
                Consume(TokenType.Dot);
                var accessMethod = ComposeAccessMethod(string.Empty);

                (alias, aliasSpan) = ComposeAlias();

                fromNode = new SchemaFromNode(name.Value, accessMethod.Name, accessMethod.Arguments, alias,
                    _fromPosition);
            }
            else
            {
                (alias, aliasSpan) = ComposeAlias();
                fromNode = new ReferentialFromNode(name.Value, alias);
            }

            if (!aliasSpan.IsEmpty)
                fromNode.WithSpan(aliasSpan);

            if (!string.IsNullOrWhiteSpace(alias))
                RegisterFromAlias(alias);

            return fromNode;
        }

        if (Current.TokenType == TokenType.Function)
        {
            var method = ComposeAccessMethod(string.Empty);
            (alias, aliasSpan) = ComposeAlias();

            if (!string.IsNullOrWhiteSpace(alias))
                RegisterFromAlias(alias);

            var fromNode = new AliasedFromNode(method.Name, method.Arguments, alias, _fromPosition, method.TypeParameter);
            if (!aliasSpan.IsEmpty)
                fromNode.WithSpan(aliasSpan);
            return fromNode;
        }

        if (Current.TokenType == TokenType.MethodAccess)
        {
            var sourceAlias = Current.Value;
            var accessMethod = ComposeAccessMethod(sourceAlias);
            (alias, aliasSpan) = ComposeAlias();

            var isSchemaReference = sourceAlias.StartsWith('#') ||
                                    !isApplyContext ||
                                    (isApplyContext && !IsKnownFromAlias(sourceAlias));

            if (isSchemaReference && !sourceAlias.StartsWith('#'))
            {
                var schemaName = EnsureHashPrefix(sourceAlias);

                if (!string.IsNullOrWhiteSpace(alias))
                    RegisterFromAlias(alias);

                var fromNode = new SchemaFromNode(schemaName, accessMethod.Name, accessMethod.Arguments, alias, _fromPosition);
                if (!aliasSpan.IsEmpty)
                    fromNode.WithSpan(aliasSpan);
                return fromNode;
            }

            if (sourceAlias.StartsWith('#'))
            {
                if (!string.IsNullOrWhiteSpace(alias))
                    RegisterFromAlias(alias);

                var fromNode = new SchemaFromNode(sourceAlias, accessMethod.Name, accessMethod.Arguments, alias, _fromPosition);
                if (!aliasSpan.IsEmpty)
                    fromNode.WithSpan(aliasSpan);
                return fromNode;
            }

            if (string.IsNullOrWhiteSpace(alias))
                throw new NotSupportedException("Alias cannot be empty when parsing From clause.");

            var accessFromNode = new AccessMethodFromNode(alias, sourceAlias, accessMethod);
            if (!aliasSpan.IsEmpty)
                accessFromNode.WithSpan(aliasSpan);

            RegisterFromAlias(alias);

            return accessFromNode;
        }


        var baseNode = ComposeBaseTypes();
        var columnName = baseNode switch
        {
            IdentifierNode id => id.Name,
            WordNode word => word.Value,
            _ => throw new NotSupportedException($"Expected identifier or word but got {baseNode.GetType().Name}")
        };

        if (Current.TokenType == TokenType.Dot)
        {
            Consume(Current.TokenType);

            if (Current.TokenType == TokenType.Function)
            {
                var accessMethod = ComposeAccessMethod(string.Empty);
                (alias, aliasSpan) = ComposeAlias();

                var schemaName = EnsureHashPrefix(columnName);

                if (!string.IsNullOrWhiteSpace(alias))
                    RegisterFromAlias(alias);

                var fromNode = new SchemaFromNode(schemaName, accessMethod.Name, accessMethod.Arguments, alias, _fromPosition);
                if (!aliasSpan.IsEmpty)
                    fromNode.WithSpan(aliasSpan);
                return fromNode;
            }

            var properties = new List<string>();
            var anyParsed = false;

            while (Current.TokenType == TokenType.Property)
            {
                if (!anyParsed)
                    anyParsed = true;

                var propertyName = Current.Value;
                properties.Add(propertyName);

                Consume(TokenType.Property);

                if (Current.TokenType == TokenType.Dot)
                {
                    Consume(TokenType.Dot);
                    continue;
                }

                break;
            }

            if (anyParsed)
            {
                (alias, aliasSpan) = ComposeAlias();

                if (string.IsNullOrWhiteSpace(alias))
                    throw new NotSupportedException("Alias cannot be empty when parsing From clause.");

                RegisterFromAlias(alias);
                var fromNode = new PropertyFromNode(alias, columnName, properties.ToArray());
                if (!aliasSpan.IsEmpty)
                    fromNode.WithSpan(aliasSpan);
                return fromNode;
            }

            throw new NotSupportedException($"Unrecognized token {Current.TokenType} when parsing From clause.");
        }

        (alias, aliasSpan) = ComposeAlias();

        if (!string.IsNullOrWhiteSpace(alias))
            RegisterFromAlias(alias);

        var inMemoryNode = new InMemoryTableFromNode(columnName, alias);
        if (!aliasSpan.IsEmpty)
            inMemoryNode.WithSpan(aliasSpan);
        return inMemoryNode;
    }



    private ValuesFromNode ComposeValuesFrom()
    {
        var valuesToken = ConsumeAndGetToken(Current.TokenType);
        Consume(TokenType.LBracket);

        var rows = new List<ValuesRowNode>();
        while (Current.TokenType != TokenType.RBracket)
        {
            rows.Add(ComposeValuesRow());

            if (Current.TokenType != TokenType.Comma)
                break;

            Consume(TokenType.Comma);
            if (Current.TokenType == TokenType.RBracket)
                break;
        }

        var closingToken = ConsumeAndGetToken(TokenType.RBracket);
        var (alias, aliasSpan) = ComposeAlias();

        if (string.IsNullOrWhiteSpace(alias))
            throw new SyntaxException(
                "VALUES source requires an alias after the closing brace. Example: from values { { Name: 'A' } } v.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2022_InvalidAlias,
                closingToken.Span);

        RegisterFromAlias(alias);

        var fromNode = new ValuesFromNode(rows, alias);
        fromNode.WithSpan(valuesToken.Span.Through(closingToken.Span));

        if (!aliasSpan.IsEmpty)
            fromNode.WithFullSpan(fromNode.Span.Through(aliasSpan));

        return fromNode;
    }


    private ValuesRowNode ComposeValuesRow()
    {
        var openingToken = ConsumeAndGetToken(TokenType.LBracket);

        var fields = new List<ValuesFieldNode>();
        while (Current.TokenType != TokenType.RBracket)
        {
            var fieldToken = ComposeValuesFieldName();
            Consume(TokenType.Colon);

            fields.Add(new ValuesFieldNode(fieldToken.Value, ComposeOperations(), fieldToken.Span));

            if (Current.TokenType != TokenType.Comma)
                break;

            Consume(TokenType.Comma);
            if (Current.TokenType == TokenType.RBracket)
                break;
        }

        var closingToken = ConsumeAndGetToken(TokenType.RBracket);

        return new ValuesRowNode(fields, openingToken.Span.Through(closingToken.Span));
    }


    private Token ComposeValuesFieldName()
    {
        if (Current.TokenType is not (TokenType.Identifier or TokenType.Word))
            throw new SyntaxException(
                "Expected field name in VALUES row literal.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2002_MissingToken,
                Current.Span);

        return ConsumeAndGetToken(Current.TokenType);
    }


    private void PushFromAliasesScope()
    {
        _fromAliasesStack.Push(new HashSet<string>(StringComparer.OrdinalIgnoreCase));
    }


    private void PopFromAliasesScope()
    {
        _fromAliasesStack.Pop();
    }


    private void RegisterFromAlias(string alias)
    {
        if (_fromAliasesStack.Count > 0 && !string.IsNullOrEmpty(alias))
            _fromAliasesStack.Peek().Add(alias);
    }


    private bool IsKnownFromAlias(string alias)
    {
        foreach (var scope in _fromAliasesStack)
            if (scope.Contains(alias))
                return true;
        return false;
    }

    private enum Associativity
    {
        Left,
        Right
    }
}
