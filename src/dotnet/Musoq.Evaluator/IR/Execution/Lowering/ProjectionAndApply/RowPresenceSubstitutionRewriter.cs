using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

internal sealed class RowPresenceSubstitutionRewriter(
    IReadOnlyDictionary<string, bool> presenceByAlias) : ExecutionIrRewriter
{
    protected override ExecutionExpression RewriteRowPresence(ExecutionRowPresence expression)
    {
        if (!presenceByAlias.TryGetValue(expression.Alias, out var isPresent))
            return base.RewriteRowPresence(expression);

        var value = expression.IsPresent ? isPresent : !isPresent;
        return new ExecutionLiteral(value, typeof(bool));
    }
}
