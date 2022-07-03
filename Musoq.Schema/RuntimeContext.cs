using System.Collections.Generic;
using System.Threading;

namespace Musoq.Schema
{
    public class RuntimeContext
    {
        public CancellationToken EndWorkToken { get; }

        public IReadOnlyCollection<ISchemaColumn> AllColumns { get; }

        public IReadOnlyCollection<ISchemaColumn> UsedColumns { get; }

        public RuntimeContext(CancellationToken endWorkToken, IReadOnlyCollection<ISchemaColumn> originallyInferedColumns, IReadOnlyCollection<ISchemaColumn> usedColumns = null)
        {
            EndWorkToken = endWorkToken;
            AllColumns = originallyInferedColumns;
            UsedColumns = usedColumns;
        }

        public static RuntimeContext Empty => new(CancellationToken.None, new ISchemaColumn[0], new ISchemaColumn[0]);
    }
}
