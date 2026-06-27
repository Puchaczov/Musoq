using System.Threading;
using Musoq.Evaluator.Diagnostics;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator;

public interface IProfiledRunnable
{
    Table RunWithProfile(CancellationToken token, QueryProfileRecorder profileRecorder);
}
