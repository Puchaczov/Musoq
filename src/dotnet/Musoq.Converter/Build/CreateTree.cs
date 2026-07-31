using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Converter.Build;

public class CreateTree(BuildChain successor) : BuildChain(successor)
{
    public override void Build(BuildItems items)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items), "BuildItems cannot be null when creating AST tree.");

        if (string.IsNullOrWhiteSpace(items.RawQuery))
            throw AstValidationException.ForInvalidNodeStructure("Query", "CreateTree", "RawQuery is null or empty");

        var phase = global::Musoq.Converter.EvaluatorPerformanceTelemetry.BeginPhase("parse");
        try
        {
            var script = items.RawQuery;
            var rootNode = ParsedQueryTemplateCache.GetOrAdd(
                script,
                ParsedQueryTemplateCache.DefaultParserContract,
                () => Parse(script));

            if (rootNode == null)
                throw AstValidationException.ForNullNode("RootNode", "CreateTree after parsing");

            items.RawQueryTree = rootNode;
            items.SourceText = new SourceText(items.RawQuery);
        }
        catch (Exception ex) when (ex is not AstValidationException)
        {
            throw new AstValidationException("Query", "CreateTree", $"Failed to parse SQL query: {ex.Message}", ex);
        }
        finally
        {
            phase.Dispose();
        }

        Successor?.Build(items);
    }

    private static RootNode Parse(string script)
    {
        var lexer = new Lexer(script, true);
        var parser = new Parser.Parser(lexer);
        return parser.ComposeAll();
    }
}
