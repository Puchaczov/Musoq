using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private void RecordIncompleteStatementIfNeeded()
    {
        if (!HasRecoveredRawStringDiagnosticAtCurrent())
            RecordError(DiagnosticCode.MQ2016_IncompleteStatement,
                "Failed to compose statement. The SQL query structure is invalid.", Current.Span);
    }

    private void RecordSyntaxExceptionIfNeeded(SyntaxException exception)
    {
        if (!HasRecoveredRawStringDiagnosticAtCurrent())
            RecordSyntaxException(exception);
    }

    private ParseResult CreateParseResult(RootNode? root, SourceText sourceText, DiagnosticBag diagnostics)
    {
        MergeLexerDiagnostics(diagnostics);
        return new ParseResult(root, sourceText, diagnostics.ToSortedList());
    }

    private ParseResult CreateFailedParseResult(SourceText sourceText, DiagnosticBag diagnostics)
    {
        MergeLexerDiagnostics(diagnostics);
        return ParseResult.Failed(sourceText, diagnostics.ToSortedList());
    }

    private bool HasRecoveredRawStringDiagnosticAtCurrent()
    {
        if (Current.TokenType != TokenType.Semicolon)
            return false;

        foreach (var diagnostic in _lexer.Diagnostics)
        {
            if (diagnostic.Code == DiagnosticCode.MQ1002_UnterminatedString &&
                diagnostic.Span.End <= Current.Span.Start)
                return true;
        }

        return false;
    }

    private void MergeLexerDiagnostics(DiagnosticBag destination)
    {
        if (ReferenceEquals(destination, _lexer.Diagnostics))
            return;

        destination.AddRange(_lexer.Diagnostics);
    }
}
