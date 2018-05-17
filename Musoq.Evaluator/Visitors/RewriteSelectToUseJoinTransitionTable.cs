using System.Collections.Generic;
using Musoq.Evaluator.Helpers;
using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors
{
    public class RewriteSelectToUseJoinTransitionTable : CloneQueryVisitor
    {
        private readonly string _alias;

        public RewriteSelectToUseJoinTransitionTable(string alias = "")
        {
            _alias = alias;
        }

        public SelectNode ChangedSelect { get; private set; }
        public GroupByNode ChangedGroupBy { get; private set; }
        public Node RewrittenNode => Nodes.Pop();

        public override void Visit(AccessColumnNode node)
        {
            base.Visit(new AccessColumnNode(NamingHelper.ToColumnName(node.Alias, node.Name), _alias, node.ReturnType, node.Span));
        }

        public override void Visit(SelectNode node)
        {
            var fields = new FieldNode[node.Fields.Length];

            for (int i = 0, j = fields.Length - 1; i < fields.Length; i++, j--)
            {
                fields[j] = (FieldNode)Nodes.Pop();
            }

            ChangedSelect = new SelectNode(fields);
        }

        public override void Visit(GroupByNode node)
        {
            var fields = new FieldNode[node.Fields.Length];

            for (int i = 0, j = fields.Length - 1; i < fields.Length; i++, j--)
            {
                fields[j] = (FieldNode)Nodes.Pop();
            }

            if(node.Having != null)
                ChangedGroupBy = new GroupByNode(fields, (HavingNode)Nodes.Pop());
            else
                ChangedGroupBy = new GroupByNode(fields, null);
        }
    }
}
