using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Converter;

internal sealed class InMemorySourceShape
{
    private static readonly ConcurrentDictionary<Type, Lazy<InMemorySourceShape>> Shapes = new();

    private readonly ISchemaColumn[] _columns;

    private InMemorySourceShape(Type rowType)
    {
        RowType = rowType;
        _columns = CreateColumns(rowType);
        Metadata = new SchemaTableMetadata(rowType);
    }

    public Type RowType { get; }

    public SchemaTableMetadata Metadata { get; }

    public static InMemorySourceShape For(Type rowType)
    {
        ArgumentNullException.ThrowIfNull(rowType);

        return Shapes.GetOrAdd(
            rowType,
            static type => new Lazy<InMemorySourceShape>(
                () => new InMemorySourceShape(type),
                LazyThreadSafetyMode.ExecutionAndPublication)).Value;
    }

    public ISchemaColumn[] CreateColumnsSnapshot()
    {
        return _columns.ToArray();
    }

    public ISchemaColumn? GetColumnByName(string name)
    {
        return _columns.SingleOrDefault(column => string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase));
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return _columns
            .Where(column => string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private static ISchemaColumn[] CreateColumns(Type rowType)
    {
        if (IsScalar(rowType))
            return [new SchemaColumn("Value", 0, rowType)];

        var columns = new List<ISchemaColumn>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in rowType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || property.GetIndexParameters().Length != 0)
                continue;

            AddColumn(columns, names, property.Name, property.PropertyType);
        }

        foreach (var field in rowType.GetFields(BindingFlags.Public | BindingFlags.Instance))
            AddColumn(columns, names, field.Name, field.FieldType);

        return columns.Count > 0
            ? columns.ToArray()
            : [new SchemaColumn("Value", 0, rowType)];
    }

    private static void AddColumn(
        ICollection<ISchemaColumn> columns,
        ISet<string> names,
        string name,
        Type type)
    {
        if (!names.Add(name))
            throw new InvalidOperationException($"In-memory source row member '{name}' is ambiguous.");

        columns.Add(new SchemaColumn(name, columns.Count, type));
    }

    private static bool IsScalar(Type type)
    {
        return type.IsPrimitive ||
               type.IsEnum ||
               type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(DateTime) ||
               type == typeof(DateTimeOffset) ||
               type == typeof(Guid) ||
               type == typeof(TimeSpan);
    }
}
