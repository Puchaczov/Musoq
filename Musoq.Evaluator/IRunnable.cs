using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Musoq.Evaluator.Tables;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Schema;

namespace Musoq.Evaluator;

public interface IRunnable
{
    ISchemaProvider Provider { get; set; }
        
    IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> PositionalEnvironmentVariables { get; set; }
        
    IReadOnlyDictionary<string, (SchemaFromNode FromNode, IReadOnlyCollection<ISchemaColumn> UsedColumns, WhereNode WhereNode, bool HasExternallyProvidedTypes)> QueriesInformation { get; set; }

    Table Run(CancellationToken token);
    
    Task<Table> RunAsync(CancellationToken token) => Task.Run(() => Run(token), token);
}