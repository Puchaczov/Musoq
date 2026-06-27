using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private bool IsDiagnosticCommandStart()
    {
        return IsContextWord("profile") ||
               IsContextWord("explain") ||
               IsContextWord("analyze");
    }

    private bool IsContextWord(string value)
    {
        return Current.TokenType is TokenType.Identifier or TokenType.Word or TokenType.Function &&
               Current.Value.Equals(value, StringComparison.OrdinalIgnoreCase);
    }

    private DiagnosticCommandNode ComposeDiagnosticCommand()
    {
        if (IsContextWord("profile"))
            return ComposeProfileCommand();

        if (IsContextWord("explain"))
            return ComposeExplainAnalyzeCommand();

        throw new SyntaxException(
            "Standalone ANALYZE is not implemented. Use EXPLAIN ANALYZE <query>.",
            _lexer.AlreadyResolvedQueryPart,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            Current.Span);
    }

    private DiagnosticCommandNode ComposeProfileCommand()
    {
        var commandToken = ConsumeAndGetToken();
        return ComposeDiagnosticCommandBody(DiagnosticCommandKind.Profile, commandToken.Span.Start, commandToken.Span);
    }

    private DiagnosticCommandNode ComposeExplainAnalyzeCommand()
    {
        var explainToken = ConsumeAndGetToken();

        if (!IsContextWord("analyze"))
        {
            throw new SyntaxException(
                "EXPLAIN without ANALYZE is not supported. Use EXPLAIN ANALYZE <query>.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2030_UnsupportedSyntax,
                Current.Span.IsEmpty ? explainToken.Span : Current.Span);
        }

        var analyzeToken = ConsumeAndGetToken();
        return ComposeDiagnosticCommandBody(
            DiagnosticCommandKind.ExplainAnalyze,
            explainToken.Span.Start,
            explainToken.Span.Through(analyzeToken.Span));
    }

    private DiagnosticCommandNode ComposeDiagnosticCommandBody(
        DiagnosticCommandKind kind,
        int commandStart,
        TextSpan commandSpan)
    {
        EnsureDiagnosticInnerQueryStart();

        var innerStart = Current.Span.Start;
        var innerEnd = ConsumeDiagnosticInnerStatement();
        var span = commandSpan.Through(new TextSpan(innerStart, Math.Max(0, innerEnd - innerStart)));
        var innerQueryText = innerEnd > innerStart
            ? _lexer.Input[innerStart..innerEnd]
            : string.Empty;

        return new DiagnosticCommandNode(kind, commandStart, innerStart, innerEnd, innerQueryText, span);
    }

    private void EnsureDiagnosticInnerQueryStart()
    {
        if (Current.TokenType is TokenType.EndOfFile or TokenType.Semicolon)
        {
            throw new SyntaxException(
                "Diagnostic command requires an inner SELECT, FROM, WITH, PIVOT, or UNPIVOT query.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2030_UnsupportedSyntax,
                Current.Span);
        }

        if (Current.TokenType is TokenType.Select or TokenType.From or TokenType.With or TokenType.Pivot or TokenType.Unpivot)
            return;

        var actual = string.IsNullOrWhiteSpace(Current.Value)
            ? Current.TokenType.ToString()
            : Current.Value;

        throw new SyntaxException(
            $"Diagnostic command does not support inner query starting with '{actual}'. Expected SELECT, FROM, WITH, PIVOT, or UNPIVOT.",
            _lexer.AlreadyResolvedQueryPart,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            Current.Span);
    }

    private int ConsumeDiagnosticInnerStatement()
    {
        var innerEnd = Current.Span.Start;

        while (Current.TokenType is not TokenType.EndOfFile and not TokenType.Semicolon)
        {
            innerEnd = Current.Span.End;
            Consume(Current.TokenType);
        }

        return innerEnd;
    }
}
