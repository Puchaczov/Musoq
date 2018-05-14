using System.Collections.Generic;
using Musoq.Evaluator.Helpers;
using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors
{
    public class RewriteWhereConditionWithUpdatedColumnAccess : RewriteQueryVisitor
    {
        private readonly IDictionary<string, string> _aliases;

        public WhereNode Where { get; private set; }

        public RewriteWhereConditionWithUpdatedColumnAccess(TransitionSchemaProvider schemaProvider, List<AccessMethodNode> refreshMethods, IDictionary<string, string> usedTables) 
            : base(schemaProvider, refreshMethods)
        {
            _aliases = usedTables;
        }

        public override void Visit(AccessColumnNode node)
        {
            if(_aliases.ContainsKey(node.Alias))
                base.Visit(new AccessColumnNode(NamingHelper.ToColumnName(node.Alias, node.Name), _aliases[node.Alias], node.ReturnType, TextSpan.Empty));
            else
                base.Visit(node);
        }

        public override void Visit(WhereNode node)
        {
            Where = new WhereNode(Nodes.Pop());
        }
    }
}
