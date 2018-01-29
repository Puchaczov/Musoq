using System.Reflection;
using FQL.Parser.Tokens;

namespace FQL.Parser.Nodes
{
    public class GroupByAccessMethodNode : AccessMethodNode
    {
        public GroupByAccessMethodNode(FunctionToken fToken, ArgsListNode args, ArgsListNode extraArgs, MethodInfo method) 
            : base(fToken, args, extraArgs, method)
        {
        }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}