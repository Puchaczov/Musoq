using System;

namespace Musoq.Parser.Nodes
{
    public class DetailedAccessColumnNode : AccessColumnNode
    {
        public DetailedAccessColumnNode(string column, int argFieldOrder, Type returnType)
            : base(column, argFieldOrder, returnType, TextSpan.Empty)
        {
        }
    }
}