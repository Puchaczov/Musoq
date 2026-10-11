using System.Collections.Generic;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Targets.CSharpClr;

internal static class HashBuildBucketNaming
{
    internal static string Resolve(ExecutionHashAdd hashAdd)
    {
        var reservedNames = new HashSet<string>(StringComparer.Ordinal)
        {
            hashAdd.Row.Name,
            hashAdd.Hash.Name,
            hashAdd.KeyVariableName ?? "key"
        };
        if (hashAdd.PrecomputedKey is { } key)
            reservedNames.Add(key.Name);

        var name = hashAdd.BucketVariableName ?? "matches";
        while (reservedNames.Contains(name))
            name += "Bucket";

        return name;
    }
}
