using System.Reflection;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Nodes
{
    public class AccessRefreshAggreationScoreNode : AccessMethodNode
    {
        public AccessRefreshAggreationScoreNode(FunctionToken fToken, ArgsListNode args, ArgsListNode extraArgs,
            MethodInfo method, string alias)
            : base(fToken, args, extraArgs, method, alias)
        {
        }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}