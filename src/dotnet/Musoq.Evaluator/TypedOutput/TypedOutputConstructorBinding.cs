using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Musoq.Evaluator.TypedOutput;

internal sealed record TypedOutputConstructorBinding(TypedOutputColumn Column, Type TargetType);
