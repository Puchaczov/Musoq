using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public enum ExecutionConstantInSetKind
{
    Array,
    Switch,
    HashSet,
    FrozenSet
}
