using System;
using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.Tests.IR;

internal static class AuthorizedSourcePlanningFixture
{
    public static IReadOnlyDictionary<string, SourceDescriptor> Describe(PhysicalNode plan)
    {
        var descriptors = new Dictionary<string, SourceDescriptor>(StringComparer.Ordinal);
        Add(plan, descriptors);
        return descriptors;
    }

    private static void Add(PhysicalNode node, Dictionary<string, SourceDescriptor> descriptors)
    {
        if (node is PhysicalSchemaScanNode { SourceContextId: { } sourceId } scan)
            descriptors[sourceId] = new SourceDescriptor
            {
                Identity = new SourceIdentity(scan.SchemaName, scan.MethodName, sourceId, scan.Alias),
                ExecutionPermissions = SourceExecutionPermissions.ConcurrentAccess | SourceExecutionPermissions.ConcurrentRowProcessing
            };
        foreach (var child in node.Children)
            Add(child, descriptors);
    }
}
