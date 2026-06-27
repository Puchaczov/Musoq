using System.Text;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private static string GenerateSchemaReferenceReadCode(string localVar, SchemaReferenceTypeNode schemaRef)
    {
        var typeName = schemaRef.FullTypeName;
        var tempInterpreter = $"_{localVar}_interpreter";
        var builder = new StringBuilder();
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {tempInterpreter} = new {typeName}();");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {localVar} = {tempInterpreter}.InterpretAt(data, ParsePosition);");
        builder.Append(System.Globalization.CultureInfo.InvariantCulture, $"ParsePosition = {tempInterpreter}.BytesConsumed;");
        return builder.ToString();
    }

    private string GenerateArrayReadCode(string localVar, string fieldName, ArrayTypeNode arrayType)
    {
        var builder = new StringBuilder();
        var countExpr = GenerateSizeExpression(arrayType.SizeExpression);
        var elementTypeName = GetArrayElementClrTypeName(fieldName, arrayType.ElementType);
        var loopVar = $"_{localVar}_i";
        var tempVar = $"_{localVar}_list";

        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {tempVar} = new System.Collections.Generic.List<{elementTypeName}>();");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"for (int {loopVar} = 0; {loopVar} < {countExpr}; {loopVar}++)");
        builder.AppendLine("{");

        if (arrayType.ElementType is PrimitiveTypeNode primitiveElement)
        {
            var readMethod = GetPrimitiveReadMethod(primitiveElement);
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    {tempVar}.Add({readMethod}(data));");
        }
        else if (arrayType.ElementType is StringTypeNode stringElement)
        {
            var elemVar = $"_{localVar}_elem";
            builder.Append(Indent(GenerateStringDeclarationCode(elemVar, stringElement), 1));
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    {tempVar}.Add({elemVar});");
        }
        else if (arrayType.ElementType is SchemaReferenceTypeNode schemaRefElement)
        {
            var typeName = schemaRefElement.FullTypeName;
            var tempInterpreter = $"_{localVar}_elemInterpreter";
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    var {tempInterpreter} = new {typeName}();");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    var _elem = {tempInterpreter}.InterpretAt(data, ParsePosition);");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    ParsePosition = {tempInterpreter}.BytesConsumed;");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    {tempVar}.Add(_elem);");
        }
        else if (arrayType.ElementType is InlineSchemaTypeNode inlineSchemaElement)
        {
            AppendInlineArrayElementRead(
                builder,
                inlineSchemaElement,
                fieldName,
                tempVar,
                1);
            var elemVar = GetInlineArrayElementVariable(tempVar);
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    {tempVar}.Add({elemVar});");
        }
        else
        {
            throw CreateUnsupportedCodeGenerationException(
                fieldName,
                arrayType.ElementType,
                "array element type");
        }

        builder.AppendLine("}");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {localVar} = {tempVar}.ToArray();");

        return builder.ToString();
    }

    private void AppendInlineArrayElementRead(
        StringBuilder builder,
        InlineSchemaTypeNode inlineSchema,
        string fieldName,
        string localVar,
        int indentationLevel)
    {
        var indent = new string(' ', indentationLevel * 4);
        var inlineClassName = GetOrRegisterInlineSchemaClassName(fieldName, inlineSchema, null);
        var tempInterpreter = GetInlineArrayElementInterpreterVariable(localVar);
        var elemVar = GetInlineArrayElementVariable(localVar);

        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{indent}var {tempInterpreter} = new {inlineClassName}();");

        var outerAssignments = GenerateOuterRefAssignments(tempInterpreter, inlineSchema);
        if (!string.IsNullOrEmpty(outerAssignments))
            builder.AppendLine(Indent(outerAssignments, indentationLevel));

        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{indent}var {elemVar} = {tempInterpreter}.InterpretAt(data, ParsePosition);");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{indent}ParsePosition = {tempInterpreter}.BytesConsumed;");
    }

    private static string GetInlineArrayElementInterpreterVariable(string localVar)
    {
        return $"_{localVar}_elemInterpreter";
    }

    private static string GetInlineArrayElementVariable(string localVar)
    {
        return $"_{localVar}_elem";
    }

    private string GenerateRepeatUntilReadCode(string localVar, RepeatUntilTypeNode repeatUntilType, string fieldName)
    {
        return repeatUntilType.StopKind == RepeatUntilStopKind.EndOfInput
            ? GenerateRepeatUntilEofReadCode(localVar, repeatUntilType, fieldName)
            : GenerateRepeatUntilConditionReadCode(localVar, repeatUntilType, fieldName);
    }

    private string GenerateRepeatUntilConditionReadCode(string localVar, RepeatUntilTypeNode repeatUntilType, string fieldName)
    {
        var builder = new StringBuilder();
        var elementTypeName = GetRepeatUntilElementClrTypeName(fieldName, repeatUntilType.ElementType);
        var tempVar = $"_{localVar}_list";
        var lastElemVar = $"_{localVar}_lastElem";

        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {tempVar} = new System.Collections.Generic.List<{elementTypeName}>();");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{elementTypeName} {lastElemVar};");
        builder.AppendLine("do");
        builder.AppendLine("{");

        AppendRepeatUntilElementRead(builder, repeatUntilType.ElementType, fieldName, localVar, tempVar, lastElemVar);

        var conditionExpr = GenerateRepeatUntilCondition(repeatUntilType.Condition!, fieldName, lastElemVar, tempVar);
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"}} while (!({conditionExpr}));");

        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {localVar} = {tempVar}.ToArray();");

        return builder.ToString();
    }

    private string GenerateRepeatUntilEofReadCode(string localVar, RepeatUntilTypeNode repeatUntilType, string fieldName)
    {
        var builder = new StringBuilder();
        var elementTypeName = GetRepeatUntilElementClrTypeName(fieldName, repeatUntilType.ElementType);
        var tempVar = $"_{localVar}_list";
        var lastElemVar = $"_{localVar}_lastElem";
        var startPosVar = $"_{localVar}_startPos";
        var startBitVar = $"_{localVar}_startBit";

        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {tempVar} = new System.Collections.Generic.List<{elementTypeName}>();");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{elementTypeName} {lastElemVar};");
        builder.AppendLine("while (!IsAtEnd(data))");
        builder.AppendLine("{");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    var {startPosVar} = ParsePosition;");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    var {startBitVar} = BitOffset;");

        AppendRepeatUntilElementRead(builder, repeatUntilType.ElementType, fieldName, localVar, tempVar, lastElemVar);

        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    EnsureRepeatMadeProgress(\"{fieldName}\", {startPosVar}, {startBitVar});");
        builder.AppendLine("}");

        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {localVar} = {tempVar}.ToArray();");

        return builder.ToString();
    }

    private void AppendRepeatUntilElementRead(
        StringBuilder builder,
        TypeAnnotationNode elementType,
        string fieldName,
        string localVar,
        string tempVar,
        string lastElemVar)
    {
        if (elementType is PrimitiveTypeNode primitiveElement)
        {
            var readMethod = GetPrimitiveReadMethod(primitiveElement);
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    {lastElemVar} = {readMethod}(data);");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    {tempVar}.Add({lastElemVar});");
            return;
        }

        if (elementType is BitsTypeNode bitsElement)
        {
            var bitsTypeName = GetBitsClrTypeName(bitsElement);
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    {lastElemVar} = ({bitsTypeName})ReadBits(data, {bitsElement.BitCount});");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    {tempVar}.Add({lastElemVar});");
            return;
        }

        if (elementType is SchemaReferenceTypeNode schemaRefElement)
        {
            var typeName = schemaRefElement.FullTypeName;
            var tempInterpreter = $"_{localVar}_elemInterpreter";
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    var {tempInterpreter} = new {typeName}();");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    {lastElemVar} = {tempInterpreter}.InterpretAt(data, ParsePosition);");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    ParsePosition = {tempInterpreter}.BytesConsumed;");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    {tempVar}.Add({lastElemVar});");
            return;
        }

        if (elementType is StringTypeNode stringElement)
        {
            var readCode = GenerateStringAssignmentCode(lastElemVar, stringElement);
            builder.Append(Indent(readCode, 1));
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    {tempVar}.Add({lastElemVar});");
            return;
        }

        if (elementType is InlineSchemaTypeNode inlineSchemaElement)
        {
            AppendInlineArrayElementRead(
                builder,
                inlineSchemaElement,
                fieldName,
                tempVar,
                1);
            var elemVar = GetInlineArrayElementVariable(tempVar);
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    {lastElemVar} = {elemVar};");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    {tempVar}.Add({lastElemVar});");
            return;
        }

        throw new NotSupportedException(
            $"Unsupported repeat until element type: {elementType.GetType().Name}");
    }

    private static string GenerateAlignmentCode(AlignmentNode alignmentNode)
    {
        var bits = alignmentNode.AlignmentBits;
        return $"AlignToBits(data, {bits});";
    }

    private ConstructionNotYetSupported CreateUnsupportedCodeGenerationException(
        string fieldName,
        TypeAnnotationNode unsupportedType,
        string role)
    {
        return new ConstructionNotYetSupported(
            $"Unsupported interpretation schema code generation for schema '{_currentSchemaName}', field '{fieldName}', {role} '{unsupportedType.GetType().Name}'.");
    }
}
