using System.Collections.Generic;
using System.Threading;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Schema;

public class RuntimeContext(
    CancellationToken endWorkToken,
    IReadOnlyCollection<ISchemaColumn> originallyInferredColumns,
    IReadOnlyDictionary<string, string> environmentVariables,
    (SchemaFromNode FromNode, IReadOnlyCollection<ISchemaColumn> Columns, WhereNode WhereNode) queryInformation)
{
    public CancellationToken EndWorkToken { get; } = endWorkToken;

    public IReadOnlyCollection<ISchemaColumn> AllColumns { get; } = originallyInferredColumns;

    public IReadOnlyDictionary<string, string> EnvironmentVariables { get; } = environmentVariables;

    public (SchemaFromNode FromNode, IReadOnlyCollection<ISchemaColumn> Columns, WhereNode WhereNode) QueryInformation { get; } = queryInformation;
}