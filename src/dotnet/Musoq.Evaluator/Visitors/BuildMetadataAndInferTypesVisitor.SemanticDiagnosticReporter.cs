using System;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private sealed class SemanticDiagnosticReporter(DiagnosticContext? diagnosticContext)
    {
        public bool TryReportTypeMismatch(string message, Node node)
        {
            if (diagnosticContext == null)
                return false;

            diagnosticContext.ReportError(DiagnosticCode.MQ3005_TypeMismatch, message, node);
            return true;
        }

        public bool TryReportException(Exception exception, Node? node)
        {
            if (diagnosticContext == null)
                return false;

            diagnosticContext.ReportException(exception, node?.Span);
            return true;
        }
    }
}
