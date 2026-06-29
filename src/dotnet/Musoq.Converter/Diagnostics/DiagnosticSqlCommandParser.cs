using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Converter.Diagnostics;

public static class DiagnosticSqlCommandParser
{
    public static bool TryParse(
        string script,
        out DiagnosticSqlCommand? command,
        out IReadOnlyList<Diagnostic>? diagnostics)
    {
        command = null;
        diagnostics = null;

        var lexer = new Lexer(script, true, recoverOnError: true);
        var parser = new global::Musoq.Parser.Parser(lexer, lexer.Diagnostics, enableRecovery: true);
        var parseResult = parser.ParseWithDiagnostics();

        if (TryFindCommand(parseResult.Root, out var commandNode))
        {
            if (parseResult.HasErrors)
            {
                diagnostics = parseResult.Errors.ToArray();
                return true;
            }

            var source = parseResult.SourceText.Text;
            command = new DiagnosticSqlCommand(
                MapKind(commandNode.Kind),
                source[..commandNode.CommandStart] + source[commandNode.InnerStart..]);
            return true;
        }

        var commandDiagnostics = parseResult.Errors
            .Where(IsDiagnosticCommandDiagnostic)
            .ToArray();

        if (commandDiagnostics.Length == 0)
            return false;

        diagnostics = commandDiagnostics;
        return true;
    }

    private static bool TryFindCommand(RootNode? root, out DiagnosticCommandNode command)
    {
        command = null!;

        if (root?.Expression is not StatementsArrayNode statements)
            return false;

        foreach (var statement in statements.Statements)
        {
            if (statement.Node is ParameterBlockNode)
                continue;

            if (statement.Node is DiagnosticCommandNode diagnosticCommand)
            {
                command = diagnosticCommand;
                return true;
            }

            return false;
        }

        return false;
    }

    private static DiagnosticSqlCommandKind MapKind(DiagnosticCommandKind kind)
    {
        return kind switch
        {
            DiagnosticCommandKind.Profile => DiagnosticSqlCommandKind.Profile,
            DiagnosticCommandKind.ExplainAnalyze => DiagnosticSqlCommandKind.ExplainAnalyze,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }

    private static bool IsDiagnosticCommandDiagnostic(Diagnostic diagnostic)
    {
        return diagnostic.Message.Contains("Diagnostic command", StringComparison.Ordinal) ||
               diagnostic.Message.Contains("EXPLAIN without ANALYZE", StringComparison.Ordinal) ||
               diagnostic.Message.Contains("Standalone ANALYZE", StringComparison.Ordinal);
    }
}
