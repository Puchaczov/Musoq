using System.Collections.Generic;
using Musoq.Evaluator.Tests.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Schema.DataSourceProgress;

public class ReportingSchemaProvider<T>(IDictionary<string, IEnumerable<T>> values) : ISchemaProvider
    where T : BasicEntity
{
    public ISchema GetSchema(string schema)
    {
        if (!values.TryGetValue(schema, out var value))
            throw new SchemaNotFoundException();

        return new ReportingSchema<T>(schema, value);
    }
}
