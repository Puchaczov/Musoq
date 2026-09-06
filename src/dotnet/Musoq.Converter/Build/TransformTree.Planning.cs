using System.Runtime.ExceptionServices;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Optimization.Logical;
using Musoq.Evaluator.IR.Planning;
using Musoq.Evaluator.IR.Planning.Printing;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;
using Musoq.Parser.Nodes;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.Optimization;
using PlanningContext = Musoq.Evaluator.IR.Planning.PlanningContext;
using SchemaFromNode = Musoq.Parser.Nodes.From.SchemaFromNode;

namespace Musoq.Converter.Build;

public partial class TransformTree
{
    private static CteExecutionPlan? ComputeCteExecutionPlan(
        RootNode queryTree,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CteExpressionNode? cteExpression = null;

        switch (queryTree.Expression)
        {
            case CteExpressionNode directCte:
                cteExpression = directCte;
                break;
            case StatementsArrayNode statementsArray:
            {
                foreach (var statement in statementsArray.Statements)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (statement.Node is CteExpressionNode nestedCte)
                    {
                        cteExpression = nestedCte;
                        break;
                    }
                }
                break;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        return cteExpression == null
            ? null
            : CteParallelizationAnalyzer.CreatePlan(cteExpression, cancellationToken);
    }

    private static PlanningStageBuildResult? BuildPlans(
        SemanticBuildArtifacts semantic,
        TransformPipelineContext context)
    {
        try
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            var aliasKeyedColumns = CreateAliasKeyedInferredColumns(
                semantic.Phase.Metadata,
                context.CancellationToken);
            var sourcePlanRequests = new Dictionary<SchemaFromNode, SourcePlanRequest>();
            foreach (var pair in semantic.SourcePlanRequestsPerSchema)
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                sourcePlanRequests[pair.Key] = pair.Value with
                {
                    CancellationToken = context.CancellationToken
                };
            }

            var logicalArtifacts = BuildLogicalPlanMeasured(
                semantic.TransformedQueryTree,
                aliasKeyedColumns,
                context);
            if (logicalArtifacts is null)
                return null;
            context.CancellationToken.ThrowIfCancellationRequested();
            var planningScope = semantic.ScopeArtifact.CreateScope();
            var planningContext = new PlanningContext(
                logicalArtifacts,
                context.CompilationOptions,
                context.SchemaProvider,
                semantic.UsedColumns,
                semantic.UsedWhereNodes,
                sourcePlanRequests,
                semantic.PipelineInferredColumns ?? aliasKeyedColumns,
                semantic.PipelineUsedColumns ?? new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase),
                planningScope,
                context.SchemaRegistry,
                ExecutionPlanningShapeResolverAdapter.Create(planningScope, semantic.PipelineInferredColumns ?? aliasKeyedColumns, schemaRegistry: context.SchemaRegistry),
                semantic.CteExecutionPlan)
            {
                CancellationToken = context.CancellationToken,
                SourceContractDiagnosticLocationsBySource = semantic.SourceContractDiagnosticLocationsPerSchema
            };

            var planner = new QueryPlanner();
            var physicalPhase = EvaluatorPerformanceTelemetry.BeginPhase("semantic.physical-plan");
            PlanningResult planningResult;
            try
            {
                planningResult = planner.Plan(planningContext);
            }
            finally
            {
                physicalPhase.Dispose();
            }
            context.CancellationToken.ThrowIfCancellationRequested();
            SourceContractDiagnosticReporter.Report(
                planningResult,
                context.DiagnosticContext);
            SourceOptimizationDiagnosticReporter.Report(
                planningResult,
                context.DiagnosticContext);
            context.CancellationToken.ThrowIfCancellationRequested();

            var updatedContext = context
                .AppendTrace(logicalArtifacts.OptimizerTrace)
                .AppendTrace(planningResult.PhysicalArtifacts.OptimizerTrace);
            var updatedSemantic = semantic with
            {
                UsedWhereNodes = ApplyPlannedWhereNodes(
                    semantic.UsedWhereNodes,
                    planningResult.Properties.SourcePredicatePlansBySourceId,
                    context.CancellationToken)
            };

            string? planningText = null;
            if (context.EmitExecutionPlanText)
            {
                var planningTextPhase = EvaluatorPerformanceTelemetry.BeginPhase("semantic.planning-text");
                try
                {
                    planningText = PlanningTextPrinter.Print(planningResult);
                    context.CancellationToken.ThrowIfCancellationRequested();
                }
                finally
                {
                    planningTextPhase.Dispose();
                }
            }

            var artifacts = new PlanningBuildArtifacts
            {
                InitialLogicalPlan = logicalArtifacts.InitialLogicalPlan,
                OptimizedLogicalPlan = logicalArtifacts.OptimizedLogicalPlan,
                LogicalPlan = logicalArtifacts.OptimizedLogicalPlan,
                PlanningResult = planningResult,
                PlanningText = planningText,
                InitialPhysicalPlan = planningResult.PhysicalArtifacts.InitialPhysicalPlan,
                OptimizedPhysicalPlan = planningResult.PhysicalArtifacts.OptimizedPhysicalPlan,
                PhysicalPlan = planningResult.PhysicalArtifacts.OptimizedPhysicalPlan
            };

            context.CancellationToken.ThrowIfCancellationRequested();
            return new PlanningStageBuildResult(artifacts, updatedSemantic, updatedContext);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SchemaProviderFailureException providerFailure)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            ExceptionDispatchInfo.Capture(providerFailure.InnerException ?? providerFailure).Throw();
            throw new InvalidOperationException("Schema provider failure rethrow did not propagate.");
        }
        catch (Exception ex) when (EvaluatorExceptionTaxonomy.FindSchemaProviderFailure(ex) is not null)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            var providerFailure = EvaluatorExceptionTaxonomy.FindSchemaProviderFailure(ex)!;
            ExceptionDispatchInfo.Capture(providerFailure.InnerException ?? providerFailure).Throw();
            throw new InvalidOperationException("Schema provider failure rethrow did not propagate.");
        }
        catch (Exception ex) when (EvaluatorExceptionTaxonomy.IsExpectedQueryFailure(ex))
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (!context.DiagnosticContext.HasErrors)
                context.DiagnosticContext.ReportException(ex);
            return null;
        }
        catch (Exception ex)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (!context.DiagnosticContext.HasErrors)
                context.DiagnosticContext.ReportException(
                    InternalDiagnosticException.ForCompiler(ex));
            return null;
        }
    }

    private static LogicalPlanningArtifacts? BuildLogicalPlanMeasured(
        RootNode queryTree,
        Dictionary<string, ISchemaColumn[]> aliasKeyedColumns,
        TransformPipelineContext context)
    {
        var phase = EvaluatorPerformanceTelemetry.BeginPhase("semantic.logical-plan");
        try
        {
            var logicalBuilder = new LogicalPlanBuilder(aliasKeyedColumns);
            var logicalTraverser = new LogicalPlanBuildTraverseVisitor(logicalBuilder);
            context.CancellationToken.ThrowIfCancellationRequested();
            queryTree.Accept(logicalTraverser);
            context.CancellationToken.ThrowIfCancellationRequested();

            if (logicalTraverser.Result is null)
                return null;

            var logicalOptimizer = new LogicalOptimizer(
                context.CompilationOptions.UseConstantFolding,
                context.DiagnosticContext);
            context.CancellationToken.ThrowIfCancellationRequested();
            var logicalOptimizationResult = logicalOptimizer.Optimize(logicalTraverser.Result);
            context.CancellationToken.ThrowIfCancellationRequested();
            return new LogicalPlanningArtifacts(
                logicalOptimizationResult.InitialPlan,
                logicalOptimizationResult.OptimizedPlan,
                logicalOptimizationResult.Trace);
        }
        finally
        {
            phase.Dispose();
        }
    }

    private static Dictionary<SchemaFromNode, WhereNode> ApplyPlannedWhereNodes(
        IReadOnlyDictionary<SchemaFromNode, WhereNode> rawWhereNodes,
        IReadOnlyDictionary<string, SourcePredicatePlan> sourcePredicatePlans,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = new Dictionary<SchemaFromNode, WhereNode>(rawWhereNodes.Count);

        foreach (var whereNode in rawWhereNodes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.IsNullOrWhiteSpace(whereNode.Key.Id) &&
                sourcePredicatePlans.TryGetValue(whereNode.Key.Id, out var predicatePlan))
            {
                result[whereNode.Key] = predicatePlan.PushedWhereNode;
                continue;
            }

            result[whereNode.Key] = whereNode.Value;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }
}
