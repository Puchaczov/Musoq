using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Parser;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.IR.Planning.SourcePlanning;

internal static partial class SourcePlanningPlanner
{
    private static TextSpan ResolveColumnSpan(
        PlanningContext context,
        SchemaFromNode? sourceNode,
        string columnName)
    {
        if (sourceNode != null &&
            context.SourceContractDiagnosticLocationsBySource.TryGetValue(sourceNode, out var locations) &&
            locations.TryGetColumnSpan(columnName, out var span))
        {
            return span;
        }

        return ResolveSourceSpan(sourceNode);
    }

    private static TextSpan ResolveSourceSpan(SchemaFromNode? sourceNode)
    {
        return sourceNode is Musoq.Evaluator.Parser.SchemaFromNode { HasSpan: true } semantic
            ? semantic.Span
            : TextSpan.Empty;
    }

    private static SourceMetadataRequiresDefaultException CreateMetadataDefaultException(
        SchemaScanNode scan,
        Musoq.Evaluator.Parser.SchemaFromNode sourceNode,
        Exception innerException)
    {
        return new SourceMetadataRequiresDefaultException(
            scan.SchemaName,
            scan.MethodName,
            sourceNode.HasSpan ? sourceNode.Span : TextSpan.Empty,
            innerException);
    }
}
