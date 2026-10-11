using System.Collections.Generic;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.Tests.Schema.LibraryExecution;

internal static class LibraryExecutionFixture
{
    public static LibraryExecutionEntity[] CreateRows() =>
    [
        new(1, "A", 1, 1.25m, 1.5d),
        new(2, "A", 2, 2.75m, 2.5d),
        new(3, "A", null, null, null),
        new(4, "B", -3, -3m, -3.5d),
        new(5, "B", 5, 5m, 5.5d)
    ];

    public static ISchemaProvider CreateProvider() => CreateProvider(CreateRows());

    public static ISchemaProvider CreateProvider<TEntity>(TEntity[] rows)
        => CreateProvider<TEntity, LibraryExecutionLibrary>(rows);

    public static ISchemaProvider CreateProvider<TEntity, TLibrary>(TEntity[] rows,
        SourceExecutionPermissions permissions = SourceExecutionPermissions.None)
        where TLibrary : Musoq.Plugins.LibraryBase, new()
    {
        var source = new GenericChunkSource<TEntity>(rows,
            GenericEntityTable<TEntity>.NameToIndexMap,
            GenericEntityTable<TEntity>.IndexToObjectAccessMap);
        var schema = new GenericSchema<TLibrary>(
            new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
            {
                ["first"] = (new GenericEntityTable<TEntity>(), source)
            }, executionPermissions: permissions);

        return new GenericSchemaProvider(new Dictionary<string, ISchema> { ["#schema"] = schema });
    }
}
