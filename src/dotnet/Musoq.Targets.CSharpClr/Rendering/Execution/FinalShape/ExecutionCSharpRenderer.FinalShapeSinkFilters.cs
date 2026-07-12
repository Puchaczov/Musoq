using System.Collections.Generic;
using System.Linq;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private bool CanUseHashJoinHelperSetInCurrentSink(
        ExecutionCSharpRenderer.HashJoinHelperSet helperSet,
        ExecutionRenderContext context)
    {
        return !HasCurrentFinalShapeAppendTarget(helperSet.Probe.AppendTargets, context);
    }

    private bool CanUseKeySetHelperSetInCurrentSink(
        ExecutionCSharpRenderer.KeySetHelperSet helperSet,
        ExecutionRenderContext context)
    {
        return !HasCurrentFinalShapeAppendTarget(helperSet.Probe.AppendTargets, context);
    }

    private bool CanUseSortedCopyHelperInCurrentSink(
        ExecutionCSharpRenderer.SortedCopyHelper helper,
        ExecutionRenderContext context)
    {
        return !IsCurrentFinalShapeTargetOrSourceBuffer(helper.Sort.Target.Name, context);
    }

    private bool CanUseWindowAppendRowsHelperInCurrentSink(
        ExecutionCSharpRenderer.WindowAppendRowsHelper helper,
        ExecutionRenderContext context)
    {
        return !HasCurrentFinalShapeAppendTarget(helper.AppendTargets, context);
    }

    private bool CanUseAggregateFinalizeHelperInCurrentSink(
        string tableName,
        ExecutionRenderContext context)
    {
        return !IsCurrentFinalShapeTargetOrSourceBuffer(tableName, context);
    }

    private bool CanUseParallelFilterProjectHelperInCurrentSink(
        ExecutionParallelFilterProjectLoop parallelProject,
        ExecutionRenderContext context)
    {
        return !IsCurrentFinalShapeTargetOrSourceBuffer(parallelProject.AppendRow.Table.Name, context);
    }

    private bool HasCurrentFinalShapeAppendTarget(
        IReadOnlyList<ExecutionVariable> appendTargets,
        ExecutionRenderContext context)
    {
        return appendTargets.Any(target => IsCurrentFinalShapeTargetOrSourceBuffer(target.Name, context));
    }

    private static bool IsCurrentFinalShapeTargetOrSourceBuffer(
        string tableName,
        ExecutionRenderContext context)
    {
        return context.Session.FinalShapeYieldSink is { } sink &&
               (string.Equals(tableName, sink.TableName, StringComparison.Ordinal) ||
                (sink.SourceBuffers?.ContainsKey(tableName) ?? false));
    }

    private static bool IsCurrentFinalShapeSourceBuffer(
        string tableName,
        ExecutionRenderContext context)
    {
        return context.Session.FinalShapeYieldSink is { } sink &&
               (sink.SourceBuffers?.ContainsKey(tableName) ?? false);
    }
}
