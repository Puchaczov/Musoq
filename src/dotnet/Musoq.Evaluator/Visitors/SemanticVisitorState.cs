using System.Collections.Generic;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private readonly SemanticAnalysisState _semanticState = new();

    private DiagnosticState _diagnostics => _semanticState.Diagnostics;

    private EnumBindingState _enumBinding => _semanticState.Enums;

    private MethodResolutionState _methodResolution => _semanticState.MethodResolution;

    private SemanticQueryState _queryState => _semanticState.Query;

    private ResultShapeState _resultShape => _semanticState.ResultShape;

    private SourceBindingState _sourceBinding => _semanticState.SourceBinding;

    protected Dictionary<string, IReadOnlyDictionary<string, string>> InternalSourceRuntimeSettingsBySourceContextId =>
        _sourceBinding.InternalSourceRuntimeSettingsBySourceContextId;

    protected Dictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> InternalSourceRuntimeSettingDescriptionsBySourceContextId =>
        _sourceBinding.InternalSourceRuntimeSettingDescriptionsBySourceContextId;

    internal bool InsideWindowFunction
    {
        get => _queryState.InsideWindowFunction;
        set => _queryState.InsideWindowFunction = value;
    }
}
