using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static bool TryRewriteCoalescePayloadKey(
        ExecutionCoalesce coalesce,
        string payloadAlias,
        IReadOnlyDictionary<string, ExecutionExpression> fieldMap,
        [NotNullWhen(true)] out ExecutionExpression? rewritten)
    {
        if (!TryRewritePayloadKeyExpressions(coalesce.Expressions, payloadAlias, fieldMap, out var expressions))
        {
            rewritten = null;
            return false;
        }

        rewritten = coalesce with { Expressions = expressions };
        return true;
    }

}
