using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Musoq.Evaluator.Utils;

namespace Musoq.Targets.CSharpClr;

public sealed class RenderContext
{
    private readonly List<SyntaxNode> _classMembers = [];

    public SyntaxGenerator Generator { get; }

    public IReadOnlyList<SyntaxNode> ClassMembers => _classMembers;

    public Scope? Scope { get; }

    public string AssemblyName { get; }

    public IReadOnlyList<ScriptParameterDefinition> ScriptParameterDefinitions { get; }

    public IReadOnlyList<ScriptVariableDefinition> ScriptVariableDefinitions { get; }

    public QueryInstrumentationMode InstrumentationMode { get; }

    public QueryResultMode ResultMode { get; }

    public Type? OutputType { get; }

    public FinalResultSinkKind FinalResultSinkKind { get; }

    public bool ForceTableResultMaterialization { get; }

    public bool EnableContextualExecution { get; }

    public TableViaRowsResultInfo? TableViaRowsResult { get; private set; }

    public RenderContext(SyntaxGenerator generator, RenderContextOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(generator);
        options ??= new RenderContextOptions();

        Generator = generator;
        Scope = options.Scope;
        AssemblyName = options.AssemblyName;
        ScriptParameterDefinitions = options.ScriptParameterDefinitions ?? Array.Empty<ScriptParameterDefinition>();
        ScriptVariableDefinitions = options.ScriptVariableDefinitions ?? Array.Empty<ScriptVariableDefinition>();
        InstrumentationMode = options.InstrumentationMode;
        ResultMode = options.ResultMode;
        OutputType = options.OutputType;
        FinalResultSinkKind = options.FinalResultSinkKind;
        ForceTableResultMaterialization = options.ForceTableResultMaterialization;
        EnableContextualExecution = options.EnableContextualExecution;
    }

    public void AddClassMember(SyntaxNode member)
    {
        ArgumentNullException.ThrowIfNull(member);
        _classMembers.Add(member);
    }

    public void SetTableViaRowsResult(TableViaRowsResultInfo result)
    {
        TableViaRowsResult = result ?? throw new ArgumentNullException(nameof(result));
    }
}
