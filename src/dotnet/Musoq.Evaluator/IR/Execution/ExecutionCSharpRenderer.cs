using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{

    private const string DescSchemaVariableName = "descSchema";
    private const string DescRuntimeContextVariableName = "descRuntimeCtx";
    private const string DescEmptyInferredColumnsVariableName = "emptyInferred";
    private const string DescSchemaTableVariableName = "schemaTable";
    private const string StatsVariableName = "stats";
    private const string ProfileRecorderVariableName = "profileRecorder";
    private readonly Dictionary<ExecutionConstantInSet, string> _constantInSetFieldNames = [];
    private readonly List<ConstantInSetField> _constantInSetFields = [];
    private readonly Dictionary<string, string> _staticMetadataFieldNames = new(StringComparer.Ordinal);
    private readonly List<StaticMetadataField> _staticMetadataFields = [];
    private readonly Dictionary<string, string> _aggregateGroupTypeNames = new(StringComparer.Ordinal);
    private readonly Dictionary<ExecutionParallelFilterProjectLoop, string> _parallelFilterProjectFunctionNames = [];
    private readonly Dictionary<string, string> _parallelSingleKeyAggregateFunctionNames = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _serialSingleKeyAggregateFunctionNames = new(StringComparer.Ordinal);
    private readonly IReadOnlyList<ScriptParameterDefinition> _scriptParameterDefinitions;
    private readonly IReadOnlyList<ScriptVariableDefinition> _scriptVariableDefinitions;
    private readonly IReadOnlyDictionary<string, string> _scriptParameterLocalNames;
    private readonly IReadOnlyDictionary<string, string> _scriptVariableLocalNames;
    private readonly QueryInstrumentationMode _instrumentationMode;
    private ExecutionPlanOperatorCatalog _operatorCatalog = ExecutionPlanOperatorCatalog.Create(string.Empty);
    private bool _profileRecorderInScope;
    private IReadOnlyDictionary<int, string> _storedRowsCacheNames = new Dictionary<int, string>();
    private HashSet<int> _declaredStoredRowsCaches = [];
    private IReadOnlyDictionary<string, string> _reflectedMemberAccessorNames = new Dictionary<string, string>(StringComparer.Ordinal);
    private IReadOnlyDictionary<string, GeneratedRowShape> _tableRowShapesByVariableName =
        new Dictionary<string, GeneratedRowShape>(StringComparer.Ordinal);
    private IReadOnlyDictionary<int, TypedStoredTableResult> _typedStoredTableResults =
        new Dictionary<int, TypedStoredTableResult>();
    private IReadOnlyDictionary<string, GeneratedRowShape> _typedRowBufferVariables =
        new Dictionary<string, GeneratedRowShape>(StringComparer.Ordinal);
    private IReadOnlyDictionary<ExecutionBlock, SingleKeyAggregateUpdateHelper> _singleKeyAggregateUpdateHelpersByBlock =
        new Dictionary<ExecutionBlock, SingleKeyAggregateUpdateHelper>();
    private IReadOnlyDictionary<ExecutionBlock, EnumerableTraversalHelper> _enumerableTraversalHelpersByBlock =
        new Dictionary<ExecutionBlock, EnumerableTraversalHelper>();
    private readonly HashSet<ExecutionBlock> _suppressedEnumerableTraversalHelperBlocks = [];
    private IReadOnlyDictionary<string, IReadOnlySet<GeneratedRowContextConstructor>> _generatedRowConstructorUsagesByType =
        new Dictionary<string, IReadOnlySet<GeneratedRowContextConstructor>>(StringComparer.Ordinal);
    private IReadOnlySet<string> _generatedRowTypesUsedAsRowContexts = new HashSet<string>(StringComparer.Ordinal);
    private IReadOnlySet<string> _generatedRowTypesUsedAtPublicBoundary = new HashSet<string>(StringComparer.Ordinal);
    private IReadOnlySet<string> _generatedRowTypesRequiringRowBase = new HashSet<string>(StringComparer.Ordinal);
    private Dictionary<int, int> _storedGeneratedRowsLoopNameCounts = [];
    private bool _includeCteIndexResults;
    private bool _includeCteRowResults;
    private bool _includeTableResults = true;
    private bool _useQueryRunContext;
    private bool _emitChunkLoopCancellationChecks = true;
    private bool _suppressSingleKeyAggregateUpdateHelpers;
    private FinalShapeYieldSink? _finalShapeYieldSink;

    private bool IsInstrumentationEnabled => _instrumentationMode != QueryInstrumentationMode.Disabled;

    private bool IsFullInstrumentationEnabled => _instrumentationMode == QueryInstrumentationMode.Full;

    private bool IsOperatorProfilingEnabled => IsFullInstrumentationEnabled && _profileRecorderInScope;

    internal bool IsFullProfilingEnabledForGeneratedCode => IsFullInstrumentationEnabled;

}
