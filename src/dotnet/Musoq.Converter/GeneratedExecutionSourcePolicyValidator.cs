using System.Linq;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Converter.Build;

public partial class TransformTree
{
    private static bool ValidateGeneratedExecutionSourceContracts(
        TransformPipelineContext context,
        RootNode queryTree,
        SemanticScopeArtifact scopeArtifact,
        SemanticMetadataSnapshot semanticMetadata,
        SemanticBuildArtifacts semanticArtifacts)
    {
        if (IsDescriptionQuery(queryTree))
            return true;

        foreach (var violation in ExecutionSourceCodeGenerationPolicy.FindViolations(
                     scopeArtifact.CreateScope(),
                     semanticMetadata.InferredColumns.Keys,
                     semanticArtifacts.UsedColumns))
        {
            var member = string.IsNullOrWhiteSpace(violation.MemberPath)
                ? "<source>"
                : violation.MemberPath;
            var message = ErrorCatalog.GetMessage(
                DiagnosticCode.MQ3084_SourceEntityRequiresRuntimeReflection,
                violation.EntityType.FullName ?? violation.EntityType.Name,
                violation.Source.Schema,
                violation.Source.Method,
                $"member '{member}' {violation.Reason}. Use a public CLR contract or a supported string/object dictionary row.");
            context.DiagnosticContext.ReportError(
                DiagnosticCode.MQ3084_SourceEntityRequiresRuntimeReflection,
                message,
                violation.Source.SpanOrEmpty());
        }

        return !context.DiagnosticContext.HasErrors;
    }

    private static bool IsDescriptionQuery(RootNode root)
    {
        return root.Expression switch
        {
            DescNode => true,
            StatementsArrayNode statements => statements.Statements.Any(statement => statement.Node is DescNode),
            _ => false
        };
    }
}
