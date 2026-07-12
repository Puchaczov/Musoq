using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionRowStream(
    ExecutionVariable Variable,
    ExecutionRowStreamKind Kind,
    ExecutionRowStreamRowsAccess RowsAccess = ExecutionRowStreamRowsAccess.Direct)
    : ExecutionExpression(ExecutionTypeRef.FromClr(typeof(object)));
