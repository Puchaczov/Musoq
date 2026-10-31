using System.Text;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static string ResolveInterpretMethodName(InterpretSourceKind kind)
    {
        return kind switch
        {
            InterpretSourceKind.Interpret => "Interpret",
            InterpretSourceKind.InterpretAt => "InterpretAt",
            InterpretSourceKind.Parse => "Parse",
            InterpretSourceKind.TryInterpret => "Interpret",
            InterpretSourceKind.TryParse => "Parse",
            InterpretSourceKind.PartialInterpret => "PartialInterpret",
            InterpretSourceKind.PartialParse => "PartialParse",
            _ => throw UnsupportedShape.Of($"Interpret source kind {kind}")
        };
    }

    private static string CreateInterpreterVariableName(string schemaName)
    {
        var builder = new StringBuilder(schemaName.Length);

        foreach (var character in schemaName)
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');

        if (builder.Length == 0 || (!char.IsLetter(builder[0]) && builder[0] != '_'))
            builder.Insert(0, '_');

        return $"_interpreter_{builder}";
    }

    private static string CreateSourceScanLocalName(ExecutionSourceScan sourceScan)
    {
        const string rowsSuffix = "Rows";

        if (sourceScan.Rows.Name.EndsWith(rowsSuffix, StringComparison.Ordinal))
            return sourceScan.Rows.Name[..^rowsSuffix.Length];

        return sourceScan.Rows.Name;
    }
}
