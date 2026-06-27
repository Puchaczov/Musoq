using System.Collections.Generic;
using System.Threading;

namespace Musoq.Evaluator;

/// <summary>
///     Runtime contract for generated queries that expose typed rows.
/// </summary>
public interface ITypedRunnable<out TOut> : IQueryRunnable
{
    IEnumerable<TOut> Run(TypedQueryRunOptions options);

    IEnumerable<TOut> Run(CancellationToken token);
}
