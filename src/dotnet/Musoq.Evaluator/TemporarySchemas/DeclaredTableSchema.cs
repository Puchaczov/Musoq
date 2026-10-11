using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.TemporarySchemas;

internal sealed class DeclaredTableSchema(CreateTableNode declaration, ISchemaColumn[] columns)
    : DynamicTable(columns, caseSensitive: true)
{
    public CreateTableNode Declaration { get; } = declaration;
}
