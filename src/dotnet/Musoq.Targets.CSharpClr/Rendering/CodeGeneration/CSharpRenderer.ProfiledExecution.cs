using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class CSharpRenderer
{
    private bool TryCreateTableShapeStreamingMethods(
        ExecutionPlan plan,
        ExecutionCSharpRenderer profiledRenderer,
        ExecutionCSharpRenderer unprofiledRenderer,
        string queryIdentifier,
        string shapeRowsMethodName,
        string rowsMethodName,
        FinalOutputPlan output,
        bool useQueryRunContext,
        out MethodDeclarationSyntax rowsAdapterMethod,
        out QueryMethodRenderMetadata metadata
    )
    {
        if (_context.InstrumentationMode == QueryInstrumentationMode.Disabled)
        {
            if (
                !TryCreateTableShapeStreamingMethod(
                    plan,
                    unprofiledRenderer,
                    queryIdentifier,
                    shapeRowsMethodName,
                    rowsMethodName,
                    output,
                    useQueryRunContext,
                    includeProfileRecorderParameter: false,
                    out var shapeRowsMethod,
                    out rowsAdapterMethod,
                    out metadata
                )
            )
            {
                return false;
            }

            _context.AddClassMember(shapeRowsMethod);
            SetTableRowCarrier(output);
            return true;
        }

        if (
            !TryCreateTableShapeStreamingMethod(
                plan,
                unprofiledRenderer,
                queryIdentifier,
                shapeRowsMethodName,
                rowsMethodName,
                output,
                useQueryRunContext,
                includeProfileRecorderParameter: false,
                out var unprofiledShapeRowsMethod,
                out rowsAdapterMethod,
                out metadata
            )
        )
        {
            return false;
        }

        var profiledShapeRowsMethodName = QueryMethodNameResolver.ResolveProfiled(shapeRowsMethodName);
        var profiledRowsMethodName = QueryMethodNameResolver.ResolveProfiled(rowsMethodName);
        if (
            !TryCreateTableShapeStreamingMethod(
                plan,
                profiledRenderer,
                queryIdentifier,
                profiledShapeRowsMethodName,
                profiledRowsMethodName,
                output,
                useQueryRunContext,
                includeProfileRecorderParameter: true,
                out var profiledShapeRowsMethod,
                out var profiledRowsAdapterMethod,
                out _
            )
        )
        {
            return false;
        }

        _context.AddClassMember(unprofiledShapeRowsMethod);
        _context.AddClassMember(profiledShapeRowsMethod);
        _context.AddClassMember(profiledRowsAdapterMethod);
        SetTableRowCarrier(output);
        return true;
    }

    private void SetTableRowCarrier(FinalOutputPlan output)
    {
        if (output.ReturnsOwnedRows)
            _context.SetTableViaRowsResult(output.Result with { RowTypeName = output.TableRowTypeName });
    }
}
