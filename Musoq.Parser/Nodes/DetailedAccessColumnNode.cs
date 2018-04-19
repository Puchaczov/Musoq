using System;

namespace Musoq.Parser.Nodes
{
    public class DetailedAccessColumnNode : AccessColumnNode
    {
        public DetailedAccessColumnNode(string column, int argFieldOrder, Type returnType, string alias)
            : base(column, argFieldOrder, returnType, TextSpan.Empty)
        {
            Alias = alias;
        }

        public string Alias { get; }

        public override string ToString()
        {
            if(string.IsNullOrEmpty(Alias))
                return Name;
            return $"{Alias}.{Name}";
        }
    }
}