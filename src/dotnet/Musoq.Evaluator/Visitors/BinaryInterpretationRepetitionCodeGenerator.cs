using System.Globalization;
using System.Linq;
using System.Text;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private string GenerateRepeatUntilReadCode(
        string localVar,
        RepeatUntilTypeNode repeatUntilType,
        string fieldName,
        bool declareLocal = true)
    {
        return repeatUntilType.StopKind == RepeatUntilStopKind.EndOfInput
            ? GenerateRepeatUntilEofReadCode(localVar, repeatUntilType, fieldName, declareLocal)
            : GenerateRepeatUntilConditionReadCode(localVar, repeatUntilType, fieldName, declareLocal);
    }

    private string GenerateRepeatUntilConditionReadCode(
        string localVar,
        RepeatUntilTypeNode repeatUntilType,
        string fieldName,
        bool declareLocal)
    {
        var builder = new StringBuilder();
        var elementTypeName = GetBinaryValueClrTypeName(fieldName, repeatUntilType.ElementType);
        var tempVar = $"_{localVar}_list";
        var lastElemVar = $"_{localVar}_lastElem";
        var iterationVar = $"_{localVar}_iteration";

        builder.AppendLine(CultureInfo.InvariantCulture,
            $"var {tempVar} = new System.Collections.Generic.List<{elementTypeName}>();");
        builder.AppendLine(CultureInfo.InvariantCulture, $"{elementTypeName} {lastElemVar};");
        builder.AppendLine(CultureInfo.InvariantCulture, $"var {iterationVar} = 0;");
        builder.AppendLine("do");
        builder.AppendLine("{");
        builder.AppendLine(CultureInfo.InvariantCulture,
            $"    EnsureRepeatIteration(\"{EscapeString(fieldName)}\", {iterationVar}++, {_maxRepeatIterations});");

        AppendRepeatUntilElementRead(
            builder,
            repeatUntilType.ElementType,
            fieldName,
            localVar,
            tempVar,
            lastElemVar);

        var conditionExpr = GenerateRepeatUntilCondition(
            repeatUntilType,
            lastElemVar,
            tempVar);
        builder.AppendLine(CultureInfo.InvariantCulture, $"}} while (({conditionExpr}) != true);");

        AppendRepeatResultAssignment(builder, localVar, tempVar, declareLocal);
        return builder.ToString();
    }

    private string GenerateRepeatUntilEofReadCode(
        string localVar,
        RepeatUntilTypeNode repeatUntilType,
        string fieldName,
        bool declareLocal)
    {
        var builder = new StringBuilder();
        var elementTypeName = GetBinaryValueClrTypeName(fieldName, repeatUntilType.ElementType);
        var tempVar = $"_{localVar}_list";
        var lastElemVar = $"_{localVar}_lastElem";
        var startPosVar = $"_{localVar}_startPos";
        var startBitVar = $"_{localVar}_startBit";
        var iterationVar = $"_{localVar}_iteration";

        builder.AppendLine(CultureInfo.InvariantCulture,
            $"var {tempVar} = new System.Collections.Generic.List<{elementTypeName}>();");
        builder.AppendLine(CultureInfo.InvariantCulture, $"{elementTypeName} {lastElemVar};");
        builder.AppendLine(CultureInfo.InvariantCulture, $"var {iterationVar} = 0;");
        builder.AppendLine("while (!IsAtEnd(data))");
        builder.AppendLine("{");
        builder.AppendLine(CultureInfo.InvariantCulture,
            $"    EnsureRepeatIteration(\"{EscapeString(fieldName)}\", {iterationVar}++, {_maxRepeatIterations});");
        builder.AppendLine(CultureInfo.InvariantCulture, $"    var {startPosVar} = ParsePosition;");
        builder.AppendLine(CultureInfo.InvariantCulture, $"    var {startBitVar} = BitOffset;");

        AppendRepeatUntilElementRead(
            builder,
            repeatUntilType.ElementType,
            fieldName,
            localVar,
            tempVar,
            lastElemVar);

        builder.AppendLine(CultureInfo.InvariantCulture,
            $"    EnsureRepeatMadeProgress(\"{EscapeString(fieldName)}\", {startPosVar}, {startBitVar});");
        builder.AppendLine("}");

        AppendRepeatResultAssignment(builder, localVar, tempVar, declareLocal);
        return builder.ToString();
    }

    private static void AppendRepeatResultAssignment(
        StringBuilder builder,
        string localVar,
        string tempVar,
        bool declareLocal)
    {
        var target = declareLocal ? $"var {localVar}" : localVar;
        builder.AppendLine(CultureInfo.InvariantCulture, $"{target} = {tempVar}.ToArray();");
    }

    private void AppendRepeatUntilElementRead(
        StringBuilder builder,
        TypeAnnotationNode elementType,
        string fieldName,
        string localVar,
        string tempVar,
        string lastElemVar)
    {
        var elementField = new FieldDefinitionNode(fieldName, elementType);
        var readCode = GenerateFieldReadCodeInner(elementField, lastElemVar);
        builder.Append(Indent(readCode, 1));
        builder.AppendLine(CultureInfo.InvariantCulture, $"    {tempVar}.Add({lastElemVar});");
    }

    private string GenerateRepeatUntilCondition(RepeatUntilTypeNode repeat, string lastElemVar, string listVar)
    {
        var fields = _expressionFields;
        var binding = _repeatExpressionBinding;
        _expressionFields = fields.Prepend(new FieldDefinitionNode(repeat.FieldName, repeat)).ToArray();
        _repeatExpressionBinding = new RepeatExpressionBinding(repeat.FieldName, lastElemVar, listVar);
        try
        {
            BinarySchemaExpressionTypes.Resolve(repeat.Condition!, _expressionFields, _registry);
            return GenerateConditionExpression(repeat.Condition!);
        }
        finally
        {
            _expressionFields = fields;
            _repeatExpressionBinding = binding;
        }
    }
}
