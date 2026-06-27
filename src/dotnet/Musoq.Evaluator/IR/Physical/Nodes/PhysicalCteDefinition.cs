using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Physical.Nodes;

public sealed record PhysicalCteDefinition(string Name, PhysicalNode Plan);
