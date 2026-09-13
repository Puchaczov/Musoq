using Musoq.Parser.Nodes;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private StructuralTypeDescriptor? ResolveStructuralArgumentType(Node node)
    {
        return node switch
        {
            ParameterReferenceNode parameter when _scriptParameters.DefinitionsByName.TryGetValue(parameter.Name, out var definition)
                => definition.Contract.StructuralType,
            ScriptVariableReferenceNode variable when _scriptVariables.DefinitionsByName.TryGetValue(variable.Name, out var variableDefinition)
                => variableDefinition.StructuralType,
            _ => null
        };
    }
}