using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static MemberDeclarationSyntax RenderFinalSelectShapeClass(FinalShapeResult finalResult,
        FinalOutputPlan? output)
    {
        var sequence = output?.OwnedBuffer?.SequenceField;
        if (output?.RowRepresentation == FinalOutputRowRepresentation.SelectValueShape)
        {
            var shape = RenderRowCarrierStruct(FinalSelectShapeNaming.CreateTypeName(finalResult),
                finalResult.Shape.Fields, GetGeneratedFieldName);
            return sequence == null ? shape : shape.AddMembers(CreateSequenceProperty(sequence));
        }

        var carrier = RenderRowCarrierClass(
            FinalSelectShapeNaming.CreateTypeName(finalResult),
            finalResult.Shape.Fields,
            GetGeneratedFieldName);
        return sequence == null ? carrier : carrier.AddMembers(CreateSequenceProperty(sequence));
    }

    private static MemberDeclarationSyntax CreateSequenceProperty(string name)
        => SyntaxFactory.ParseMemberDeclaration($"public int {name} {{ get; init; }}")!;

    private static bool CanRenderFinalSelectShape(FinalShapeResult finalResult)
    {
        return CanRenderIdentifier(FinalSelectShapeNaming.CreateTypeName(finalResult)) &&
               CanRenderFieldNames(finalResult.Shape.Fields.Select(GetGeneratedFieldName)) &&
               CanRenderFieldTypes(finalResult.Shape.Fields);
    }
}
