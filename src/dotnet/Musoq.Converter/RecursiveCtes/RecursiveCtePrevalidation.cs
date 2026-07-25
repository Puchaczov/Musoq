using Musoq.Evaluator;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;

namespace Musoq.Converter;

internal static class RecursiveCtePrevalidation
{
    public static bool TryValidate(Node root, DiagnosticContext diagnostics)
    {
        try
        {
            RecursiveCtePrevalidator.Validate(root);
            return true;
        }
        catch (Exception exception)
        {
            diagnostics.ReportException(exception);
            return false;
        }
    }
}
