using System.Threading.Tasks;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator;

/// <summary>
///     Optional asynchronous per-run table execution contract.
/// </summary>
public interface IContextAsyncTableRunnable : IContextTableRunnable
{
    ValueTask<Table> RunAsync(QueryRunContext context);
}
