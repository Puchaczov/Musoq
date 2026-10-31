using System.Linq;
using Musoq.Formatter.Layout;
using Musoq.Formatter.Syntax;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Syntax;
using Musoq.Parser.Tokens;

namespace Musoq.Formatter;

/// <summary>Formats complete raw Musoq scripts using general source-preserving parser capabilities.</summary>
public static class MusoqFormatter
{
    /// <summary>Formats an entire raw script without loading providers or executing queries.</summary>
    /// <param name="source">Complete script text, including declarations and comments.</param>
    /// <param name="options">Immutable options, or null to select defaults.</param>
    /// <returns>Complete output or diagnostics without partial output.</returns>
    /// <exception cref="ArgumentNullException">Source is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An option has an unsupported value.</exception>
    public static FormattingResult Format(string source, FormattingOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        options ??= new FormattingOptions();
        options.Validate();
        var syntax = Parser.Parser.ParseSyntax(source);
        if (!syntax.Success)
            return new FormattingResult(null, syntax.Diagnostics);
        var significant = syntax.Tokens.Where(t => t.Role != SyntaxTokenRole.Whitespace).ToArray();
        if (significant.Length == 0)
            return new FormattingResult(string.Empty, syntax.Diagnostics);
        var unsupported = significant.FirstOrDefault(t => !Supports(t));
        if (unsupported != null)
            return Unsupported(syntax, unsupported);
        var newline = Newline(syntax, options.LineEnding);
        var elements = LayoutTree.Build(syntax.Tokens);
        Document document;
        if (significant.All(t => t.Role == SyntaxTokenRole.Comment))
            document = Document.Join(significant.SelectMany(t => new[] { Document.SourceText(t.Text), Document.Line }));
        else
            document = new ScriptLayout(new LayoutContext(new SyntaxIndex(syntax), new TokenDocuments(options), source)).Build(elements);
        var output = new DocumentRenderer(options, newline).Render(document);
        if (!Parser.Parser.ParseSyntax(output).Success)
            return Unsupported(syntax, significant[0], "The generated layout did not preserve valid syntax.");
        return new FormattingResult(output, syntax.Diagnostics);
    }

    private static bool Supports(SyntaxToken token) => token.Role != SyntaxTokenRole.Unknown;

    private static FormattingResult Unsupported(SyntaxParseResult syntax, SyntaxToken token,
        string message = "This syntax does not yet have a formatting rule.")
    {
        var locations = syntax.SourceText.GetLocations(token.Span);
        var diagnostic = new Diagnostic(DiagnosticCode.MQ9001_InternalCompilerError, DiagnosticSeverity.Error,
            message, locations.Start, locations.End,
            phase: DiagnosticPhase.Internal, sourceKind: DiagnosticSourceKind.Query);
        return new FormattingResult(null, syntax.Diagnostics.Append(diagnostic));
    }

    private static string Newline(SyntaxParseResult syntax, LineEnding ending)
    {
        if (ending == LineEnding.Lf)
            return "\n";
        if (ending == LineEnding.CrLf)
            return "\r\n";
        foreach (var token in syntax.Tokens.Where(t => t.Role is SyntaxTokenRole.Whitespace or SyntaxTokenRole.Keyword))
        {
            var offset = token.Text.IndexOf('\n');
            if (offset >= 0)
                return offset > 0 && token.Text[offset - 1] == '\r' ? "\r\n" : "\n";
        }
        return "\n";
    }
}
