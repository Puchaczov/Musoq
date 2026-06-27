using System.Threading;
using System.Threading.Tasks;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator;

/// <summary>
///     Runtime contract for generated queries that materialize a table result.
/// </summary>
public interface ITableRunnable : IQueryRunnable
{
    Table Run(CancellationToken token);

    Task<Table> RunAsync(CancellationToken token)
    {
        return Task.Run(() => Run(token), token);
    }
}
