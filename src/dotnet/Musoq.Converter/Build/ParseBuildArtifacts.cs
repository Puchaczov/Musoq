using System.Collections.Generic;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Planning;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;
using Musoq.Parser.Nodes;
using Musoq.Schema;
using Musoq.Schema.Optimization;
using SchemaFromNode = Musoq.Parser.Nodes.From.SchemaFromNode;

namespace Musoq.Converter.Build;

/// <summary>
/// Typed view of the parse stage output that the transform pipeline begins from.
/// </summary>
internal sealed record ParseBuildArtifacts(RootNode RawQueryTree);
