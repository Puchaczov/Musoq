namespace FQL.Parser
{
    public interface ISchemaAwareExpressionVisitor : IExpressionVisitor
    {
        string CurrentSchema { get; set; }
        string CurrentTable { get; }
        string[] CurrentParameters { get; }

        void SetCurrentTable(string table, string[] parameters);
    }
}