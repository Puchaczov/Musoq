using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionRowPresence(
    string Alias,
    bool IsPresent,
    ExecutionExpression PresenceSource) : ExecutionExpression(ExecutionClrBindingFactory.FromClr(typeof(bool)));
