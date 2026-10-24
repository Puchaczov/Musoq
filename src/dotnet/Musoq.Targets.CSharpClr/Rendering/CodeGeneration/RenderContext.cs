using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Musoq.Evaluator.Utils;

namespace Musoq.Targets.CSharpClr;

public sealed class RenderContext
{
    private readonly List<SyntaxNode> _classMembers = [];
    private readonly Dictionary<string, SyntaxNode> _queryRowMembers = new(StringComparer.Ordinal);

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

    public bool IsMetadataOnly { get; }

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
        IsMetadataOnly = options.IsMetadataOnly;
    }

    public void AddClassMember(SyntaxNode member)
    {
        ArgumentNullException.ThrowIfNull(member);
        var sharedName = member switch
        {
            TypeDeclarationSyntax type when type.Identifier.ValueText.StartsWith("QueryRow_", StringComparison.Ordinal) ||
                                            type.Identifier.ValueText.StartsWith("QueryRowMaterializer_", StringComparison.Ordinal)
                => type.Identifier.ValueText,
            FieldDeclarationSyntax { Declaration.Variables.Count: 1 } field when
                field.Declaration.Variables[0].Identifier.ValueText.StartsWith("__queryRowShape_", StringComparison.Ordinal)
                => field.Declaration.Variables[0].Identifier.ValueText,
            _ => null
        };
        if (sharedName != null)
        {
            if (_queryRowMembers.TryGetValue(sharedName, out var existing))
            {
                if (!SyntaxFactory.AreEquivalent(existing, member))
                    throw new InvalidOperationException($"Conflicting query-row declaration '{sharedName}'.");
                return;
            }
            _queryRowMembers.Add(sharedName, member);
        }
        _classMembers.Add(member);
    }

    public void SetTableViaRowsResult(TableViaRowsResultInfo result)
    {
        TableViaRowsResult = result ?? throw new ArgumentNullException(nameof(result));
    }
}
