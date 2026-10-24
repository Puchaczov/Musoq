using System.Globalization;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal sealed class PagingCountBinder(
    ScriptParameterMetadataBinder parameters,
    ScriptVariableMetadataBinder variables,
    Func<DiagnosticCode, string, Node, bool> reportError)
{
    internal Node Bind(Node operand, string clause)
    {
        if (operand is ParameterReferenceNode reference && !parameters.DefinitionsByName.ContainsKey(reference.Name))
            return new IntegerNode(0, operand.Span);

        var type = Nullable.GetUnderlyingType(operand.ReturnType!) ?? operand.ReturnType;
        if (!BinaryOperatorTypeRules.IsIntegralType(type!))
            return ReportInvalidPagingCount(operand, clause);

        if (operand is ParameterReferenceNode)
            return operand;

        var value = operand switch
        {
            IntegerNode integer => integer.ObjValue,
            ScriptVariableReferenceNode variable => variables.DefinitionsByName[variable.Name].Value,
            _ => null
        };
        if (!PagingCountBinding.IsInRange(value))
            return ReportInvalidPagingCount(operand, clause);

        return new IntegerNode(Convert.ToInt32(value, CultureInfo.InvariantCulture), operand.Span);
    }

    private Node ReportInvalidPagingCount(Node operand, string clause)
    {
        var message = $"{clause} count '{operand.ToString()}' must be an integral value between 0 and {int.MaxValue}, inclusive; null is not allowed.";
        if (reportError(DiagnosticCode.MQ3118_InvalidPagingCount, message, operand))
            return new IntegerNode(0, operand.Span);
        throw new SemanticAnalysisException(message, Diagnostic.Error(DiagnosticCode.MQ3118_InvalidPagingCount, message, operand.Span));
    }
}
