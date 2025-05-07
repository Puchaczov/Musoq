using System.Reflection;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Nodes;

public class AccessRefreshAggregationScoreNode(
    FunctionToken fToken,
    ArgsListNode args,
    ArgsListNode extraArgs,
    bool canSkipInjectSource,
    MethodInfo method,
    string alias)
    : AccessMethodNode(fToken, args, extraArgs, canSkipInjectSource, method, alias)
{
    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }
}