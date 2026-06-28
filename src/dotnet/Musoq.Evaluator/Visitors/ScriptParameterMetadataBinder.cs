using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Helpers;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

internal sealed class ScriptParameterMetadataBinder(
    Func<DiagnosticCode, string, Node, bool> reportScriptParameterError,
    Action<Assembly> addAssembly)
{
    private readonly List<ScriptParameterDefinition> _definitions = [];
    private readonly Dictionary<string, ScriptParameterDefinition> _definitionsByName =
        new(StringComparer.Ordinal);

    private bool _hasSeenParameterBlock;

    public IReadOnlyList<ScriptParameterDefinition> Definitions => _definitions.ToArray();

    public IReadOnlyDictionary<string, ScriptParameterDefinition> DefinitionsByName => _definitionsByName;

    public bool TryBeginParameterBlock(ParameterBlockNode node, bool hasSeenNonParameterStatement)
    {
        if (_hasSeenParameterBlock)
            return ReportAndSkipParameterBlock(
                DiagnosticCode.MQ3056_DuplicateScriptParameterBlock,
                "Only one parameter block is allowed per script.",
                node);

        if (hasSeenNonParameterStatement)
            return ReportAndSkipParameterBlock(
                DiagnosticCode.MQ3057_ScriptParameterBlockAfterStatement,
                "The parameter block must appear before all query statements.",
                node);

        _hasSeenParameterBlock = true;
        return true;
    }

    public void TryAddDefinition(ParameterDeclarationNode parameter)
    {
        if (_definitionsByName.ContainsKey(parameter.Name))
        {
            var message = $"Script parameter '{parameter.Name}' is declared more than once.";
            if (reportScriptParameterError(DiagnosticCode.MQ3058_DuplicateScriptParameterName, message, parameter))
                return;

            throw new NotSupportedException(message);
        }

        if (!PrimitiveTypeResolver.TryResolveDeclarationType(parameter.DeclaredTypeName, out var parameterType))
        {
            var message = CreateUnsupportedTypeMessage(parameter);
            if (reportScriptParameterError(DiagnosticCode.MQ3060_UnsupportedScriptParameterType, message, parameter))
                return;

            var span = parameter.HasSpan ? parameter.Span : TextSpan.Empty;
            throw new TypeNotFoundException(parameter.DeclaredTypeName, "script parameter declaration", span);
        }

        if (!PrimitiveTypeResolver.IsValidQueryExpressionType(parameterType) &&
            !PrimitiveTypeResolver.IsSupportedCollectionParameterType(parameterType))
        {
            var message = CreateUnsupportedTypeMessage(parameter);
            if (reportScriptParameterError(DiagnosticCode.MQ3060_UnsupportedScriptParameterType, message, parameter))
                return;

            throw new NotSupportedException(message);
        }

        if (!ScriptParameterDefaultValueBinder.TryBind(parameter, parameterType, out var defaultValue, out var error))
        {
            if (reportScriptParameterError(DiagnosticCode.MQ3061_InvalidScriptParameterDefault, error, parameter))
                return;

            throw new NotSupportedException(error);
        }

        var definition = new ScriptParameterDefinition(ScriptParameterContract.Create(
            parameter.Name,
            parameter.DeclaredTypeName,
            parameterType,
            parameter.HasDefaultValue,
            defaultValue));

        _definitions.Add(definition);
        _definitionsByName.Add(parameter.Name, definition);
        addAssembly(parameterType.Assembly);
    }

    public ParameterReferenceNode BindReference(ParameterReferenceNode node)
    {
        if (!_definitionsByName.TryGetValue(node.Name, out var definition))
        {
            var message = $"Script parameter '{node.Name}' is not declared.";
            if (reportScriptParameterError(DiagnosticCode.MQ3059_UndeclaredScriptParameter, message, node))
                return new ParameterReferenceNode(node.Name, typeof(string), node.Span);

            throw new NotSupportedException(message);
        }

        addAssembly(definition.ParameterType.Assembly);
        return new ParameterReferenceNode(node.Name, definition.ParameterType, node.Span);
    }

    public void ValidateSchemaArguments(ArgsListNode args, SchemaFromNode schemaFromNode)
    {
        foreach (var arg in args.Args)
        {
            if (arg is ParameterReferenceNode parameterReference)
            {
                ValidateDirectSchemaArgument(parameterReference, schemaFromNode);
                continue;
            }

            if (!ContainsParameterReference(arg))
                continue;

            var message =
                $"Script parameters in source arguments for '{schemaFromNode.Schema}.{schemaFromNode.Method}' must be passed directly and have a default value.";
            if (reportScriptParameterError(DiagnosticCode.MQ3062_InvalidScriptParameterSourceArgument, message, arg))
                continue;

            throw new NotSupportedException(message);
        }
    }

    private bool ReportAndSkipParameterBlock(DiagnosticCode code, string message, ParameterBlockNode node)
    {
        if (reportScriptParameterError(code, message, node))
            return false;

        throw new NotSupportedException(message);
    }

    private static string CreateUnsupportedTypeMessage(ParameterDeclarationNode parameter)
    {
        if (parameter.DeclaredTypeName.EndsWith("[]?", StringComparison.Ordinal))
            return $"Script parameter '{parameter.Name}' nullable collection type '{parameter.DeclaredTypeName}' is not supported.";

        return $"Script parameter '{parameter.Name}' type '{parameter.DeclaredTypeName}' is not supported.";
    }

    private void ValidateDirectSchemaArgument(
        ParameterReferenceNode parameterReference,
        SchemaFromNode schemaFromNode)
    {
        if (_definitionsByName.TryGetValue(parameterReference.Name, out var definition) &&
            definition.HasDefaultValue)
            return;

        var message =
            $"Script parameter '{parameterReference.Name}' is used in source arguments for '{schemaFromNode.Schema}.{schemaFromNode.Method}' and must declare a default value.";
        if (reportScriptParameterError(DiagnosticCode.MQ3062_InvalidScriptParameterSourceArgument, message, parameterReference))
            return;

        throw new NotSupportedException(message);
    }

    private static bool ContainsParameterReference(Node node)
    {
        return node switch
        {
            null => false,
            ParameterReferenceNode => true,
            ArgsListNode args => args.Args.Any(ContainsParameterReference),
            AccessMethodNode accessMethod => ContainsParameterReference(accessMethod.Arguments),
            DotNode dot => ContainsParameterReference(dot.Root) || ContainsParameterReference(dot.Expression),
            CaseNode caseNode => ContainsParameterReference(caseNode.Else) ||
                                 caseNode.WhenThenPairs.Any(pair =>
                                     ContainsParameterReference(pair.When) || ContainsParameterReference(pair.Then)),
            BinaryNode binary => ContainsParameterReference(binary.Left) || ContainsParameterReference(binary.Right),
            UnaryNode unary => ContainsParameterReference(unary.Expression),
            _ => false
        };
    }
}
