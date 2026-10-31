using System.Collections.Generic;
using System.Linq;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private CapturedLocal[] CollectParallelSingleKeyAggregateCaptures(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate)
    {
        var excludedNames = new HashSet<string>(StringComparer.Ordinal)
        {
            parallelAggregate.Source.Name,
            parallelAggregate.Group.Name,
            parallelAggregate.RootGroup.Name,
            parallelAggregate.GroupsToFinalize.Name
        };
        var captures = new Dictionary<string, CapturedLocal>(StringComparer.Ordinal);

        foreach (var variableName in ExecutionIrAnalysis.CollectDeclaredVariableNames(parallelAggregate.AggregateBody))
            excludedNames.Add(variableName);
        AddHelperCaptures(parallelAggregate.Key, excludedNames, captures);
        AddHelperCaptures(parallelAggregate.AggregateBody, excludedNames, captures);

        return captures.Values.ToArray();
    }
}
