using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning.Cardinality;

namespace Musoq.Evaluator.IR.Planning;

[Flags]
internal enum CteOutputCharacteristics
{
    None = 0,
    OrderSensitive = 1,
    Aggregate = 2,
    Window = 4,
    SetOperation = 8,
    SideEffectSensitive = 16
}
