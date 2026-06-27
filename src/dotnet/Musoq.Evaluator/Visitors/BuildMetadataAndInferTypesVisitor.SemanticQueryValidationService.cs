using System;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private sealed class SemanticQueryValidationService(SemanticDiagnosticReporter diagnosticReporter)
    {
        public void ValidateExpressionIsBoolean(Node expression, string context)
        {
            var expressionType = BinaryOperatorTypeRules.NormalizeOperandType(expression.ReturnType);
            if (BinaryOperatorTypeRules.CanSkipStaticTypeValidation(expressionType))
                return;

            if (expressionType == typeof(bool))
                return;

            var message = CreateBooleanContextTypeMismatchMessage(expression, expressionType, context);

            if (diagnosticReporter.TryReportTypeMismatch(message, expression))
                return;

            throw new TypeMismatchException(
                typeof(bool),
                expressionType,
                expression.HasSpan ? expression.Span : TextSpan.Empty);
        }
    }
}
