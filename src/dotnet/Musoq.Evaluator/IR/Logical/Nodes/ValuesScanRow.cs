using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Logical.Nodes;

public sealed record ValuesScanRow(IReadOnlyList<ValuesScanField> Fields);
