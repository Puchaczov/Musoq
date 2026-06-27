using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
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
/// Typed view of the rendering stage output: the compiled assembly inputs needed
/// by the downstream compilation build link.
/// </summary>
internal sealed record RenderingBuildArtifacts(CSharpCompilation Compilation, string AccessToClassPath);
