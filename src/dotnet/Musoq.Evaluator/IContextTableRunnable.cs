using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator;

/// <summary>
///     Optional per-run table execution contract.
/// </summary>
/// <remarks>
///     Implementations receive an immutable snapshot of runtime bindings. This contract is
///     intentionally additive; older runnables continue to use <see cref="ITableRunnable"/>.
/// </remarks>
public interface IContextTableRunnable : ITableRunnable
{
    Table Run(QueryRunContext context);
}
