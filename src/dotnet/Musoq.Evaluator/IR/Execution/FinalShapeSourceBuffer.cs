using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

internal sealed record FinalShapeSourceBuffer(
    string ShapeTypeName,
    IReadOnlyList<FieldBinding> Fields);
