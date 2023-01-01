using System.Collections.Generic;
using System.Threading;

namespace Musoq.Schema
{
    public class RuntimeContext
    {
        public CancellationToken EndWorkToken { get; }

        public IReadOnlyCollection<ISchemaColumn> AllColumns { get; }
        
        public IReadOnlyDictionary<string, string> EnvironmentVariables { get; }

        public IReadOnlyCollection<ISchemaColumn> UsedColumns { get; }

        public RuntimeContext(CancellationToken endWorkToken, IReadOnlyCollection<ISchemaColumn> originallyInferredColumns, IReadOnlyDictionary<string, string> environmentVariables, IReadOnlyCollection<ISchemaColumn> usedColumns = null)
        {
            EndWorkToken = endWorkToken;
            AllColumns = originallyInferredColumns;
            EnvironmentVariables = environmentVariables;
            UsedColumns = usedColumns;
        }
    }
}
