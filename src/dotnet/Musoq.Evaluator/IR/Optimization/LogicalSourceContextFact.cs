using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Optimization;

internal sealed record LogicalSourceContextFact(
    string ScopePath,
    string Alias,
    string? SourceContextId,
    string SourceKind);
