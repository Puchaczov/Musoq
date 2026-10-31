using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Diagnostics;

namespace Musoq.Parser.Syntax;

/// <summary>A general source-preserving parse result with original lexical text and grammar regions.</summary>
public sealed class SyntaxParseResult
{
    internal SyntaxParseResult(SourceText sourceText, SyntaxNode? root, IEnumerable<SyntaxToken> tokens,
        IEnumerable<Diagnostic> diagnostics)
    {
        SourceText = sourceText;
        Root = root;
        Tokens = Array.AsReadOnly(tokens.ToArray());
        Diagnostics = Array.AsReadOnly(diagnostics.OrderBy(d => d.Location.Offset).ToArray());
    }

    public SourceText SourceText { get; }
    public SyntaxNode? Root { get; }
    public IReadOnlyList<SyntaxToken> Tokens { get; }
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
    public bool Success => Root != null && !Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
}
