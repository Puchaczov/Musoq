using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.IR.Optimization.Logical;

internal sealed record LogicalSourceContextFact(
    string ScopePath,
    string Alias,
    string? SourceContextId,
    string SourceKind);

