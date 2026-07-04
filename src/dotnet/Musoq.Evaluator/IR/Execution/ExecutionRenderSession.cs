using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

internal sealed class ExecutionRenderSession
{
    internal Dictionary<ExecutionConstantInSet, string> ConstantInSetFieldNames { get; } = [];
    internal List<ExecutionCSharpRenderer.ConstantInSetField> ConstantInSetFields { get; } = [];
    internal Dictionary<string, string> StaticMetadataFieldNames { get; } = new(StringComparer.Ordinal);
    internal List<ExecutionCSharpRenderer.StaticMetadataField> StaticMetadataFields { get; } = [];
    internal Dictionary<string, string> AggregateGroupTypeNames { get; } = new(StringComparer.Ordinal);
    internal Dictionary<ExecutionParallelFilterProjectLoop, string> ParallelFilterProjectFunctionNames { get; } = [];
    internal Dictionary<string, string> ParallelSingleKeyAggregateFunctionNames { get; } = new(StringComparer.Ordinal);
    internal ExecutionPlanOperatorCatalog OperatorCatalog { get; set; } = ExecutionPlanOperatorCatalog.Create(string.Empty);
    internal bool ProfileRecorderInScope { get; set; }
    internal IReadOnlyDictionary<int, string> StoredRowsCacheNames { get; set; } = new Dictionary<int, string>();
    internal HashSet<int> DeclaredStoredRowsCaches { get; set; } = [];
    internal IReadOnlyDictionary<string, string> ReflectedMemberAccessorNames { get; set; } =
        new Dictionary<string, string>(StringComparer.Ordinal);
    internal IReadOnlyDictionary<string, GeneratedRowShape> TableRowShapesByVariableName { get; set; } =
        new Dictionary<string, GeneratedRowShape>(StringComparer.Ordinal);
    internal IReadOnlyDictionary<int, TypedStoredTableResult> TypedStoredTableResults { get; set; } =
        new Dictionary<int, TypedStoredTableResult>();
    internal IReadOnlyDictionary<string, GeneratedRowShape> TypedRowBufferVariables { get; set; } =
        new Dictionary<string, GeneratedRowShape>(StringComparer.Ordinal);
    internal IReadOnlyDictionary<ExecutionBlock, ExecutionCSharpRenderer.SingleKeyAggregateUpdateHelper> SingleKeyAggregateUpdateHelpersByBlock { get; set; } =
        new Dictionary<ExecutionBlock, ExecutionCSharpRenderer.SingleKeyAggregateUpdateHelper>();
    internal IReadOnlyDictionary<ExecutionBlock, ExecutionCSharpRenderer.EnumerableTraversalHelper> EnumerableTraversalHelpersByBlock { get; set; } =
        new Dictionary<ExecutionBlock, ExecutionCSharpRenderer.EnumerableTraversalHelper>();
    internal HashSet<ExecutionBlock> SuppressedEnumerableTraversalHelperBlocks { get; } = [];
    internal IReadOnlyDictionary<string, IReadOnlySet<ExecutionCSharpRenderer.GeneratedRowContextConstructor>> GeneratedRowConstructorUsagesByType { get; set; } =
        new Dictionary<string, IReadOnlySet<ExecutionCSharpRenderer.GeneratedRowContextConstructor>>(StringComparer.Ordinal);
    internal IReadOnlySet<string> GeneratedRowTypesUsedAsRowContexts { get; set; } = new HashSet<string>(StringComparer.Ordinal);
    internal IReadOnlySet<string> GeneratedRowTypesUsedAtPublicBoundary { get; set; } = new HashSet<string>(StringComparer.Ordinal);
    internal IReadOnlySet<string> GeneratedRowTypesRequiringRowBase { get; set; } = new HashSet<string>(StringComparer.Ordinal);
    internal Dictionary<int, int> StoredGeneratedRowsLoopNameCounts { get; set; } = [];
    internal bool IncludeCteIndexResults { get; set; }
    internal bool IncludeCteRowResults { get; set; }
    internal bool IncludeTableResults { get; set; } = true;
    internal bool UseQueryRunContext { get; set; }
    internal bool EmitChunkLoopCancellationChecks { get; set; } = true;
    internal bool SuppressSingleKeyAggregateUpdateHelpers { get; set; }
    internal FinalShapeYieldSink? FinalShapeYieldSink { get; set; }
}
