using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Musoq.Evaluator.IR.Planning.OptimizationDiagnostics;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes.From;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Exceptions;

namespace Musoq.Evaluator.IR.Planning.SourcePlanning;

internal static partial class SourcePlanningPlanner
{
    private static (SourcePlanResult Result, SourceDescriptor Descriptor) PlanSource(
        PlanningContext context,
        SchemaScanNode scan,
        SchemaFromNode? sourceNode,
        SourcePlanRequest request)
    {
        var schema = SchemaProviderBoundary.Invoke(() => context.SchemaProvider.GetSchema(scan.SchemaName));
        var semanticSource = sourceNode as Musoq.Evaluator.Parser.SchemaFromNode;
        var parameters = semanticSource is { StaticMetadataArguments.Length: > 0 } or
                         { HasRequiredRuntimeArguments: true }
            ? semanticSource.StaticMetadataArguments
            : semanticSource != null
                ? SchemaArgumentBinder.BindStaticArguments(semanticSource.Parameters, invocation: semanticSource.BoundInvocation)
                : sourceNode != null
                    ? SchemaArgumentBinder.BindStaticArguments(sourceNode.Parameters)
                    : [];
        var metadataContext = new SourceMetadataContext(
            request.Identity.SourceContextId,
            CancellationToken.None,
            ResolveColumns(context, scan),
            request.SourceRuntimeSettings,
            NullLogger.Instance);

        try
        {
            var descriptor = SchemaProviderBoundary.Invoke(() => schema.DescribeSource(
                scan.MethodName,
                new SourceDescribeContext(request.Identity, metadataContext),
                parameters));
            EnumSourceDescriptorContractValidator.Validate(
                metadataContext.AllColumns,
                descriptor,
                columnName => ResolveColumnSpan(context, sourceNode, columnName));
            var result = SchemaProviderBoundary.Invoke(() => schema.TryPlanSource(scan.MethodName, request, parameters))
                         ?? SourcePlanResult.RejectAll(request);
            SourcePredicatePlanContractValidator.Validate(
                request,
                result,
                ResolveSourceSpan(sourceNode));
            result = OptimizationDiagnosticOriginMarker.Mark(result, "TryPlanSource");
            result = SourceContractDiagnosticOriginMarker.Mark(result, "TryPlanSource");
            result = OptimizationDiagnosticOriginMarker.Prepend(result, descriptor.Diagnostics, "DescribeSource");
            result = SourceContractDiagnosticOriginMarker.Prepend(result, descriptor.ContractDiagnostics, "DescribeSource");
            return (result, descriptor);
        }
        catch (SchemaProviderFailureException exception) when (semanticSource?.HasRequiredRuntimeArguments == true)
        {
            throw CreateMetadataDefaultException(scan, semanticSource, exception);
        }
        catch (SchemaArgumentException exception) when (
            semanticSource?.HasRequiredRuntimeArguments == true &&
            !string.Equals(exception.ParamName, "methodName", StringComparison.Ordinal))
        {
            throw CreateMetadataDefaultException(scan, semanticSource, exception);
        }
    }

}
