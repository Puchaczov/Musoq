namespace Musoq.Parser
{
    public interface IQueryPartAwareExpressionVisitor : IExpressionVisitor
    {
        void SetQueryPart(QueryPart part);
    }
}