using System.Linq;
using Musoq.Schema;

namespace Musoq.Evaluator.TemporarySchemas;

internal sealed class CoupledTableSchema(ISchemaTable source, ISchemaColumn[] columns) : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } = columns;

    public SchemaTableMetadata Metadata => source.Metadata;

    public ISchemaColumn? GetColumnByName(string name) => GetColumnsByName(name).SingleOrDefault();

    public ISchemaColumn[] GetColumnsByName(string name) => source.GetColumnsByName(name)
        .Where(candidate => Columns.Any(column => column.ColumnIndex == candidate.ColumnIndex &&
                                                column.ColumnName == candidate.ColumnName))
        .ToArray();
}
