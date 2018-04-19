using Musoq.Parser.Nodes;

namespace Musoq.Parser
{
    public interface ISchemaAwareExpressionVisitor : IExpressionVisitor
    {
        string CurrentSchema { get; set; }

        string CurrentTable { get; }

        string[] CurrentParameters { get; }

        void SetCurrentTable(string table, string[] parameters);

        void AddCteSchema(string name);

        void SetCurrentCteName(string name);

        void BeginCteQueryPart(CteExpressionNode node, CtePart part);

        void EndCteQuery();

        void ClearAliases();
    }
}