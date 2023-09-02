using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors
{
    public class GetSelectFieldsTraverseVisitor : RawTraverseVisitor<IQueryPartAwareExpressionVisitor>
    {
        public GetSelectFieldsTraverseVisitor(IQueryPartAwareExpressionVisitor visitor) 
            : base(visitor)
        {
        }

        public override void Visit(QueryNode node)
        {
            base.Visit(node);
            Visitor.SetQueryPart(QueryPart.None);
        }

        public override void Visit(SelectNode node)
        {
            Visitor.SetQueryPart(QueryPart.Select);
            base.Visit(node);
            Visitor.SetQueryPart(QueryPart.None);
        }

        public override void Visit(WhereNode node)
        {
            Visitor.SetQueryPart(QueryPart.Where);
            base.Visit(node);
            Visitor.SetQueryPart(QueryPart.None);
        }

        public override void Visit(GroupByNode node)
        {
            Visitor.SetQueryPart(QueryPart.GroupBy);
            base.Visit(node);
            Visitor.SetQueryPart(QueryPart.None);
        }

        public override void Visit(HavingNode node)
        {
            Visitor.SetQueryPart(QueryPart.Having);
            base.Visit(node);
            Visitor.SetQueryPart(QueryPart.None);
        }

        public override void Visit(FromNode node)
        {
            Visitor.SetQueryPart(QueryPart.From);
            base.Visit(node);
            Visitor.SetQueryPart(QueryPart.None);
        }

        public override void Visit(ExpressionFromNode node)
        {
            Visitor.SetQueryPart(QueryPart.From);
            base.Visit(node);
            Visitor.SetQueryPart(QueryPart.None);
        }

        public override void Visit(InMemoryTableFromNode node)
        {
            Visitor.SetQueryPart(QueryPart.From);
            base.Visit(node);
            Visitor.SetQueryPart(QueryPart.None);
        }

        public override void Visit(JoinFromNode node)
        {
            Visitor.SetQueryPart(QueryPart.From);
            base.Visit(node);
            Visitor.SetQueryPart(QueryPart.None);
        }

        public override void Visit(JoinInMemoryWithSourceTableFromNode node)
        {
            Visitor.SetQueryPart(QueryPart.From);
            base.Visit(node);
            Visitor.SetQueryPart(QueryPart.None);
        }

        public override void Visit(JoinSourcesTableFromNode node)
        {
            Visitor.SetQueryPart(QueryPart.From);
            base.Visit(node);
            Visitor.SetQueryPart(QueryPart.None);
        }

        public override void Visit(SchemaFromNode node)
        {
            Visitor.SetQueryPart(QueryPart.From);
            base.Visit(node);
            Visitor.SetQueryPart(QueryPart.None);
        }
    }
}