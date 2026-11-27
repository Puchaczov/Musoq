using System.Text;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private string GenerateSchemaReferenceReadCode(string localVar, string fieldName, SchemaReferenceTypeNode schemaRef)
    {
        var typeName = schemaRef.FullTypeName;
        var tempInterpreter = $"_{localVar}_interpreter";
        var builder = new StringBuilder();
        builder.AppendLine(
            System.Globalization.CultureInfo.InvariantCulture,
            $"var {tempInterpreter} = {CreateNestedInterpreter(typeName)};"
        );
        builder.Append(
            StatusRead(localVar, $"base.InterpretNestedStatus({tempInterpreter}, data, \"{EscapeString(fieldName)}\")")
        );
        return builder.ToString();
    }

    private string GenerateAlignmentCode(AlignmentNode alignmentNode)
    {
        var bits = alignmentNode.AlignmentBits;
        return StatusCheck($"base.AlignWithStatus({bits})");
    }

    private ConstructionNotYetSupported CreateUnsupportedCodeGenerationException(
        string fieldName,
        TypeAnnotationNode unsupportedType,
        string role
    )
    {
        return new ConstructionNotYetSupported(
            $"Unsupported interpretation schema code generation for schema '{_currentSchemaName}', field '{fieldName}', {role} '{unsupportedType.GetType().Name}'.",
            DiagnosticCode.MQ4016_UnsupportedSchemaConstruction
        );
    }
}
