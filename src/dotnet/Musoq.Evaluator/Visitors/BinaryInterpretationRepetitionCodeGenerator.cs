using System.Globalization;
using System.Linq;
using System.Text;
using Musoq.Parser.Nodes;
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
        var elementTypeName = GetRepeatUntilElementClrTypeName(fieldName, repeatUntilType.ElementType);
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
            $"    EnsureRepeatIteration(\"{EscapeString(fieldName)}\", {iterationVar}++);");

        AppendRepeatUntilElementRead(
            builder,
            repeatUntilType.ElementType,
            fieldName,
            localVar,
            tempVar,
            lastElemVar);

        var conditionExpr = GenerateRepeatUntilCondition(
            repeatUntilType.Condition!,
            fieldName,
            lastElemVar,
            tempVar);
        builder.AppendLine(CultureInfo.InvariantCulture, $"}} while (!({conditionExpr}));");

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
        var elementTypeName = GetRepeatUntilElementClrTypeName(fieldName, repeatUntilType.ElementType);
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
            $"    EnsureRepeatIteration(\"{EscapeString(fieldName)}\", {iterationVar}++);");
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

    private string GenerateRepeatUntilCondition(Node condition, string fieldName, string lastElemVar, string listVar)
    {
        return GenerateRepeatUntilConditionInner(condition, fieldName, lastElemVar, listVar);
    }

    private string GenerateRepeatUntilConditionInner(
        Node condition,
        string fieldName,
        string lastElemVar,
        string listVar)
    {
        return condition switch
        {
            EqualityNode eq =>
                $"({GenerateRepeatUntilConditionInner(eq.Left, fieldName, lastElemVar, listVar)} == {GenerateRepeatUntilConditionInner(eq.Right, fieldName, lastElemVar, listVar)})",
            DiffNode diff =>
                $"({GenerateRepeatUntilConditionInner(diff.Left, fieldName, lastElemVar, listVar)} != {GenerateRepeatUntilConditionInner(diff.Right, fieldName, lastElemVar, listVar)})",
            GreaterNode gt =>
                $"({GenerateRepeatUntilConditionInner(gt.Left, fieldName, lastElemVar, listVar)} > {GenerateRepeatUntilConditionInner(gt.Right, fieldName, lastElemVar, listVar)})",
            GreaterOrEqualNode gte =>
                $"({GenerateRepeatUntilConditionInner(gte.Left, fieldName, lastElemVar, listVar)} >= {GenerateRepeatUntilConditionInner(gte.Right, fieldName, lastElemVar, listVar)})",
            LessNode lt =>
                $"({GenerateRepeatUntilConditionInner(lt.Left, fieldName, lastElemVar, listVar)} < {GenerateRepeatUntilConditionInner(lt.Right, fieldName, lastElemVar, listVar)})",
            LessOrEqualNode lte =>
                $"({GenerateRepeatUntilConditionInner(lte.Left, fieldName, lastElemVar, listVar)} <= {GenerateRepeatUntilConditionInner(lte.Right, fieldName, lastElemVar, listVar)})",
            AndNode andNode =>
                $"({GenerateRepeatUntilConditionInner(andNode.Left, fieldName, lastElemVar, listVar)} && {GenerateRepeatUntilConditionInner(andNode.Right, fieldName, lastElemVar, listVar)})",
            OrNode orNode =>
                $"({GenerateRepeatUntilConditionInner(orNode.Left, fieldName, lastElemVar, listVar)} || {GenerateRepeatUntilConditionInner(orNode.Right, fieldName, lastElemVar, listVar)})",
            NotNode notNode =>
                $"!({GenerateRepeatUntilConditionInner(notNode.Expression, fieldName, lastElemVar, listVar)})",
            BooleanNode booleanNode => booleanNode.Value ? "true" : "false",
            IntegerNode intNode => FormatRepeatIntegerLiteral(intNode.ObjValue),
            HexIntegerNode hexNode => FormatHexLiteral(hexNode),
            BinaryIntegerNode binaryNode => FormatRepeatIntegerLiteral(binaryNode.ObjValue),
            OctalIntegerNode octalNode => FormatRepeatIntegerLiteral(octalNode.ObjValue),
            DecimalNode decimalNode => decimalNode.ToString(),
            WordNode wordNode => $"\"{EscapeString(wordNode.Value)}\"",

            AccessColumnNode accessColumn when IsRepeatField(accessColumn.Name, fieldName) => lastElemVar,
            AccessColumnNode accessColumn => GetLocalVarName(accessColumn.Name),
            IdentifierNode identifier when IsRepeatField(identifier.Name, fieldName) => lastElemVar,
            IdentifierNode identifier => GetLocalVarName(identifier.Name),

            DotNode { Root: AccessColumnNode rootColumn } dotNode
                when IsRepeatField(rootColumn.Name, fieldName) =>
                $"{lastElemVar}.{GetPropertyNameFromExpression(dotNode.Expression)}",
            DotNode { Root: IdentifierNode rootIdentifier } dotNode
                when IsRepeatField(rootIdentifier.Name, fieldName) =>
                $"{lastElemVar}.{GetPropertyNameFromExpression(dotNode.Expression)}",
            DotNode dotNode =>
                $"{GenerateRepeatUntilConditionInner(dotNode.Root, fieldName, lastElemVar, listVar)}.{GetPropertyNameFromExpression(dotNode.Expression)}",

            ArrayIndexNode { Array: AccessColumnNode arrayColumn } arrayIndex
                when IsRepeatField(arrayColumn.Name, fieldName) =>
                GenerateArrayIndexExpression(listVar, arrayIndex.Index),
            ArrayIndexNode { Array: IdentifierNode arrayIdentifier } arrayIndex
                when IsRepeatField(arrayIdentifier.Name, fieldName) =>
                GenerateArrayIndexExpression(listVar, arrayIndex.Index),
            ArrayIndexNode arrayIndex =>
                $"{GenerateRepeatUntilConditionInner(arrayIndex.Array, fieldName, lastElemVar, listVar)}[{GenerateRepeatUntilConditionInner(arrayIndex.Index, fieldName, lastElemVar, listVar)}]",

            AddNode add =>
                $"({GenerateRepeatUntilConditionInner(add.Left, fieldName, lastElemVar, listVar)} + {GenerateRepeatUntilConditionInner(add.Right, fieldName, lastElemVar, listVar)})",
            HyphenNode hyphen =>
                $"({GenerateRepeatUntilConditionInner(hyphen.Left, fieldName, lastElemVar, listVar)} - {GenerateRepeatUntilConditionInner(hyphen.Right, fieldName, lastElemVar, listVar)})",
            StarNode star =>
                $"({GenerateRepeatUntilConditionInner(star.Left, fieldName, lastElemVar, listVar)} * {GenerateRepeatUntilConditionInner(star.Right, fieldName, lastElemVar, listVar)})",
            FSlashNode slash =>
                $"({GenerateRepeatUntilConditionInner(slash.Left, fieldName, lastElemVar, listVar)} / {GenerateRepeatUntilConditionInner(slash.Right, fieldName, lastElemVar, listVar)})",
            ModuloNode modulo =>
                $"({GenerateRepeatUntilConditionInner(modulo.Left, fieldName, lastElemVar, listVar)} % {GenerateRepeatUntilConditionInner(modulo.Right, fieldName, lastElemVar, listVar)})",
            BitwiseAndNode bitwiseAnd =>
                $"({GenerateRepeatUntilConditionInner(bitwiseAnd.Left, fieldName, lastElemVar, listVar)} & {GenerateRepeatUntilConditionInner(bitwiseAnd.Right, fieldName, lastElemVar, listVar)})",
            BitwiseOrNode bitwiseOr =>
                $"({GenerateRepeatUntilConditionInner(bitwiseOr.Left, fieldName, lastElemVar, listVar)} | {GenerateRepeatUntilConditionInner(bitwiseOr.Right, fieldName, lastElemVar, listVar)})",
            BitwiseXorNode bitwiseXor =>
                $"({GenerateRepeatUntilConditionInner(bitwiseXor.Left, fieldName, lastElemVar, listVar)} ^ {GenerateRepeatUntilConditionInner(bitwiseXor.Right, fieldName, lastElemVar, listVar)})",
            LeftShiftNode leftShift =>
                $"({GenerateRepeatUntilConditionInner(leftShift.Left, fieldName, lastElemVar, listVar)} << {GenerateRepeatUntilConditionInner(leftShift.Right, fieldName, lastElemVar, listVar)})",
            RightShiftNode rightShift =>
                $"({GenerateRepeatUntilConditionInner(rightShift.Left, fieldName, lastElemVar, listVar)} >> {GenerateRepeatUntilConditionInner(rightShift.Right, fieldName, lastElemVar, listVar)})",
            AccessMethodNode method => GenerateRepeatMethodCallExpression(method, fieldName, lastElemVar, listVar),
            _ => condition.ToString() ?? "false"
        };
    }

    private static bool IsRepeatField(string name, string fieldName)
    {
        return string.Equals(name, fieldName, StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatRepeatIntegerLiteral(object value)
    {
        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "0";
    }

    private string GenerateRepeatMethodCallExpression(
        AccessMethodNode method,
        string fieldName,
        string lastElemVar,
        string listVar)
    {
        var arguments = method.Arguments.Args
            .Select(argument => GenerateRepeatUntilConditionInner(argument, fieldName, lastElemVar, listVar))
            .ToArray();

        if (string.Equals(method.Name, "ToString", StringComparison.OrdinalIgnoreCase) && arguments.Length == 1)
            return $"{arguments[0]}.ToString()";

        return $"{method.Name}({string.Join(", ", arguments)})";
    }

    private string GenerateSubstreamReadCodeInner(
        string localVar,
        string fieldName,
        SubstreamTypeNode substreamType)
    {
        if (substreamType.Mode == SubstreamMode.Raw)
            return $"{localVar} = ReadBytes(data, {GenerateRawSubstreamSize(fieldName, substreamType)});";

        var sizeExpr = GenerateSizeExpression(substreamType.SizeExpression);
        var lengthVar = $"_{localVar}_substreamLength";
        var sliceVar = $"_{localVar}_substreamSlice";
        var interpreterVar = $"_{localVar}_substreamInterpreter";
        var builder = new StringBuilder();

        builder.AppendLine(CultureInfo.InvariantCulture, $"var {lengthVar} = {sizeExpr};");
        builder.AppendLine(CultureInfo.InvariantCulture, $"var {sliceVar} = ReadSubstreamSlice(data, {lengthVar});");
        builder.Append(GenerateSubstreamInterpreterConstruction(interpreterVar, fieldName, substreamType.Target!));
        AppendGeneratedLine(builder, $"{localVar} = InterpretNestedAt({interpreterVar}, {sliceVar}, 0, \"{EscapeString(fieldName)}\");");

        if (substreamType.Mode == SubstreamMode.Exact)
            builder.AppendLine(CultureInfo.InvariantCulture,
                $"EnsureSubstreamFullyConsumed(\"{EscapeString(fieldName)}\", {lengthVar}, {interpreterVar}.BytesConsumed);");

        builder.Append(CultureInfo.InvariantCulture, $"ParsePosition += {lengthVar};");
        return builder.ToString();
    }
}
