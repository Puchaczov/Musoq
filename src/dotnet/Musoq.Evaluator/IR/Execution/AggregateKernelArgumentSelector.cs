using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

internal static class AggregateKernelArgumentSelector
{
    public static TArgument[] SelectValueArgumentsAfterGroup<TArgument>(
        IReadOnlyList<TArgument> arguments)
    {
        if (arguments.Count <= 1)
            return [];

        var values = new TArgument[arguments.Count - 1];
        for (var argumentIndex = 1; argumentIndex < arguments.Count; argumentIndex++)
            values[argumentIndex - 1] = arguments[argumentIndex];

        return values;
    }
}
