using System.Linq;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Syntax;

namespace Musoq.Parser;

public partial class Parser
{
    private readonly SourceCapture? _sourceCapture;

    internal Parser(ILexer lexer, SourceCapture capture) : this(lexer)
    {
        _sourceCapture = capture;
    }

    /// <summary>Parses original syntax and trivia using the shared grammar without semantic normalization loss.</summary>
    public static SyntaxParseResult ParseSyntax(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var sourceText = new SourceText(source);
        var capture = new SourceCapture(sourceText);
        if (string.IsNullOrWhiteSpace(source))
            return new SyntaxParseResult(sourceText, capture.Root(), capture.Tokens(), []);
        var lexer = new Lexer(source, true, false, capture.Observe);
        try
        {
            var ast = new Parser(lexer, capture).ComposeAllCore();
            var embedded = EmbeddedSyntax.Parse(ast, capture, sourceText);
            var diagnostics = lexer.Diagnostics.Concat(embedded).ToArray();
            SyntaxNode? root = null;
            if (!diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
                root = capture.Root();
            return new SyntaxParseResult(sourceText, root, capture.Tokens(), diagnostics);
        }
        catch (Exception exception) when (exception is IDiagnosticException)
        {
            var diagnostic = exception.ToDiagnosticOrGeneric(sourceText);
            return new SyntaxParseResult(sourceText, null, capture.Tokens(), lexer.Diagnostics.Append(diagnostic));
        }
    }

}
