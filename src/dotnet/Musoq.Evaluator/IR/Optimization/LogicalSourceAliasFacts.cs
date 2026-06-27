using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Optimization;

internal sealed record LogicalSourceAliasFacts(
    LogicalSourceContextFact[] SourceContexts,
    LogicalAliasScopeFact[] AliasScopes)
{
    public bool HasStableSourceContextAssignments =>
        SourceContexts.All(static source => !string.IsNullOrWhiteSpace(source.SourceContextId));

    public bool AliasDiagnosticsAreComplete =>
        AliasScopes.All(static scope => scope.DuplicateAliases.Length == 0);
}
