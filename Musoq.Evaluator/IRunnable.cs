using System.Collections.Generic;
using System.Threading;
using Musoq.Evaluator.Tables;
using Musoq.Schema;

namespace Musoq.Evaluator
{
    public interface IRunnable
    {
        ISchemaProvider Provider { get; set; }
        
        IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> PositionalEnvironmentVariables { get; set; }

        Table Run(CancellationToken token);
    }
}