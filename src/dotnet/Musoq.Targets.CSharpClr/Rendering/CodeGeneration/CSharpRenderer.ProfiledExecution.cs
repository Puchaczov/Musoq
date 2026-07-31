using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Execution;

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
        TableViaRowsResultInfo resultInfo,
        out MethodDeclarationSyntax rowsAdapterMethod,
        out QueryMethodRenderMetadata metadata)
    {
        if (_context.InstrumentationMode == QueryInstrumentationMode.Disabled)
        {
            if (!TryCreateTableShapeStreamingMethod(
                    plan,
                    unprofiledRenderer,
                    queryIdentifier,
                    shapeRowsMethodName,
                    rowsMethodName,
                    resultInfo,
                    includeProfileRecorderParameter: false,
                    out var shapeRowsMethod,
                    out rowsAdapterMethod,
                    out metadata))
            {
                return false;
            }

            _context.AddClassMember(shapeRowsMethod);
            return true;
        }

        if (!TryCreateTableShapeStreamingMethod(
                plan,
                unprofiledRenderer,
                queryIdentifier,
                shapeRowsMethodName,
                rowsMethodName,
                resultInfo,
                includeProfileRecorderParameter: false,
                out var unprofiledShapeRowsMethod,
                out rowsAdapterMethod,
                out metadata))
        {
            return false;
        }

        var profiledShapeRowsMethodName = QueryMethodNameResolver.ResolveProfiled(shapeRowsMethodName);
        var profiledRowsMethodName = QueryMethodNameResolver.ResolveProfiled(rowsMethodName);
        if (!TryCreateTableShapeStreamingMethod(
                plan,
                profiledRenderer,
                queryIdentifier,
                profiledShapeRowsMethodName,
                profiledRowsMethodName,
                resultInfo,
                includeProfileRecorderParameter: true,
                out var profiledShapeRowsMethod,
                out var profiledRowsAdapterMethod,
                out _))
        {
            return false;
        }

        _context.AddClassMember(unprofiledShapeRowsMethod);
        _context.AddClassMember(profiledShapeRowsMethod);
        _context.AddClassMember(profiledRowsAdapterMethod);
        return true;
    }
}
