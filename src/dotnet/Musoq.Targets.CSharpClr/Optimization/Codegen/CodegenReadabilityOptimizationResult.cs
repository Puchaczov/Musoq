using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Targets.CSharpClr.Optimization.Codegen;

internal sealed record CodegenReadabilityOptimizationResult(
    CompilationUnitSyntax InitialCode,
    CompilationUnitSyntax OptimizedCode,
    OptimizationTrace Trace);

