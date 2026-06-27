using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private static ExecutionVariable[] CollectMethodCallCaches(ExecutionBlock block)
    {
        var caches = new Dictionary<string, ExecutionVariable>(StringComparer.Ordinal);
        foreach (var methodCall in ExecutionIrAnalysis.CollectExpressions<ExecutionMethodCall>(block))
        {
            if (methodCall.Cache != null)
                caches.TryAdd(methodCall.Cache.Name, methodCall.Cache);
        }

        return caches.Values.ToArray();
    }
}
