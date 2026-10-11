using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private LocalDeclarationStatementSyntax RenderCreateHashPayload(ExecutionCreateHashPayload createPayload, ExecutionRenderContext context)
    {
        return CreateLocalDeclaration(
            CreateVariableTypeSyntax(createPayload.Payload),
            createPayload.Payload.Name,
            CreateHashPayloadCreation(createPayload.PayloadShape, createPayload.Values, context));
    }

    private ObjectCreationExpressionSyntax CreateHashPayloadCreation(
        HashPayloadShape payloadShape,
        IReadOnlyList<ExecutionRowValue> values,
        ExecutionRenderContext context)
    {
        var fields = GetHashPayloadFields(payloadShape);
        var rowValues = values
            .Select((value, index) => RenderRowConstructorValue(
                value.Value,
                fields[index].Type.RequireClrType(), context));

        return CreateObjectCreation(payloadShape.TypeName, rowValues.ToArray());
    }
}
