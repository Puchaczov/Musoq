using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.Diagnostics;

public static class ExecutionPlanOperatorIdAnnotator
{
    public static string Annotate(ExecutionPlan executionPlan) =>
        ExecutionPlanOperatorCatalog.Create(executionPlan).AnnotatedExecutionPlanText;

    public static string Annotate(string executionPlanText)
        => ExecutionPlanOperatorCatalog.Create(executionPlanText).AnnotatedExecutionPlanText;

    public static IReadOnlyList<OperatorProfileSnapshot> CreateOperatorSnapshots(
        ExecutionPlan executionPlan,
        QueryProfileSnapshot profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(executionPlan);

        return CreateOperatorSnapshots(ExecutionPlanOperatorCatalog.Create(executionPlan), profile);
    }

    public static IReadOnlyList<OperatorProfileSnapshot> CreateOperatorSnapshots(
        ExecutionPlanOperatorCatalog catalog,
        QueryProfileSnapshot profile)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(profile);

        return catalog.Operators
            .Select(descriptor => ResolveActualSnapshot(descriptor, profile) ??
                                  new OperatorProfileSnapshot(
                                      descriptor.Id,
                                      descriptor.NodeKind,
                                      0,
                                      0,
                                      TimeSpan.Zero,
                                      HasActualStats: false))
            .ToArray();
    }

    public static IReadOnlyList<OperatorProfileSnapshot> CreateOperatorSnapshots(
        string annotatedExecutionPlanText,
        QueryProfileSnapshot profile,
        long resultRows)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return CreateOperatorSnapshots(ExecutionPlanOperatorCatalog.Create(RemoveOperatorAnnotations(annotatedExecutionPlanText)), profile);
    }

    private static OperatorProfileSnapshot? ResolveActualSnapshot(
        ExecutionPlanOperatorDescriptor descriptor,
        QueryProfileSnapshot profile)
    {
        return profile.Operators.FirstOrDefault(operation => operation.Id == descriptor.Id);
    }

    private static string RemoveOperatorAnnotations(string annotatedExecutionPlanText)
    {
        if (string.IsNullOrWhiteSpace(annotatedExecutionPlanText))
            return string.Empty;

        return string.Join(
            Environment.NewLine,
            annotatedExecutionPlanText
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n')
                .Select(static line =>
                {
                    var trimmed = line.TrimStart();
                    if (!trimmed.StartsWith("[op", StringComparison.Ordinal))
                        return line;

                    var closeIndex = trimmed.IndexOf(']', StringComparison.Ordinal);
                    if (closeIndex < 0)
                        return line;

                    var indentLength = line.Length - trimmed.Length;
                    return line[..indentLength] + trimmed[(closeIndex + 1)..].TrimStart();
                }));
    }
}
