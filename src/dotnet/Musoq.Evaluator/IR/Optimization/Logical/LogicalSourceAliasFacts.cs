using System.Linq;

namespace Musoq.Evaluator.IR.Optimization.Logical;

internal sealed record LogicalSourceAliasFacts(
    LogicalSourceContextFact[] SourceContexts,
    LogicalAliasScopeFact[] AliasScopes)
{
    public bool HasStableSourceContextAssignments =>
        SourceContexts.All(static source => !string.IsNullOrWhiteSpace(source.SourceContextId));

    public bool AliasDiagnosticsAreComplete =>
        AliasScopes.All(static scope => scope.DuplicateAliases.Length == 0);
}

