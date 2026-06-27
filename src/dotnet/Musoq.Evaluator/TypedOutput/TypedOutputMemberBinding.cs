using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Musoq.Evaluator.TypedOutput;

internal sealed record TypedOutputMemberBinding(
    string MemberName,
    Type TargetType,
    TypedOutputColumn Column,
    MemberInfo Member);
