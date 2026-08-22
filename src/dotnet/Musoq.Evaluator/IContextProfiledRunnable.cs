using Musoq.Evaluator.Diagnostics;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator;

/// <summary>
///     Optional profiling contract that receives the complete per-run context.
/// </summary>
public interface IContextProfiledRunnable
{
    Table RunWithProfile(QueryRunContext context, QueryProfileRecorder profileRecorder);
}
