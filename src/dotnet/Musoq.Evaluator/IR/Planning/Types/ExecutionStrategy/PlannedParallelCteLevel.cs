using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record PlannedParallelCteLevel(
    int Level,
    IReadOnlyList<string> DefinitionNames);
