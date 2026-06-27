using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Helpers;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal sealed class ScriptVariableMetadataBinder(
    Func<DiagnosticCode, string, Node, bool> reportError,
    Action<Assembly> addAssembly)
{
    private readonly List<ScriptVariableDefinition> _definitions = [];
    private readonly Dictionary<string, ScriptVariableDefinition> _definitionsByName = new(StringComparer.Ordinal);

    public IReadOnlyList<ScriptVariableDefinition> Definitions => _definitions.ToArray();

    public IReadOnlyDictionary<string, ScriptVariableDefinition> DefinitionsByName => _definitionsByName;

    public void TryAddDefinition(
        ScriptVariableDeclarationNode declaration,
        IReadOnlyDictionary<string, ScriptParameterDefinition> scriptParameters)
    {
        if (_definitionsByName.ContainsKey(declaration.Name) || scriptParameters.ContainsKey(declaration.Name))
        {
            var message = $"Script symbol '{declaration.Name}' is declared more than once.";
            if (reportError(DiagnosticCode.MQ3063_DuplicateScriptSymbolName, message, declaration))
                return;

            throw new NotSupportedException(message);
        }

        if (!PrimitiveTypeResolver.TryResolveDeclarationType(declaration.DeclaredTypeName, out var variableType) ||
            !PrimitiveTypeResolver.IsValidQueryExpressionType(variableType))
        {
            var message = $"Script variable '{declaration.Name}' type '{declaration.DeclaredTypeName}' is not supported.";
            if (reportError(DiagnosticCode.MQ3064_UnsupportedScriptVariableType, message, declaration))
                return;

            throw new TypeNotFoundException(
                declaration.DeclaredTypeName,
                "script variable declaration",
                declaration.HasSpan ? declaration.Span : TextSpan.Empty);
        }

        var evaluation = ScriptVariableInitializerEvaluator.Evaluate(
            declaration.Initializer,
            _definitionsByName,
            scriptParameters,
            declaration.Name);

        if (!evaluation.Success)
        {
            var code = evaluation.ErrorCode ?? DiagnosticCode.MQ3065_InvalidScriptVariableInitializer;
            if (reportError(code, evaluation.Error, declaration))
                return;

            throw new NotSupportedException(evaluation.Error);
        }

        var conversion = ScriptValueConverter.ConvertValue(
            "Script variable",
            declaration.Name,
            declaration.DeclaredTypeName,
            variableType,
            evaluation.Value);

        if (!conversion.Success)
        {
            if (reportError(DiagnosticCode.MQ3065_InvalidScriptVariableInitializer, conversion.Error, declaration))
                return;

            throw new NotSupportedException(conversion.Error);
        }

        var definition = new ScriptVariableDefinition(
            declaration.Name,
            variableType,
            conversion.Value,
            CanUseConstKeyword(variableType, conversion.Value));

        _definitions.Add(definition);
        _definitionsByName.Add(declaration.Name, definition);
        addAssembly(variableType.Assembly);
    }

    public bool TryBindReference(ParameterReferenceNode node, out ScriptVariableReferenceNode reference)
    {
        if (_definitionsByName.TryGetValue(node.Name, out var definition))
        {
            addAssembly(definition.VariableType.Assembly);
            reference = new ScriptVariableReferenceNode(node.Name, definition.VariableType, node.Span);
            return true;
        }

        reference = null!;
        return false;
    }

    private static bool CanUseConstKeyword(Type variableType, object? value)
    {
        if (Nullable.GetUnderlyingType(variableType) != null)
            return false;

        if (value == null)
            return variableType == typeof(string);

        return variableType == typeof(string)
               || variableType == typeof(bool)
               || variableType == typeof(char)
               || variableType == typeof(byte)
               || variableType == typeof(sbyte)
               || variableType == typeof(short)
               || variableType == typeof(ushort)
               || variableType == typeof(int)
               || variableType == typeof(uint)
               || variableType == typeof(long)
               || variableType == typeof(ulong)
               || variableType == typeof(float)
               || variableType == typeof(double)
               || variableType == typeof(decimal);
    }
}