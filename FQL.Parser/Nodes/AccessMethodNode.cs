using System;
using System.Linq;
using System.Reflection;
using FQL.Parser.Tokens;
using FQL.Plugins.Attributes;

namespace FQL.Parser.Nodes
{
    public class AccessMethodNode : Node
    {
        public readonly FunctionToken FToken;

        public AccessMethodNode(FunctionToken fToken, ArgsListNode args, ArgsListNode extraAggregateArguments, MethodInfo method = (MethodInfo) null)
        {
            FToken = fToken;
            Arguments = args;
            ExtraAggregateArguments = extraAggregateArguments;
            Method = method;
            Id = $"{nameof(AccessMethodNode)}{fToken.Value}{args.Id}";
        }

        public MethodInfo Method { get; private set; }

        public ArgsListNode Arguments { get; }

        public string Name => FToken.Value;

        public bool IsAggregateMethod => Method != null && Method.GetCustomAttribute<AggregationMethodAttribute>() != null;

        public ArgsListNode ExtraAggregateArguments { get; }

        public int ArgsCount => Arguments.Args.Length;
        public override Type ReturnType => Method != null ? Method.ReturnType : typeof(void);

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public void ChangeMethod(MethodInfo method)
        {
            Method = method;
        }

        public override string Id { get; }

        public override string ToString()
        {
            return ArgsCount > 0 ? $"{Name}({Arguments.ToString()})" : $"{Name}()";
        }
    }
}