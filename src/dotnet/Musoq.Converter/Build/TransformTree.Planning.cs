using System.Runtime.ExceptionServices;
using System.Collections.Generic;
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
using PlanningContext = Musoq.Evaluator.IR.Planning.PlanningContext;
using SchemaFromNode = Musoq.Parser.Nodes.From.SchemaFromNode;

namespace Musoq.Converter.Build;

public partial class TransformTree
{
    private static CteExecutionPlan? ComputeCteExecutionPlan(RootNode queryTree)
    {
        CteExpressionNode? cteExpression = null;

        switch (queryTree.Expression)
        {
            case CteExpressionNode directCte:
                cteExpression = directCte;
                break;
            case StatementsArrayNode statementsArray:
            {
                foreach (var statement in statementsArray.Statements)
                    if (statement.Node is CteExpressionNode nestedCte)
                    {
                        cteExpression = nestedCte;
                        break;
                    }

                break;
            }
        }

        return cteExpression == null ? null : CteParallelizationAnalyzer.CreatePlan(cteExpression);
    }

    private static PlanningStageBuildResult? BuildPlans(
        SemanticBuildArtifacts semantic,
        TransformPipelineContext context)
    {
        try
        {
            var aliasKeyedColumns = CreateAliasKeyedInferredColumns(semantic.Phase.Metadata);
            var logicalArtifacts = BuildLogicalPlanMeasured(
                semantic.TransformedQueryTree,
                aliasKeyedColumns,
                context);
            if (logicalArtifacts is null)
                return null;
            var planningScope = semantic.ScopeArtifact.CreateScope();
            var planningContext = new PlanningContext(
                logicalArtifacts,
                context.CompilationOptions,
                context.SchemaProvider,
                semantic.UsedColumns,
                semantic.UsedWhereNodes,
                semantic.SourcePlanRequestsPerSchema,
                semantic.PipelineInferredColumns ?? aliasKeyedColumns,
                semantic.PipelineUsedColumns ?? new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase),
                planningScope,
                context.SchemaRegistry,
                ExecutionPlanningShapeResolverAdapter.Create(planningScope, semantic.PipelineInferredColumns ?? aliasKeyedColumns, schemaRegistry: context.SchemaRegistry),
                semantic.CteExecutionPlan)
            {
                SourceContractDiagnosticLocationsBySource = semantic.SourceContractDiagnosticLocationsPerSchema
            };

            var planner = new QueryPlanner();
            var physicalPhase = global::Musoq.Converter.EvaluatorPerformanceTelemetry.BeginPhase("semantic.physical-plan");
            PlanningResult planningResult;
            try
            {
                planningResult = planner.Plan(planningContext);
            }
            finally
            {
                physicalPhase.Dispose();
            }
            SourceContractDiagnosticReporter.Report(
                planningResult,
                context.DiagnosticContext);
            SourceOptimizationDiagnosticReporter.Report(
                planningResult,
                context.DiagnosticContext);

            var updatedContext = context
                .AppendTrace(logicalArtifacts.OptimizerTrace)
                .AppendTrace(planningResult.PhysicalArtifacts.OptimizerTrace);
            var updatedSemantic = semantic with
            {
                UsedWhereNodes = ApplyPlannedWhereNodes(semantic.UsedWhereNodes, planningResult.Properties.SourcePredicatePlansBySourceId)
            };

            string? planningText = null;
            if (context.EmitExecutionPlanText)
            {
                var planningTextPhase = global::Musoq.Converter.EvaluatorPerformanceTelemetry.BeginPhase("semantic.planning-text");
                try
                {
                    planningText = PlanningTextPrinter.Print(planningResult);
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

            return new PlanningStageBuildResult(artifacts, updatedSemantic, updatedContext);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SchemaProviderFailureException providerFailure)
        {
            ExceptionDispatchInfo.Capture(providerFailure.InnerException ?? providerFailure).Throw();
            throw new InvalidOperationException("Schema provider failure rethrow did not propagate.");
        }
        catch (Exception ex) when (EvaluatorExceptionTaxonomy.FindSchemaProviderFailure(ex) is not null)
        {
            var providerFailure = EvaluatorExceptionTaxonomy.FindSchemaProviderFailure(ex)!;
            ExceptionDispatchInfo.Capture(providerFailure.InnerException ?? providerFailure).Throw();
            throw new InvalidOperationException("Schema provider failure rethrow did not propagate.");
        }
        catch (Exception ex) when (EvaluatorExceptionTaxonomy.IsExpectedQueryFailure(ex))
        {
            if (!context.DiagnosticContext.HasErrors)
                context.DiagnosticContext.ReportException(ex);
            return null;
        }
        catch (Exception ex)
        {
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
        var phase = global::Musoq.Converter.EvaluatorPerformanceTelemetry.BeginPhase("semantic.logical-plan");
        try
        {
            var logicalBuilder = new LogicalPlanBuilder(aliasKeyedColumns);
            var logicalTraverser = new LogicalPlanBuildTraverseVisitor(logicalBuilder);
            queryTree.Accept(logicalTraverser);

            if (logicalTraverser.Result is null)
                return null;

            var logicalOptimizer = new LogicalOptimizer(
                context.CompilationOptions.UseConstantFolding,
                context.DiagnosticContext);
            var logicalOptimizationResult = logicalOptimizer.Optimize(logicalTraverser.Result);
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
        IReadOnlyDictionary<string, SourcePredicatePlan> sourcePredicatePlans)
    {
        var result = new Dictionary<SchemaFromNode, WhereNode>(rawWhereNodes.Count);

        foreach (var whereNode in rawWhereNodes)
        {
            if (!string.IsNullOrWhiteSpace(whereNode.Key.Id) &&
                sourcePredicatePlans.TryGetValue(whereNode.Key.Id, out var predicatePlan))
            {
                result[whereNode.Key] = predicatePlan.PushedWhereNode;
                continue;
            }

            result[whereNode.Key] = whereNode.Value;
        }

        return result;
    }
}
