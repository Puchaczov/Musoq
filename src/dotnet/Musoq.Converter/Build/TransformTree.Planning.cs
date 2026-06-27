using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Optimization;
using Musoq.Evaluator.IR.Planning;
using Musoq.Evaluator.IR.Planning.Printing;
using Musoq.Evaluator.Visitors;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;
using Musoq.Parser.Nodes;
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

    private static PlanningBuildArtifacts? BuildPlans(
        SemanticBuildArtifacts semantic,
        BuildMetadataAndInferTypesVisitor metadata,
        BuildItems items)
    {
        try
        {
            var aliasKeyedColumns = CreateAliasKeyedInferredColumns(metadata);
            var logicalBuilder = new LogicalPlanBuilder(aliasKeyedColumns);
            var logicalTraverser = new LogicalPlanBuildTraverseVisitor(logicalBuilder);
            semantic.TransformedQueryTree.Accept(logicalTraverser);

            if (logicalTraverser.Result is null)
                return null;

            var logicalOptimizer = new LogicalOptimizer(
                items.CompilationOptions.UseConstantFolding,
                items.DiagnosticContext);
            var logicalOptimizationResult = logicalOptimizer.Optimize(logicalTraverser.Result);
            var logicalArtifacts = new LogicalPlanningArtifacts(logicalOptimizationResult.InitialPlan, logicalOptimizationResult.OptimizedPlan, logicalOptimizationResult.Trace);

            var planningContext = new PlanningContext(
                logicalArtifacts,
                items.CompilationOptions,
                items.SchemaProvider,
                semantic.UsedColumns,
                semantic.UsedWhereNodes,
                semantic.SourcePlanRequestsPerSchema,
                semantic.PipelineInferredColumns ?? aliasKeyedColumns,
                semantic.PipelineUsedColumns ?? new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase),
                semantic.PipelineScope,
                items.SchemaRegistry,
                semantic.CteExecutionPlan)
            {
                SourceContractDiagnosticLocationsBySource = semantic.SourceContractDiagnosticLocationsPerSchema
            };

            var planner = new QueryPlanner();
            var planningResult = planner.Plan(planningContext);
            SourceContractDiagnosticReporter.Report(
                planningResult,
                items.DiagnosticContext);
            OptimizationFallbackWarningReporter.ReportFallbackWarnings(
                planningResult,
                items.CompilationOptions,
                items.DiagnosticContext);

            items.OptimizerTraceText = OptimizationTraceTextPrinter.Append(
                items.OptimizerTraceText,
                logicalArtifacts.OptimizerTrace);
            items.OptimizerTraceText = OptimizationTraceTextPrinter.Append(
                items.OptimizerTraceText,
                planningResult.PhysicalArtifacts.OptimizerTrace);
            items.UsedWhereNodes = ApplyPlannedWhereNodes(semantic.UsedWhereNodes, planningResult.Properties.SourcePredicatePlansBySourceId);

            return new PlanningBuildArtifacts
            {
                InitialLogicalPlan = logicalArtifacts.InitialLogicalPlan,
                OptimizedLogicalPlan = logicalArtifacts.OptimizedLogicalPlan,
                LogicalPlan = logicalArtifacts.OptimizedLogicalPlan,
                PlanningResult = planningResult,
                PlanningText = PlanningTextPrinter.Print(planningResult),
                InitialPhysicalPlan = planningResult.PhysicalArtifacts.InitialPhysicalPlan,
                OptimizedPhysicalPlan = planningResult.PhysicalArtifacts.OptimizedPhysicalPlan,
                PhysicalPlan = planningResult.PhysicalArtifacts.OptimizedPhysicalPlan
            };
        }
        catch (IndexOutOfRangeException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Physical plan building failed. See inner exception for the root cause.", ex);
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
