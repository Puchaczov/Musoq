using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static ClassDeclarationSyntax RenderFinalSelectShapeClass(FinalShapeResult finalResult)
    {
        return ExecutionCSharpRenderer.RenderRowCarrierClass(
            FinalSelectShapeNaming.CreateTypeName(finalResult),
            finalResult.Shape.Fields,
            ExecutionCSharpRenderer.GetGeneratedFieldName);
    }

    private static bool CanRenderFinalSelectShape(FinalShapeResult finalResult)
    {
        return ExecutionCSharpRenderer.CanRenderIdentifier(FinalSelectShapeNaming.CreateTypeName(finalResult)) &&
               ExecutionCSharpRenderer.CanRenderFieldNames(finalResult.Shape.Fields.Select(ExecutionCSharpRenderer.GetGeneratedFieldName)) &&
               ExecutionCSharpRenderer.CanRenderFieldTypes(finalResult.Shape.Fields);
    }
}
