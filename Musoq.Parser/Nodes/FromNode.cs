using System;
using Musoq.Schema.DataSources;

namespace Musoq.Parser.Nodes
{
    public abstract class FromNode : Node
    {
        protected FromNode(string alias)
        {
            Alias = alias;
        }

        public virtual string Alias { get; }

        public override Type ReturnType => typeof(RowSource);
    }
}