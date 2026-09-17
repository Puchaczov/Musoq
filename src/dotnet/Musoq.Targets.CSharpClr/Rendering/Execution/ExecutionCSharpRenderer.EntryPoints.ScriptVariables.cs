using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<StatementSyntax> CreateScriptVariableBindingStatements()
    {
        foreach (var definition in _scriptVariableDefinitions)
            yield return CreateScriptVariableDeclaration(definition);
    }

    private LocalDeclarationStatementSyntax CreateScriptVariableDeclaration(ScriptVariableDefinition definition)
    {
        if (StructuralCarrierSyntaxFactory.TryCreateVariableStorage(
                _scriptVariableDefinitions,
                definition,
                out var structuralType,
                out var structuralInitializer))
        {
            return CreateLocalDeclaration(
                structuralType,
                GetScriptVariableLocalName(definition.Name),
                structuralInitializer);
        }

        var declaration = CreateLocalDeclaration(
            CreateTypeSyntax(definition.VariableType),
            GetScriptVariableLocalName(definition.Name),
            CreateScriptVariableInitializer(definition));

        return definition.CanUseConstKeyword
            ? declaration.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ConstKeyword)))
            : declaration;
    }

    private static ExpressionSyntax CreateScriptVariableInitializer(ScriptVariableDefinition definition)
    {
        return definition.CanUseConstKeyword
            ? RenderLiteral(definition.Value)
            : ScriptParameterSyntaxFactory.CreateDefaultArgumentExpression(definition.VariableType, definition.Value, definition.StructuralType);
    }

    private string GetScriptVariableLocalName(string name)
    {
        if (_scriptVariableLocalNames.TryGetValue(name, out var localName))
            return localName;

        throw new InvalidOperationException(
            $"Script variable '{name}' is not declared in render context.");
    }
}