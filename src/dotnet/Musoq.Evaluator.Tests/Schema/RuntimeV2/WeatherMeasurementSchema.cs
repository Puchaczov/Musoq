using System.Collections.Generic;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests.Schema.RuntimeV2;

public sealed class WeatherMeasurementSchema(IReadOnlyList<WeatherMeasurementEntity> rows)
    : SchemaBase("weather", CreateLibrary())
{
    public override ISchemaTable GetTableByName(
        string name,
        SourceMetadataContext metadataContext,
        params object?[] parameters)
    {
        return new WeatherMeasurementTable();
    }

    public override RowSource<T> GetRowSource<T>(
        string name,
        SourceExecutionContext executionContext,
        params object?[] parameters)
    {
        return EnsureSourceType<T, WeatherMeasurementEntity>(name, new WeatherMeasurementRowSource(rows));
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();
        methodsManager.RegisterLibraries(new LibraryBase());
        return new MethodsAggregator(methodsManager);
    }
}
