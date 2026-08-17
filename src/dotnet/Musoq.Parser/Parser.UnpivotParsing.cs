using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private QueryNode ComposeUnpivotQuery()
    {
        PushFromAliasesScope();
        try
        {
            _fromPosition += 1;
            Consume(TokenType.Unpivot);
            var source = ComposeSource(SourceParseContext.Primary);
            var sourceExpression = ComposeJoinOrApply(source);
            var nameColumn = ComposeUnpivotOnColumn();
            var entries = ComposeUnpivotEntries();
            ConsumeUnpivotUsingKeyword();
            var valueColumn = ComposeUnpivotOutputColumnName("UNPIVOT USING requires a value output column name.");
            var keepFields = ComposeUnpivotKeepFields();
            ValidateUnpivotOutputNames(nameColumn.Name, valueColumn.Name, entries, keepFields, nameColumn.Span, valueColumn.Span);
            var orderBy = ComposeOrderBy();
            var skip = ComposeSkip();
            var take = ComposeTake();
            var unpivot = new UnpivotFromNode(sourceExpression, nameColumn.Name, valueColumn.Name, entries, keepFields);
            var select = new SelectNode([new FieldNode(new AllColumnsNode(), 0, null)]);
            return new QueryNode(select, new ExpressionFromNode(unpivot), null, null, orderBy, skip, take, null, null, default);
        }
        finally
        {
            PopFromAliasesScope();
        }
    }

    private SyntaxException UnpivotSyntax(string message, DiagnosticCode code, TextSpan span)
        => new(message, _lexer.AlreadyResolvedQueryPart, code, span);

    private (string Name, TextSpan Span) ComposeUnpivotOnColumn()
    {
        if (Current.TokenType != TokenType.On)
            throw UnpivotSyntax(
                "UNPIVOT requires an ON clause after the source. Example: unpivot #sales.wide() on Quarter in (Q1 as Q1) using Sales.",
                DiagnosticCode.MQ2002_MissingToken,
                Current.Span);

        Consume(TokenType.On);
        var nameColumn = ComposeUnpivotOutputColumnName("UNPIVOT ON requires the generated name column before IN.");

        if (Current.TokenType != TokenType.In)
            throw UnpivotSyntax(
                "UNPIVOT ON requires an IN (...) list of value expressions.",
                DiagnosticCode.MQ2002_MissingToken,
                Current.Span);

        Consume(TokenType.In);
        return nameColumn;
    }

    private UnpivotEntryNode[] ComposeUnpivotEntries()
    {
        Consume(TokenType.LeftParenthesis);
        var entries = new List<UnpivotEntryNode>();

        if (Current.TokenType == TokenType.RightParenthesis)
            throw UnpivotSyntax(
                "UNPIVOT IN requires at least one value expression.",
                DiagnosticCode.MQ2003_InvalidExpression,
                Current.Span);

        do
        {
            var expression = ComposeOperations();
            var aliasResult = ComposeAlias(AliasContext.Unpivot);
            EnsureAliasSyntax(aliasResult, AliasContext.Unpivot);
            var alias = aliasResult.Alias;
            var aliasSpan = aliasResult.Span;

            if (string.IsNullOrWhiteSpace(alias) && !TryCreateUnpivotStableAlias(expression, out alias))
                throw UnpivotSyntax(
                    "UNPIVOT IN entries require an alias unless the value expression is a simple identifier or property access.",
                    DiagnosticCode.MQ2022_InvalidAlias,
                    expression.Span);

            entries.Add(new UnpivotEntryNode(expression, alias, aliasSpan));

            if (Current.TokenType != TokenType.Comma)
                break;

            Consume(TokenType.Comma);
            if (Current.TokenType == TokenType.RightParenthesis)
                throw UnpivotSyntax(
                    "UNPIVOT IN list has a trailing comma. Add another value expression or remove the comma.",
                    DiagnosticCode.MQ2014_TrailingComma,
                    Current.Span);
        } while (true);

        Consume(TokenType.RightParenthesis);
        return entries.ToArray();
    }

    private void ConsumeUnpivotUsingKeyword()
    {
        if (!IsContextualKeyword("using"))
            throw UnpivotSyntax(
                "UNPIVOT requires a USING clause with the generated value column name. Example: using Sales.",
                DiagnosticCode.MQ2002_MissingToken,
                Current.Span);

        Consume(Current.TokenType);
    }

    private FieldNode[] ComposeUnpivotKeepFields()
    {
        if (!IsContextualKeyword("keep"))
            return [];

        Consume(Current.TokenType);
        var fields = new List<FieldNode>();
        var order = 0;

        while (!IsUnpivotClauseTerminator(Current.TokenType))
        {
            var field = ConsumeField(order++);
            if (!field.HasExplicitFieldName && TryCreateUnpivotStableAlias(field.Expression, out var alias))
            {
                field = new FieldNode(field.Expression, field.FieldOrder, alias, true, field.Span);
            }
            else if (!field.HasExplicitFieldName)
            {
                throw UnpivotSyntax(
                    "UNPIVOT KEEP expressions require an alias unless the expression is a simple identifier or property access.",
                    DiagnosticCode.MQ2022_InvalidAlias,
                    field.Expression.Span);
            }

            fields.Add(field);

            if (Current.TokenType != TokenType.Comma)
                break;

            Consume(TokenType.Comma);
            if (IsUnpivotClauseTerminator(Current.TokenType))
                throw UnpivotSyntax(
                    "UNPIVOT KEEP list has a trailing comma. Add another keep expression or remove the comma.",
                    DiagnosticCode.MQ2014_TrailingComma,
                    Current.Span);
        }

        if (fields.Count == 0)
            throw UnpivotSyntax(
                "UNPIVOT KEEP requires at least one expression.",
                DiagnosticCode.MQ2003_InvalidExpression,
                Current.Span);

        return fields.ToArray();
    }

    private (string Name, TextSpan Span) ComposeUnpivotOutputColumnName(string message)
    {
        if (Current.TokenType is not (TokenType.Identifier or TokenType.Word))
            throw UnpivotSyntax(message, DiagnosticCode.MQ2002_MissingToken, Current.Span);

        var token = ConsumeAndGetToken(Current.TokenType);
        if (string.IsNullOrWhiteSpace(token.Value))
            throw UnpivotSyntax(
                "UNPIVOT output column names cannot be empty.",
                DiagnosticCode.MQ2022_InvalidAlias,
                token.Span);

        return (token.Value, token.Span);
    }

    private void ValidateUnpivotOutputNames(
        string nameColumn,
        string valueColumn,
        IReadOnlyList<UnpivotEntryNode> entries,
        IReadOnlyList<FieldNode> keepFields,
        TextSpan nameColumnSpan,
        TextSpan valueColumnSpan)
    {
        var outputNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var keepField in keepFields)
            AddUnpivotOutputName(outputNames, keepField.FieldName, keepField.Span);

        AddUnpivotOutputName(outputNames, nameColumn, nameColumnSpan);
        AddUnpivotOutputName(outputNames, valueColumn, valueColumnSpan);

        var entryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
            if (!entryNames.Add(entry.NameValue))
                throw UnpivotSyntax(
                    $"UNPIVOT IN generated duplicate name value '{entry.NameValue}'. Use unique aliases in the IN list.",
                    DiagnosticCode.MQ2008_DuplicateAlias,
                    entry.NameValueSpan.IsEmpty ? entry.Expression.Span : entry.NameValueSpan);
    }

    private void AddUnpivotOutputName(HashSet<string> outputNames, string outputName, TextSpan span)
    {
        if (outputNames.Add(outputName))
            return;

        throw UnpivotSyntax(
            $"UNPIVOT generated duplicate output column name '{outputName}'. Use unique keep, name, and value column aliases.",
            DiagnosticCode.MQ2008_DuplicateAlias,
            span);
    }

    private static bool IsUnpivotClauseTerminator(TokenType tokenType)
    {
        return tokenType is TokenType.OrderBy or TokenType.Skip or TokenType.Take
            or TokenType.RightParenthesis or TokenType.Semicolon or TokenType.EndOfFile
            or TokenType.Union or TokenType.UnionAll or TokenType.Except or TokenType.Intersect;
    }

    private static bool TryCreateUnpivotStableAlias(Node expression, out string alias)
    {
        switch (expression)
        {
            case IdentifierNode identifier:
                alias = identifier.Name;
                return !string.IsNullOrWhiteSpace(alias);
            case DotNode { Expression: IdentifierNode identifier }:
                alias = identifier.Name;
                return !string.IsNullOrWhiteSpace(alias);
            default:
                alias = string.Empty;
                return false;
        }
    }
}
