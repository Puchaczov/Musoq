using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private ParsedSource ComposeFrom()
    {
        Consume(TokenType.From);
        return ComposeSource(SourceParseContext.Primary);
    }

    private ParsedSource ComposeSource(SourceParseContext context)
    {
        var sourceStart = Current.Span.Start;

        if (IsValuesSource())
            return ComposeValuesFrom();

        if (Current.TokenType == TokenType.LeftParenthesis)
            return ComposeDerivedTableFrom(context);

        string alias;
        TextSpan aliasSpan;
        if (Current.TokenType == TokenType.Word)
        {
            var name = ComposeWord();

            FromNode fromNode;
            if (Current.TokenType == TokenType.Dot)
            {
                Consume(TokenType.Dot);
                var accessMethod = ComposeAccessMethod(string.Empty, true);
                var sourceEndSpan = Previous?.Span ?? name.Span;
                var aliasResult = ComposeAlias(AliasContext.Source);
                EnsureAliasSyntax(aliasResult, AliasContext.Source);
                alias = aliasResult.Alias;
                aliasSpan = aliasResult.Span;

                fromNode = new SchemaFromNode(name.Value, accessMethod.Name, accessMethod.Arguments, alias,
                    _fromPosition);
                if (!aliasSpan.IsEmpty)
                    fromNode.WithSpan(aliasSpan);

                if (!string.IsNullOrWhiteSpace(alias))
                    RegisterFromAlias(alias);

                return ParsedSource.Create(fromNode, SourceKind.Schema, sourceStart, sourceEndSpan, aliasResult);
            }

            var referentialSourceEnd = name.Span;
            var referentialAlias = ComposeAlias(AliasContext.Source);
            EnsureAliasSyntax(referentialAlias, AliasContext.Source);
            alias = referentialAlias.Alias;
            aliasSpan = referentialAlias.Span;
            fromNode = new ReferentialFromNode(name.Value, alias);
            if (!aliasSpan.IsEmpty)
                fromNode.WithSpan(aliasSpan);

            RegisterFromAlias(string.IsNullOrWhiteSpace(alias) ? name.Value : alias);

            return ParsedSource.Create(fromNode, SourceKind.Referential, sourceStart, referentialSourceEnd,
                referentialAlias, name.Value);
        }

        if (Current.TokenType == TokenType.Function)
        {
            var method = ComposeAccessMethod(string.Empty, true);
            var sourceEndSpan = Previous?.Span ?? new TextSpan(sourceStart, 0);
            var aliasResult = ComposeAlias(AliasContext.Source);
            EnsureAliasSyntax(aliasResult, AliasContext.Source);
            alias = aliasResult.Alias;
            aliasSpan = aliasResult.Span;

            if (!string.IsNullOrWhiteSpace(alias))
                RegisterFromAlias(alias);

            var fromNode = new AliasedFromNode(method.Name, method.Arguments, alias, _fromPosition, method.TypeParameter);
            if (!aliasSpan.IsEmpty)
                fromNode.WithSpan(aliasSpan);
            return ParsedSource.Create(fromNode, SourceKind.Function, sourceStart, sourceEndSpan, aliasResult);
        }

        if (Current.TokenType == TokenType.MethodAccess)
        {
            var sourceAlias = Current.Value;
            var allowNamedArguments = sourceAlias.StartsWith('#') ||
                                      context != SourceParseContext.ApplyRight ||
                                      !IsKnownFromAlias(sourceAlias);
            var accessMethod = ComposeAccessMethod(sourceAlias, allowNamedArguments);
            var sourceEndSpan = Previous?.Span ?? new TextSpan(sourceStart, sourceAlias.Length);
            var aliasResult = ComposeAlias(AliasContext.Source);
            EnsureAliasSyntax(aliasResult, AliasContext.Source);
            alias = aliasResult.Alias;
            aliasSpan = aliasResult.Span;

            var isSchemaReference = sourceAlias.StartsWith('#') ||
                                    context != SourceParseContext.ApplyRight ||
                                    !IsKnownFromAlias(sourceAlias);

            if (isSchemaReference && !sourceAlias.StartsWith('#'))
            {
                var schemaName = EnsureHashPrefix(sourceAlias);

                if (!string.IsNullOrWhiteSpace(alias))
                    RegisterFromAlias(alias);

                var fromNode = new SchemaFromNode(schemaName, accessMethod.Name, accessMethod.Arguments, alias, _fromPosition);
                if (!aliasSpan.IsEmpty)
                    fromNode.WithSpan(aliasSpan);
                return ParsedSource.Create(fromNode, SourceKind.Schema, sourceStart, sourceEndSpan, aliasResult);
            }

            if (sourceAlias.StartsWith('#'))
            {
                if (!string.IsNullOrWhiteSpace(alias))
                    RegisterFromAlias(alias);

                var fromNode = new SchemaFromNode(sourceAlias, accessMethod.Name, accessMethod.Arguments, alias, _fromPosition);
                if (!aliasSpan.IsEmpty)
                    fromNode.WithSpan(aliasSpan);
                return ParsedSource.Create(fromNode, SourceKind.Schema, sourceStart, sourceEndSpan, aliasResult);
            }

            var accessFromNode = new AccessMethodFromNode(alias, sourceAlias, accessMethod);
            if (!aliasSpan.IsEmpty)
                accessFromNode.WithSpan(aliasSpan);

            if (!string.IsNullOrWhiteSpace(alias))
                RegisterFromAlias(alias);

            return ParsedSource.Create(accessFromNode, SourceKind.AccessMethod, sourceStart, sourceEndSpan, aliasResult);
        }

        var baseNode = ComposeBaseTypes();
        var columnName = baseNode switch
        {
            IdentifierNode id => id.Name,
            WordNode word => word.Value,
            _ => throw new SyntaxException(
                $"Expected identifier or word but got {baseNode.GetType().Name}.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2001_UnexpectedToken,
                baseNode.Span)
        };

        if (Current.TokenType == TokenType.Dot)
        {
            Consume(Current.TokenType);

            if (Current.TokenType == TokenType.Function)
            {
                var accessMethod = ComposeAccessMethod(string.Empty, true);
                var sourceEndSpan = Previous?.Span ?? baseNode.Span;
                var aliasResult = ComposeAlias(AliasContext.Source);
                EnsureAliasSyntax(aliasResult, AliasContext.Source);
                alias = aliasResult.Alias;
                aliasSpan = aliasResult.Span;

                var schemaName = EnsureHashPrefix(columnName);
                if (!string.IsNullOrWhiteSpace(alias))
                    RegisterFromAlias(alias);

                var fromNode = new SchemaFromNode(schemaName, accessMethod.Name, accessMethod.Arguments, alias, _fromPosition);
                if (!aliasSpan.IsEmpty)
                    fromNode.WithSpan(aliasSpan);
                return ParsedSource.Create(fromNode, SourceKind.Schema, sourceStart, sourceEndSpan, aliasResult);
            }

            var properties = new List<string>();
            var anyParsed = false;
            while (Current.TokenType == TokenType.Property)
            {
                anyParsed = true;
                properties.Add(Current.Value);
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
                var sourceEndSpan = Previous?.Span ?? baseNode.Span;
                var aliasResult = ComposeAlias(AliasContext.Source);
                EnsureAliasSyntax(aliasResult, AliasContext.Source);
                alias = aliasResult.Alias;
                aliasSpan = aliasResult.Span;

                if (!string.IsNullOrWhiteSpace(alias))
                    RegisterFromAlias(alias);

                var fromNode = new PropertyFromNode(alias, columnName, properties.ToArray());
                if (!aliasSpan.IsEmpty)
                    fromNode.WithSpan(aliasSpan);
                return ParsedSource.Create(fromNode, SourceKind.Property, sourceStart, sourceEndSpan, aliasResult);
            }

            throw new SyntaxException(
                $"Unrecognized token {Current.TokenType} when parsing FROM clause.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2001_UnexpectedToken,
                Current.Span);
        }

        var inMemoryAlias = ComposeAlias(AliasContext.Source);
        EnsureAliasSyntax(inMemoryAlias, AliasContext.Source);
        alias = inMemoryAlias.Alias;
        aliasSpan = inMemoryAlias.Span;
        RegisterFromAlias(string.IsNullOrWhiteSpace(alias) ? columnName : alias);

        var inMemoryNode = new InMemoryTableFromNode(columnName, alias);
        inMemoryNode.WithSpan(baseNode.Span.Through(aliasSpan));
        return ParsedSource.Create(inMemoryNode, SourceKind.InMemory, sourceStart, baseNode.Span, inMemoryAlias,
            columnName);
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
