using System;

namespace FQL.Parser.Nodes
{
    public class DetailedAccessColumnNode : AccessColumnNode
    {
        public DetailedAccessColumnNode(string column, int argFieldOrder, Type returnType)
            : base(column, argFieldOrder, returnType, TextSpan.Empty)
        {
        }
    }
}