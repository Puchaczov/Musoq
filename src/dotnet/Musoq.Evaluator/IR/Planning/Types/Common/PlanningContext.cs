using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Optimization;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Planning.Cardinality;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;
using Musoq.Parser.Nodes;
using Musoq.Schema;
using SchemaFromNode = Musoq.Parser.Nodes.From.SchemaFromNode;

namespace Musoq.Evaluator.IR.Planning;

internal sealed partial record PlanningContext(
    LogicalPlanningArtifacts LogicalArtifacts,
    CompilationOptions CompilationOptions,
    ISchemaProvider SchemaProvider,
    IReadOnlyDictionary<SchemaFromNode, ISchemaColumn[]> UsedSchemaColumns,
    IReadOnlyDictionary<SchemaFromNode, WhereNode> UsedWhereNodes,
    IReadOnlyDictionary<SchemaFromNode, SourcePlanRequest> SourcePlanRequestsBySource,
    IReadOnlyDictionary<string, ISchemaColumn[]> InferredColumns,
    IReadOnlyDictionary<string, IReadOnlySet<string>> UsedColumns,
    Scope? Scope,
    SchemaRegistry? SchemaRegistry,
    IPlanningShapeResolver ShapeResolver,
    CteExecutionPlan? CteExecutionPlan)
{ public LogicalNode LogicalPlan => LogicalArtifacts.OptimizedLogicalPlan; }
