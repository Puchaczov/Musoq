using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Syntax;

internal static class EmbeddedSyntax
{
    internal static IReadOnlyList<Diagnostic> Parse(RootNode root, SourceCapture capture, SourceText source)
    {
        var diagnostics = new List<Diagnostic>();
        var statements = (StatementsArrayNode)root.Expression;
        foreach (var command in statements.Statements.Select(s => s.Node).OfType<DiagnosticCommandNode>())
        {
            var syntax = Parser.ParseSyntax(command.InnerQueryText);
            capture.Include(syntax, command.InnerStart);
            diagnostics.AddRange(syntax.Diagnostics.Select(d => d.WithSourceContext(source,
                new TextSpan(command.InnerStart + d.Span.Start, d.Span.Length))));
        }
        return diagnostics.AsReadOnly();
    }
}
