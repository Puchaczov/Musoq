using System.Collections.Frozen;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Expressions.CollectionParameters;

namespace Musoq.Evaluator.IR.Execution;

public static partial class ExecutionExpressionConverter
{
    internal static FrozenSet<Type> RegisteredExpressionTypes { get; } = new[] {
        typeof(ColumnRef), typeof(ScriptParameterRef), typeof(ScriptVariableRef), typeof(Literal), typeof(WildcardLiteral), typeof(BinaryOp), typeof(UnaryOp),
        typeof(MethodCall), typeof(StrictCast), typeof(ArrayAccess), typeof(RowPresence), typeof(IsNullCheck), typeof(InCheck), typeof(CollectionInCheck),
        typeof(PatternMatch), typeof(Between), typeof(CaseWhen), typeof(Coalesce), typeof(AggregateRef), typeof(WindowFunctionRef), typeof(CteTableRef)
    }.ToFrozenSet();

    private static NotSupportedException Unsupported(IrExpression expression, string reason) =>
        new($"Execution expression '{expression.GetType().FullName}' is unsupported: {reason}.");
}
