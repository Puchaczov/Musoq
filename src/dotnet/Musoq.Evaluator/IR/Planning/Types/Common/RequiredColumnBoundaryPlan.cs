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

internal sealed record RequiredColumnBoundaryPlan(
    string BoundaryId,
    RequiredColumnBoundaryKind Kind,
    string[] RequiredColumns,
    string[] RetainedColumns,
    string[] BlockedColumns,
    string[] OriginOutputMappings,
    PlanningConfidence Confidence,
    string Reason);
