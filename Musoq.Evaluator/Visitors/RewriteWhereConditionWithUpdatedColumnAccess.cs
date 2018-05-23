using System.Collections.Generic;
using Musoq.Evaluator.Helpers;
using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors
{
    public class RewriteWhereConditionWithUpdatedColumnAccess : CloneQueryVisitor
    {
        private readonly IDictionary<string, string> _aliases;

        public RewriteWhereConditionWithUpdatedColumnAccess(IDictionary<string, string> usedTables)
        {
            _aliases = usedTables;
        }

        public WhereNode Where { get; private set; }

        public override void Visit(AccessColumnNode node)
        {
            if (_aliases.ContainsKey(node.Alias))
                base.Visit(new AccessColumnNode(NamingHelper.ToColumnName(node.Alias, node.Name), _aliases[node.Alias],
                    node.ReturnType, TextSpan.Empty));
            else
                base.Visit(node);
        }

        public override void Visit(WhereNode node)
        {
            Where = new WhereNode(Nodes.Pop());
        }
    }
}