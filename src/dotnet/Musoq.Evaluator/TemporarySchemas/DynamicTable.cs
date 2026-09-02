using System.Linq;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.TemporarySchemas;

public class DynamicTable : ISchemaTable
{
    public DynamicTable(ISchemaColumn[] columns, Type? metadata = null, bool caseSensitive = false)
    {
        IsCaseSensitive = caseSensitive;
        // Logical scalar identity is part of the column contract. Columns with
        // equal carriers but different enum identities must never collapse.
        var distinctColumnsGroups = columns.GroupBy(f => new
        {
            f.ColumnName,
            f.ColumnIndex,
            f.ColumnType,
            f.SourceReadType,
            EnumFingerprint = f.EnumType?.Fingerprint
        });

        Columns = distinctColumnsGroups.Select(group =>
        {
            var firstColumn = group.First();
            return new SchemaColumn(
                group.Key.ColumnName,
                group.Key.ColumnIndex,
                group.Key.ColumnType,
                group.Key.SourceReadType,
                firstColumn.EnumType,
                firstColumn.IntendedTypeName,
                firstColumn.ReadModifiers,
                group.Any(static column => column.Stability == ColumnStability.Volatile)
                    ? ColumnStability.Volatile
                    : ColumnStability.Stable);
        }).Cast<ISchemaColumn>().ToArray();
        Metadata = new SchemaTableMetadata(metadata ?? typeof(object));
    }

    public ISchemaColumn[] Columns { get; }

    public bool IsCaseSensitive { get; }

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(column =>
            string.Equals(column.ColumnName, name,
                IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase));
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(column =>
            string.Equals(column.ColumnName, name,
                IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    public SchemaTableMetadata Metadata { get; }
}
