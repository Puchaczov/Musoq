using System;
using System.Collections.Generic;
using System.Threading;

namespace Musoq.Schema
{
    public class RuntimeContext
    {
        public CancellationToken EndWorkToken { get; }

        public IReadOnlyCollection<ISchemaColumn> OriginallyInferedColumns { get; } 

        public RuntimeContext(CancellationToken endWorkToken, IReadOnlyCollection<ISchemaColumn> originallyInferedColumns = null)
        {
            EndWorkToken = endWorkToken;
            OriginallyInferedColumns = originallyInferedColumns;
        }

        public static RuntimeContext Empty => new RuntimeContext(CancellationToken.None, new ISchemaColumn[0]);
    }
}
