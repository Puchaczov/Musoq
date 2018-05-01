using System;
using System.Reflection;
using Musoq.Parser.Tokens;
using Musoq.Plugins.Attributes;

namespace Musoq.Parser.Nodes
{
    public class AccessMethodNode : Node
    {
        public readonly FunctionToken FToken;

        public AccessMethodNode(FunctionToken fToken, ArgsListNode args, ArgsListNode extraAggregateArguments, MethodInfo method = (MethodInfo) null, string alias = "")
        {
            FToken = fToken;
            Arguments = args;
            ExtraAggregateArguments = extraAggregateArguments;
            Method = method;
            Alias = alias;
            Id = $"{nameof(AccessMethodNode)}{alias}{fToken.Value}{args.Id}";
        }

        public MethodInfo Method { get; private set; }

        public ArgsListNode Arguments { get; }

        public string Name => FToken.Value;

        public string Alias { get; }

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