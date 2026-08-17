using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionStoredTable(int TableIndex)
    : ExecutionExpression(ExecutionClrBindingFactory.FromClr(typeof(Table)));
