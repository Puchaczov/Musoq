using System.Collections.Generic;
using System.Linq;
using System.Text;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private string GenerateSizeExpression(Node sizeExpression)
    {
        if (sizeExpression is IntegerNode intNode) return intNode.ObjValue.ToString() ?? "0";

        if (sizeExpression is AccessColumnNode accessNode)
            return GenerateSizeFieldReference(accessNode.Name);

        if (sizeExpression is IdentifierNode identifierNode)
            return GenerateSizeFieldReference(identifierNode.Name);

        if (sizeExpression is AddNode addNode)
        {
            var left = GenerateSizeExpression(addNode.Left);
            var right = GenerateSizeExpression(addNode.Right);
            return $"({left} + {right})";
        }

        if (sizeExpression is HyphenNode hyphenNode)
        {
            var left = GenerateSizeExpression(hyphenNode.Left);
            var right = GenerateSizeExpression(hyphenNode.Right);
            return $"({left} - {right})";
        }

        if (sizeExpression is StarNode starNode)
        {
            var left = GenerateSizeExpression(starNode.Left);
            var right = GenerateSizeExpression(starNode.Right);
            return $"({left} * {right})";
        }

        if (sizeExpression is FSlashNode divNode)
        {
            var left = GenerateSizeExpression(divNode.Left);
            var right = GenerateSizeExpression(divNode.Right);
            return $"({left} / {right})";
        }

        if (sizeExpression is ModuloNode modNode)
        {
            var left = GenerateSizeExpression(modNode.Left);
            var right = GenerateSizeExpression(modNode.Right);
            return $"({left} % {right})";
        }

        return sizeExpression.ToString() ?? "0";
    }

    private string GenerateComputedFieldCode(ComputedFieldNode field, List<SchemaFieldNode>? contextFields = null)
    {
        var builder = new StringBuilder();
        var localVar = GetLocalVarName(field.Name);
        var expression = GenerateConditionExpression(field.Expression);
        var typeName = InferComputedFieldTypeName(field.Expression, contextFields);

        if (contextFields != null && ReferencesConditionalField(field.Expression, contextFields) &&
            !IsReferenceType(typeName))
            typeName += "?";

        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {localVar} = ({typeName})({expression});");

        return builder.ToString();
    }

    private static bool ReferencesConditionalField(Node expression, IReadOnlyList<SchemaFieldNode> contextFields)
    {
        return expression switch
        {
            IdentifierNode id => contextFields.Any(f =>
                f.Name.Equals(id.Name, StringComparison.OrdinalIgnoreCase) && f.IsConditional),
            BinaryNode binary => ReferencesConditionalField(binary.Left, contextFields) ||
                                 ReferencesConditionalField(binary.Right, contextFields),
            _ => false
        };
    }

    private string GenerateConditionExpression(Node condition)
    {
        return condition switch
        {
            EqualityNode eq => $"({GenerateConditionExpression(eq.Left)} == {GenerateConditionExpression(eq.Right)})",
            DiffNode diff => $"({GenerateConditionExpression(diff.Left)} != {GenerateConditionExpression(diff.Right)})",
            GreaterNode gt => $"({GenerateConditionExpression(gt.Left)} > {GenerateConditionExpression(gt.Right)})",
            GreaterOrEqualNode gte =>
                $"({GenerateConditionExpression(gte.Left)} >= {GenerateConditionExpression(gte.Right)})",
            LessNode lt => $"({GenerateConditionExpression(lt.Left)} < {GenerateConditionExpression(lt.Right)})",
            LessOrEqualNode lte =>
                $"({GenerateConditionExpression(lte.Left)} <= {GenerateConditionExpression(lte.Right)})",
            AndNode andNode =>
                $"({GenerateConditionExpression(andNode.Left)} && {GenerateConditionExpression(andNode.Right)})",
            OrNode orNode =>
                $"({GenerateConditionExpression(orNode.Left)} || {GenerateConditionExpression(orNode.Right)})",
            IntegerNode intNode => Convert.ToString(intNode.ObjValue, System.Globalization.CultureInfo.InvariantCulture) ?? "0",
            BinaryIntegerNode binaryNode => Convert.ToString(binaryNode.ObjValue, System.Globalization.CultureInfo.InvariantCulture) ?? "0",
            OctalIntegerNode octalNode => Convert.ToString(octalNode.ObjValue, System.Globalization.CultureInfo.InvariantCulture) ?? "0",
            DecimalNode decNode => decNode.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) + "m",
            BooleanNode booleanNode => booleanNode.Value ? "true" : "false",
            WordNode wordNode => $"\"{EscapeString(wordNode.Value)}\"",
            StringNode stringNode => $"\"{EscapeString(stringNode.Value)}\"",
            DotNode dotNode => GenerateDotExpression(dotNode),
            ArrayIndexNode arrayIndex =>
                $"{GenerateConditionExpression(arrayIndex.Array)}[{GenerateConditionExpression(arrayIndex.Index)}]",
            AccessColumnNode acNode => GetLocalVarName(acNode.Name),
            IdentifierNode idNode => GetLocalVarName(idNode.Name),
            AccessMethodNode methodNode => GenerateMethodCallExpression(methodNode),
            AddNode add => $"({GenerateConditionExpression(add.Left)} + {GenerateConditionExpression(add.Right)})",
            HyphenNode hyp => $"({GenerateConditionExpression(hyp.Left)} - {GenerateConditionExpression(hyp.Right)})",
            StarNode star => $"({GenerateConditionExpression(star.Left)} * {GenerateConditionExpression(star.Right)})",
            FSlashNode slash =>
                $"({GenerateConditionExpression(slash.Left)} / {GenerateConditionExpression(slash.Right)})",
            ModuloNode mod => $"({GenerateConditionExpression(mod.Left)} % {GenerateConditionExpression(mod.Right)})",
            HexIntegerNode hexNode => FormatHexLiteral(hexNode),

            BitwiseAndNode bitwiseAnd =>
                $"({GenerateConditionExpression(bitwiseAnd.Left)} & {GenerateConditionExpression(bitwiseAnd.Right)})",
            BitwiseOrNode bitwiseOr =>
                $"({GenerateConditionExpression(bitwiseOr.Left)} | {GenerateConditionExpression(bitwiseOr.Right)})",
            BitwiseXorNode bitwiseXor =>
                $"({GenerateConditionExpression(bitwiseXor.Left)} ^ {GenerateConditionExpression(bitwiseXor.Right)})",
            LeftShiftNode leftShift =>
                $"({GenerateConditionExpression(leftShift.Left)} << {GenerateConditionExpression(leftShift.Right)})",
            RightShiftNode rightShift =>
                $"({GenerateConditionExpression(rightShift.Left)} >> {GenerateConditionExpression(rightShift.Right)})",
            _ => condition.ToString() ?? "false"
        };
    }

    private string GenerateMethodCallExpression(AccessMethodNode methodNode)
    {
        var methodName = methodNode.Name;
        var args = methodNode.Arguments?.Args ?? Array.Empty<Node>();
        var argExpressions = args.Select(GenerateConditionExpression).ToList();

        if (string.Equals(methodName, "ToString", StringComparison.OrdinalIgnoreCase) && argExpressions.Count == 1)
            return $"{argExpressions[0]}.ToString()";

        if (string.Equals(methodName, "ToString", StringComparison.OrdinalIgnoreCase) && argExpressions.Count == 0)
            return ".ToString()";

        return $"{methodName}({string.Join(", ", argExpressions)})";
    }

    private string GenerateDotExpression(DotNode dotNode)
    {
        var root = GenerateConditionExpression(dotNode.Root);
        var property = GetPropertyNameFromExpression(dotNode.Expression);
        return $"{root}.{property}";
    }

    private string GenerateArrayIndexExpression(string listVar, Node indexExpr)
    {
        if (indexExpr is HyphenNode
            {
                Left: IntegerNode { ObjValue: int and 0 }, Right: IntegerNode { ObjValue: int rightVal and > 0 }
            }) return $"{listVar}[{listVar}.Count - {rightVal}]";

        if (indexExpr is IntegerNode intIdx)
        {
            var idx = Convert.ToInt32(intIdx.ObjValue, System.Globalization.CultureInfo.InvariantCulture);
            if (idx < 0) return $"{listVar}[{listVar}.Count - {-idx}]";
            return $"{listVar}[{idx}]";
        }

        var indexCode = GenerateSizeExpression(indexExpr);
        return $"{listVar}[{indexCode}]";
    }

    private static string EscapeString(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal);
    }

    private static string GetPropertyNameFromExpression(Node expression)
    {
        return expression switch
        {
            AccessColumnNode acNode => acNode.Name,
            IdentifierNode idNode => idNode.Name,
            _ => expression.ToString() ?? "Unknown"
        };
    }

}
