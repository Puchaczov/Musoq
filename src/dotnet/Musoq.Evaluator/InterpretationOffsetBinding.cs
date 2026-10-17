using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator;

internal static class InterpretationOffsetBinding
{
    internal static void RequireInt(Node offset)
    {
        if (offset.ReturnType == typeof(int) || offset.ReturnType == typeof(short) ||
            offset.ReturnType == typeof(ushort) || offset.ReturnType == typeof(byte) || offset.ReturnType == typeof(sbyte))
            return;
        throw new QuerySyntaxException(
            "InterpretAt requires a non-nullable integer offset that converts to int without loss. Convert wider numeric types explicitly and supply a NULL default when needed.",
            offset.SpanOrEmpty(), DiagnosticCode.MQ3005_TypeMismatch);
    }
}
