using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private bool CanUseHashJoinHelperSetInCurrentSink(ExecutionCSharpRenderer.HashJoinHelperSet helperSet)
    {
        return !HasCurrentFinalShapeAppendTarget(helperSet.Probe.AppendTargets);
    }

    private bool CanUseKeySetHelperSetInCurrentSink(ExecutionCSharpRenderer.KeySetHelperSet helperSet)
    {
        return !HasCurrentFinalShapeAppendTarget(helperSet.Probe.AppendTargets);
    }

    private bool CanUseSortedCopyHelperInCurrentSink(ExecutionCSharpRenderer.SortedCopyHelper helper)
    {
        return !IsCurrentFinalShapeTargetOrSourceBuffer(helper.Sort.Target.Name);
    }

    private bool CanUseWindowAppendRowsHelperInCurrentSink(ExecutionCSharpRenderer.WindowAppendRowsHelper helper)
    {
        return !HasCurrentFinalShapeAppendTarget(helper.AppendTargets);
    }

    private bool CanUseAggregateFinalizeHelperInCurrentSink(string tableName)
    {
        return !IsCurrentFinalShapeTargetOrSourceBuffer(tableName);
    }

    private bool CanUseParallelFilterProjectHelperInCurrentSink(ExecutionParallelFilterProjectLoop parallelProject)
    {
        return !IsCurrentFinalShapeTargetOrSourceBuffer(parallelProject.AppendRow.Table.Name);
    }

    private bool HasCurrentFinalShapeAppendTarget(IReadOnlyList<ExecutionVariable> appendTargets)
    {
        return appendTargets.Any(target => IsCurrentFinalShapeTargetOrSourceBuffer(target.Name));
    }

    private bool IsCurrentFinalShapeTargetOrSourceBuffer(string tableName)
    {
        return RenderSession.FinalShapeYieldSink is { } sink &&
               (string.Equals(tableName, sink.TableName, StringComparison.Ordinal) ||
                (sink.SourceBuffers?.ContainsKey(tableName) ?? false));
    }

    private bool IsCurrentFinalShapeSourceBuffer(string tableName)
    {
        return RenderSession.FinalShapeYieldSink is { } sink &&
               (sink.SourceBuffers?.ContainsKey(tableName) ?? false);
    }
}
