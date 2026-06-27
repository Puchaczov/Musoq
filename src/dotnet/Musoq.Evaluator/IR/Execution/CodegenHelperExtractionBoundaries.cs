using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

internal sealed record CodegenHelperExtractionBoundaries(
    string PhaseBoundary,
    string MutationBoundary,
    string CancellationBoundary,
    string ProgressBoundary,
    string QueryStatisticsBoundary,
    string CaptureBoundary,
    string ReturnBoundary,
    string OrderingKey);
