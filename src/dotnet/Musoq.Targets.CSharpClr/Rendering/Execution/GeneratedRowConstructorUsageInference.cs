using System.Collections.Generic;
using System.Linq;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static Dictionary<string, HashSet<string>> CollectGeneratedRowVariableTypeNames(
        ExecutionBlock block,
        IReadOnlyDictionary<int, TypedStoredTableResult>? typedStoredTableResults = null) =>
        GeneratedRowVariableTypes.Collect(block, typedStoredTableResults?.ToDictionary(
            static pair => pair.Key, static pair => pair.Value.RowShape));
}
