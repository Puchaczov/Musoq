using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private LocalDeclarationStatementSyntax RenderCreateHashPayload(ExecutionCreateHashPayload createPayload)
    {
        return CreateLocalDeclaration(
            CreateVariableTypeSyntax(createPayload.Payload),
            createPayload.Payload.Name,
            CreateHashPayloadCreation(createPayload.PayloadShape, createPayload.Values));
    }

    private ObjectCreationExpressionSyntax CreateHashPayloadCreation(
        HashPayloadShape payloadShape,
        IReadOnlyList<ExecutionRowValue> values)
    {
        var fields = GetHashPayloadFields(payloadShape);
        var rowValues = values
            .Select((value, index) => RenderRowConstructorValue(
                value.Value,
                fields[index].Type));

        return CreateObjectCreation(payloadShape.TypeName, rowValues.ToArray());
    }
}
