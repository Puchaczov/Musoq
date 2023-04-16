using System.Collections.Generic;
using System.Threading;
using Musoq.Parser.Nodes.From;

namespace Musoq.Schema
{
    public class RuntimeContext
    {
        public CancellationToken EndWorkToken { get; }

        public IReadOnlyCollection<ISchemaColumn> AllColumns { get; }
        
        public IReadOnlyDictionary<string, string> EnvironmentVariables { get; }

        public (SchemaFromNode FromNode, IReadOnlyCollection<ISchemaColumn> Columns) QueryInformation { get; }

        public RuntimeContext(CancellationToken endWorkToken, IReadOnlyCollection<ISchemaColumn> originallyInferredColumns, IReadOnlyDictionary<string, string> environmentVariables, (SchemaFromNode FromNode, IReadOnlyCollection<ISchemaColumn> Columns) queryInformation)
        {
            EndWorkToken = endWorkToken;
            AllColumns = originallyInferredColumns;
            EnvironmentVariables = environmentVariables;
            QueryInformation = queryInformation;
        }
    }
}
