using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.IR.Optimization.Logical;

internal sealed record LogicalAliasScopeFact(
    string ScopePath,
    string[] Aliases,
    string[] DuplicateAliases);

