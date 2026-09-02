using Musoq.Converter.Exceptions;
using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;
using System.Collections.Immutable;

namespace Musoq.Converter.Build;

public class CreateTree(BuildChain successor) : BuildChain(successor)
{
    public override void Build(BuildItems items)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items), "BuildItems cannot be null when creating AST tree.");

        if (string.IsNullOrWhiteSpace(items.RawQuery))
            throw AstValidationException.ForInvalidNodeStructure("Query", "CreateTree", "RawQuery is null or empty");

        var phase = EvaluatorPerformanceTelemetry.BeginPhase("parse");
        try
        {
            var script = items.RawQuery;
            var parsedTemplate = ParsedQueryTemplateCache.GetOrAddWithDiagnostics(script,
                ParsedQueryTemplateCache.DefaultParserContract, () => Parse(script));

            if (parsedTemplate.Root == null)
                throw AstValidationException.ForNullNode("RootNode", "CreateTree after parsing");

            items.RawQueryTree = parsedTemplate.Root;
            items.SourceText = new SourceText(items.RawQuery);
            items.DiagnosticContext.AddRange(parsedTemplate.Diagnostics);

            if (parsedTemplate.Diagnostics.IsEmpty &&
                parsedTemplate.Root.Expression is StatementsArrayNode statements &&
                !statements.Statements.Any(static statement => IsExecutableStatement(statement.Node)))
            {
                var span = statements.Statements.Length > 0
                    ? statements.Statements[^1].Node.SpanOrEmpty()
                    : TextSpan.Empty;
                throw new SyntaxException(
                    "The query contains declarations but no executable statement. Add a SELECT, FROM, WITH, DESC, or another result-producing statement after the declarations.",
                    script,
                    DiagnosticCode.MQ2016_IncompleteStatement,
                    span);
            }
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

    private static ParsedQueryTemplate Parse(string script)
    {
        var lexer = new Lexer(script, true);
        var parser = new Parser.Parser(lexer);
        return new ParsedQueryTemplate(parser.ComposeAll(), lexer.Diagnostics.ToImmutableArray());
    }

    private static bool IsExecutableStatement(Node node)
    {
        return node is not (
            ParameterBlockNode
            or ScriptVariableDeclarationNode
            or EnumDeclarationNode
            or CreateTableNode
            or CoupleNode
            or BinarySchemaNode
            or TextSchemaNode);
    }
}
