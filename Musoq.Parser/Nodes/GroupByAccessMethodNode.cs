using System.Reflection;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Nodes
{
    public class GroupByAccessMethodNode : AccessMethodNode
    {
        public GroupByAccessMethodNode(FunctionToken fToken, ArgsListNode args, ArgsListNode extraArgs,
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