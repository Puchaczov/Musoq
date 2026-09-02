using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static ClassDeclarationSyntax RenderFinalSelectShapeClass(FinalShapeResult finalResult)
    {
        return RenderRowCarrierClass(
            FinalSelectShapeNaming.CreateTypeName(finalResult),
            finalResult.Shape.Fields,
            GetGeneratedFieldName);
    }

    private static bool CanRenderFinalSelectShape(FinalShapeResult finalResult)
    {
        return CanRenderIdentifier(FinalSelectShapeNaming.CreateTypeName(finalResult)) &&
               CanRenderFieldNames(finalResult.Shape.Fields.Select(GetGeneratedFieldName)) &&
               CanRenderFieldTypes(finalResult.Shape.Fields);
    }
}
