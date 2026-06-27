using System.Linq;
using System.Reflection;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private static bool ContainsInjectQueryStatsMethodCall(ExecutionBlock block)
    {
        return ExecutionIrAnalysis.FlattenExpressions(block).Any(ExpressionUsesInjectQueryStats);
    }

    private static bool ExpressionUsesInjectQueryStats(ExecutionExpression expression)
    {
        return expression switch
        {
            ExecutionMethodCall methodCall => MethodUsesInjectQueryStats(methodCall.Method),
            ExecutionAggregateCall aggregateCall => MethodUsesInjectQueryStats(aggregateCall.Method),
            _ => false
        };
    }

    private static bool MethodUsesInjectQueryStats(MethodInfo method)
    {
        return method.GetParameters()
            .Any(parameter => parameter.GetCustomAttribute<InjectQueryStatsAttribute>() != null);
    }
}
