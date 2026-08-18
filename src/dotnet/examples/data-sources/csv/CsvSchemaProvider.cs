using Musoq.Schema;
using Musoq.Schema.Exceptions;

namespace Musoq.Examples.DataSources.Csv;

public sealed class CsvSchemaProvider : ISchemaProvider
{
    private readonly CsvDataSourceApiRecorder? _recorder;
    private readonly bool _enableQueryScopedRows;

    public CsvSchemaProvider()
        : this(null, true)
    {
    }

    public CsvSchemaProvider(bool enableQueryScopedRows)
        : this(null, enableQueryScopedRows)
    {
    }

    internal CsvSchemaProvider(CsvDataSourceApiRecorder? recorder, bool enableQueryScopedRows = true)
    {
        _recorder = recorder;
        _enableQueryScopedRows = enableQueryScopedRows;
    }

    public ISchema GetSchema(string schema)
    {
        _recorder?.SchemaRequests.Add(schema);

        if (string.Equals(schema, CsvSchema.SchemaName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(schema, $"#{CsvSchema.SchemaName}", StringComparison.OrdinalIgnoreCase))
        {
            return new CsvSchema(_recorder, _enableQueryScopedRows);
        }

        throw new SourceNotFoundException($"CSV example schema provider does not expose schema '{schema}'.");
    }
}
