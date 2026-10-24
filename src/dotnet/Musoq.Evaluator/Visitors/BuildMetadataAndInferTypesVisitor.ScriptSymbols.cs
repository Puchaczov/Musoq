using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private readonly ScriptParameterMetadataBinder _scriptParameters;
    private readonly ScriptVariableMetadataBinder _scriptVariables;
    private PagingCountBinder? _pagingCounts;
    private PagingCountBinder PagingCounts => _pagingCounts ??=
        new PagingCountBinder(_scriptParameters, _scriptVariables, TryReportScriptParameterError);

    public IReadOnlyList<ScriptParameterDefinition> ScriptParameterDefinitions =>
        _scriptParameters.Definitions.ToArray();

    public IReadOnlyList<ScriptVariableDefinition> ScriptVariableDefinitions =>
        _scriptVariables.Definitions.ToArray();
}
