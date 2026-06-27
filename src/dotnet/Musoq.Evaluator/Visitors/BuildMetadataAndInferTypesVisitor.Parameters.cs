using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(ParameterBlockNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var parameters = PopParameterDeclarations(node);
        if (_scriptParameters.TryBeginParameterBlock(node, _diagnostics.HasSeenNonParameterStatement))
        {
            foreach (var parameter in parameters)
                _scriptParameters.TryAddDefinition(parameter);
        }

        Nodes.Push(new ParameterBlockNode(parameters, node.Span));
    }

    public override void Visit(ParameterDeclarationNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var defaultValue = node.HasDefaultValue
            ? SafePop(Nodes, "Visit(ParameterDeclarationNode).DefaultValue")
            : null;

        Nodes.Push(new ParameterDeclarationNode(node.Name, node.TypeName, node.IsNullable, defaultValue, node.Span));
    }

    public override void Visit(ParameterReferenceNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (_scriptVariables.TryBindReference(node, out var variableReference))
        {
            Nodes.Push(variableReference);
            return;
        }

        Nodes.Push(_scriptParameters.BindReference(node));
    }

    public override void Visit(ScriptVariableDeclarationNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _scriptVariables.TryAddDefinition(node, _scriptParameters.DefinitionsByName);
        Nodes.Push(new ScriptVariableDeclarationNode(node.Name, node.TypeName, node.IsNullable, node.Initializer, node.Span));
    }

    public override void Visit(ScriptVariableReferenceNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new ScriptVariableReferenceNode(node.Name, node.ReturnType, node.Span));
    }

    private ParameterDeclarationNode[] PopParameterDeclarations(ParameterBlockNode node)
    {
        var parameters = new ParameterDeclarationNode[node.Parameters.Length];

        for (var i = node.Parameters.Length - 1; i >= 0; --i)
            parameters[i] = SafeCast<ParameterDeclarationNode>(
                SafePop(Nodes, "Visit(ParameterBlockNode).Parameter"),
                "Visit(ParameterBlockNode).Parameter");

        return parameters;
    }

    private bool TryReportScriptParameterError(DiagnosticCode code, string message, Node node)
    {
        if (DiagnosticContext == null)
            return false;

        DiagnosticContext.ReportError(code, message, node);
        return true;
    }
}
