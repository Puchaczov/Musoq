using System.Collections.Generic;
using System.Linq;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;

namespace Musoq.Tests.Common;

public sealed class ApplyCarrierSchemaProvider(int rows = 1) : ISchemaProvider
{
    public const string BatchKey = "batch-alpha";
    public const string SampleKey = "sample-alpha";

    public ISchema GetSchema(string schema) => new CarrierSchema(rows);

    private sealed class CarrierSchema(int rows) : SchemaBase("fixture", CreateLibrary())
    {
        public override ISchemaTable GetTableByName(string name, SourceMetadataContext context,
            params object?[] parameters) => name == "batches"
            ? new CarrierTable<ApplyCarrierBatch>()
            : new CarrierTable<ApplyCarrierSample>();

        public override RowSource<T> GetRowSource<T, TArguments>(string name,
            SourceExecutionContext context, TArguments arguments)
        {
            if (name == "batches")
                return EnsureSourceType<T, ApplyCarrierBatch>(name,
                    new CarrierRows<ApplyCarrierBatch>(Enumerable.Repeat(new ApplyCarrierBatch(BatchKey, BatchKey), rows).ToArray()));

            return EnsureSourceType<T, ApplyCarrierSample>(name,
                new CarrierRows<ApplyCarrierSample>([new(SampleKey, [73, 0, 0, 0])]));
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methods = new MethodsManager();
            methods.RegisterLibraries(new ApplyCarrierLibrary());
            return new MethodsAggregator(methods);
        }
    }

    private sealed class CarrierTable<T> : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } = typeof(T).GetProperties()
            .Select((property, index) => (ISchemaColumn)new SchemaColumn(property.Name, index, property.PropertyType))
            .ToArray();

        public SchemaTableMetadata Metadata { get; } = new(typeof(T));
        public ISchemaColumn? GetColumnByName(string name) => Columns.FirstOrDefault(column => column.ColumnName == name);
        public ISchemaColumn[] GetColumnsByName(string name) => Columns.Where(column => column.ColumnName == name).ToArray();
    }

    private sealed class CarrierRows<T>(IReadOnlyList<T> rows) : RowSource<T>
    {
        public override IEnumerable<IReadOnlyList<T>> Chunks { get { yield return rows; } }
    }
}

public sealed record ApplyCarrierBatch(string Label, string Key);
public sealed record ApplyCarrierSample(string Key, byte[] Payload);

public sealed class ApplyCarrierLibrary : LibraryBase
{
    [BindableMethod]
    public byte[] ReadPayload([InjectSpecificSource(typeof(ApplyCarrierSample))] ApplyCarrierSample sample) => sample.Payload;
}
