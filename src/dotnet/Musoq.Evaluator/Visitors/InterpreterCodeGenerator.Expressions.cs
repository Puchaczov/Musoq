using System.Collections.Generic;
using System.Linq;
using System.Text;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private string GenerateSizeExpression(Node sizeExpression, ParseErrorCode errorCode = ParseErrorCode.InvalidSize)
    {
        if (sizeExpression is IntegerNode integer && int.TryParse(Convert.ToString(integer.ObjValue,
                System.Globalization.CultureInfo.InvariantCulture), System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out var value))
            return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var type = BinarySchemaExpressionTypes.Unwrap(BinarySchemaExpressionTypes.Resolve(sizeExpression, _expressionFields, _registry));
        if (!BinaryOperatorTypeRules.IsNumericType(type))
            throw new Musoq.Evaluator.Exceptions.QuerySyntaxException("Size and position expressions must be numeric.",
                sizeExpression.SpanOrEmpty(), Musoq.Parser.Diagnostics.DiagnosticCode.MQ4006_InvalidFieldConstraint);
        var typeName = MapClrTypeName(type);
        return $"ResolveExpressionSize<{typeName}>(() => checked(({typeName}?)({GenerateConditionExpression(sizeExpression)})), ParseErrorCode.{errorCode})";
    }

    private string GenerateComputedFieldCode(ComputedFieldNode field, List<SchemaFieldNode>? contextFields = null)
    {
        var builder = new StringBuilder();
        var localVar = GetLocalVarName(field.Name);
        var expression = GenerateConditionExpression(field.Expression);
        var typeName = InferComputedFieldTypeName(field.Expression, contextFields);

        if (contextFields != null && BinarySchemaExpressionTypes.IsNullable(BinarySchemaExpressionTypes.Resolve(field.Expression, contextFields, _registry)) &&
            !IsReferenceType(typeName))
            typeName += "?";

        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {localVar} = ({typeName})({expression});");

        return builder.ToString();
    }

    private string GenerateConditionExpression(Node condition)
    {
        if (condition is BinaryNode comparison && comparison is EqualityNode or DiffNode or GreaterNode or GreaterOrEqualNode or LessNode or LessOrEqualNode &&
            BinarySchemaExpressionTypes.IsNullable(BinarySchemaExpressionTypes.Resolve(condition, _expressionFields, _registry)))
            return GenerateNullableComparison(comparison);
        return condition switch
        {
            WhenNode or ThenNode or ElseNode => GenerateConditionExpression(((UnaryNode)condition).Expression),
            CaseNode choice => GenerateCaseExpression(choice),
            NullNode => "null",
            NotNode not => $"(!({GenerateConditionExpression(not.Expression)}))",
            EqualityNode eq => $"({GenerateConditionExpression(eq.Left)} == {GenerateConditionExpression(eq.Right)})",
            DiffNode diff => $"({GenerateConditionExpression(diff.Left)} != {GenerateConditionExpression(diff.Right)})",
            GreaterNode gt => $"({GenerateConditionExpression(gt.Left)} > {GenerateConditionExpression(gt.Right)})",
            GreaterOrEqualNode gte =>
                $"({GenerateConditionExpression(gte.Left)} >= {GenerateConditionExpression(gte.Right)})",
            LessNode lt => $"({GenerateConditionExpression(lt.Left)} < {GenerateConditionExpression(lt.Right)})",
            LessOrEqualNode lte =>
                $"({GenerateConditionExpression(lte.Left)} <= {GenerateConditionExpression(lte.Right)})",
            AndNode andNode =>
                $"({GenerateConditionExpression(andNode.Left)} {(BinarySchemaExpressionTypes.IsNullable(BinarySchemaExpressionTypes.Resolve(andNode, _expressionFields, _registry)) ? "&" : "&&")} {GenerateConditionExpression(andNode.Right)})",
            OrNode orNode =>
                $"({GenerateConditionExpression(orNode.Left)} {(BinarySchemaExpressionTypes.IsNullable(BinarySchemaExpressionTypes.Resolve(orNode, _expressionFields, _registry)) ? "|" : "||")} {GenerateConditionExpression(orNode.Right)})",
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

    private string GenerateNullableComparison(BinaryNode node)
    {
        var operation = node switch { EqualityNode => "==", DiffNode => "!=", GreaterNode => ">", GreaterOrEqualNode => ">=", LessNode => "<", LessOrEqualNode => "<=", _ => throw new InvalidOperationException("Expected comparison.") };
        return $"global::Musoq.Evaluator.Operators.SqlCompare({ComparisonOperand(node.Left, node.Right)}, {ComparisonOperand(node.Right, node.Left)}, static (__left, __right) => __left {operation} __right)";
    }

    private string ComparisonOperand(Node expression, Node other)
    {
        if (expression is not NullNode)
            return GenerateConditionExpression(expression);
        var type = BinarySchemaExpressionTypes.Unwrap(BinarySchemaExpressionTypes.Resolve(other, _expressionFields, _registry));
        var typeName = MapClrTypeName(type) + (type.IsValueType ? "?" : string.Empty);
        return $"(({typeName})null)";
    }

    private string GenerateCaseExpression(CaseNode node)
    {
        var type = BinarySchemaExpressionTypes.Resolve(node, _expressionFields, _registry);
        var typeName = MapClrTypeName(BinarySchemaExpressionTypes.Unwrap(type)) + (BinarySchemaExpressionTypes.IsNullable(type) ? "?" : string.Empty);
        var result = $"(({typeName})({GenerateConditionExpression(node.Else)}))";
        foreach (var pair in node.WhenThenPairs.Reverse())
            result = $"(({GenerateConditionExpression(pair.When)}) == true ? ({typeName})({GenerateConditionExpression(pair.Then)}) : {result})";
        return result;
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
        var path = string.Join('.', BinarySchemaExpressionTypes.PathParts(dotNode));
        if (_parentContext?.ExpressionReferences.Contains(dotNode) == true)
            return $"__parentContext.{_parentContext.Member(path)}";
        var root = GenerateConditionExpression(dotNode.Root);
        var property = GetPropertyNameFromExpression(dotNode.Expression);
        var rootType = BinarySchemaExpressionTypes.Resolve(dotNode.Root, _expressionFields, _registry);
        if (!BinarySchemaExpressionTypes.Unwrap(rootType).IsValueType && BinarySchemaExpressionTypes.MayBeNull(dotNode, _expressionFields, _registry))
            return $"({root})?.{property}";
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

        var indexCode = GenerateConditionExpression(indexExpr);
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
