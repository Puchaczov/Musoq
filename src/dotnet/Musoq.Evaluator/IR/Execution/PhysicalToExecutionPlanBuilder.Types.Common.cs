using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private sealed record SourcePipeline(PhysicalNode Source, PhysicalFilterNode? Filter);

    private sealed record BuildResult<T>(
        bool Supported,
        T Value,
        string UnsupportedReason)
    {
        public static BuildResult<T> Success(T value)
        {
            return new BuildResult<T>(true, value, string.Empty);
        }

        public static BuildResult<T> Unsupported(string reason)
        {
            return new BuildResult<T>(false, default!, reason);
        }
    }
}
