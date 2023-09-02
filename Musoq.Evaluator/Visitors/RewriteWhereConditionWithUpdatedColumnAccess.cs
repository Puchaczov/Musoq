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
            base.Visit(_aliases.TryGetValue(node.Alias, out var alias)
                ? new AccessColumnNode(NamingHelper.ToColumnName(node.Alias, node.Name), alias, node.ReturnType,
                    TextSpan.Empty)
                : node);
        }

        public override void Visit(WhereNode node)
        {
            Where = new WhereNode(Nodes.Pop());
        }
    }
}