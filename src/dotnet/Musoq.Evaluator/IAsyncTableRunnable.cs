using System.Threading;
using System.Threading.Tasks;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator;

/// <summary>
///     Optional asynchronous runtime contract for table-producing queries.
/// </summary>
/// <remarks>
///     Implementations of this contract own the asynchronous operation. A runnable that only
///     implements <see cref="ITableRunnable" /> continues to use that interface's compatibility
///     fallback when invoked through <see cref="CompiledQuery.RunAsync(CancellationToken)" />.
/// </remarks>
public interface IAsyncTableRunnable
{
    ValueTask<Table> RunAsync(CancellationToken token);
}
