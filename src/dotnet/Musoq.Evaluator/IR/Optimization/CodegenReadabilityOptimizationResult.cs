using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Optimization;

internal sealed record CodegenReadabilityOptimizationResult(
    CompilationUnitSyntax InitialCode,
    CompilationUnitSyntax OptimizedCode,
    OptimizationTrace Trace);
