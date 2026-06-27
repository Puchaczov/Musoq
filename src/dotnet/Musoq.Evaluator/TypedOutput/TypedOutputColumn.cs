using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Musoq.Evaluator.TypedOutput;

internal sealed record TypedOutputColumn(string Name, int Index, Type Type);
