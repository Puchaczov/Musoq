using Musoq.Schema;
using Musoq.Schema.Exceptions;

namespace Musoq.Examples.DataSources.Csv;

public sealed class CsvSchemaProvider : ISchemaProvider
{
    private readonly CsvDataSourceApiRecorder? _recorder;

    public CsvSchemaProvider()
    {
    }

    internal CsvSchemaProvider(CsvDataSourceApiRecorder recorder)
    {
        _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
    }

    public ISchema GetSchema(string schema)
    {
        _recorder?.SchemaRequests.Add(schema);

        if (string.Equals(schema, CsvSchema.SchemaName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(schema, $"#{CsvSchema.SchemaName}", StringComparison.OrdinalIgnoreCase))
        {
            return new CsvSchema(_recorder);
        }

        throw new SourceNotFoundException($"CSV example schema provider does not expose schema '{schema}'.");
    }
}
