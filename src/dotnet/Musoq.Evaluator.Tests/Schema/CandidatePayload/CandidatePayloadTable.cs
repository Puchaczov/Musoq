using System;
using System.Linq;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.CandidatePayload;

public sealed class CandidatePayloadTable : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } =
    [
        new SchemaColumn(nameof(CandidatePayloadEntity.Id), 0, typeof(int)),
        new SchemaColumn(nameof(CandidatePayloadEntity.Path), 1, typeof(string)),
        new SchemaColumn(nameof(CandidatePayloadEntity.Payload), 2, typeof(string))
    ];

    public SchemaTableMetadata Metadata { get; } = new(typeof(CandidatePayloadEntity));

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(column => column.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(column => column.ColumnName == name).ToArray();
    }
}
