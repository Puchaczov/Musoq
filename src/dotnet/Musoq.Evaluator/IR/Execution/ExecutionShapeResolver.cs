using System.Collections.Generic;
using Musoq.Evaluator.Utils;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionShapeResolver
{
    private readonly Scope? _scope;
    private readonly IReadOnlyDictionary<string, ISchemaColumn[]> _inferredColumns;
    private readonly IReadOnlyDictionary<string, Type> _entityTypesByAlias;
    private readonly IReadOnlySet<string> _explicitEntityTypeAliases;
    private readonly SchemaRegistry? _schemaRegistry;
    internal NativeScriptValueTypes ScriptValues { get; }
    internal NativeInlineRecordBindings InlineRecords => ScriptValues.InlineRecords;

    public ExecutionShapeResolver(
        Scope? scope = null,
        IReadOnlyDictionary<string, ISchemaColumn[]>? inferredColumns = null,
        IReadOnlyDictionary<string, Type>? entityTypesByAlias = null,
        SchemaRegistry? schemaRegistry = null,
        IReadOnlyList<ScriptVariableDefinition>? scriptVariables = null,
        IReadOnlyList<ScriptParameterDefinition>? scriptParameters = null)
    {
        _scope = scope;
        _inferredColumns = inferredColumns ?? new Dictionary<string, ISchemaColumn[]>(StringComparer.Ordinal);
        _entityTypesByAlias = entityTypesByAlias ?? new Dictionary<string, Type>(StringComparer.Ordinal);
        _explicitEntityTypeAliases = new HashSet<string>(_entityTypesByAlias.Keys, StringComparer.OrdinalIgnoreCase);
        _schemaRegistry = schemaRegistry;
        ScriptValues = new NativeScriptValueTypes(scriptVariables ?? [], scriptParameters ?? []);
    }
}
