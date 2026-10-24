using System.Collections.Generic;
using System.Linq;
using System.Text;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private RepeatExpressionBinding? _repeatExpressionBinding;

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
        _activeSchemaFieldName = field.Name;
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
        if (_repeatExpressionBinding?.EmitReference(condition, GenerateArrayIndexExpression) is { } reference)
            return reference;
        if (condition is BinaryNode comparison && comparison is EqualityNode or DiffNode or GreaterNode or GreaterOrEqualNode or LessNode or LessOrEqualNode &&
            BinarySchemaExpressionTypes.IsNullable(BinarySchemaExpressionTypes.Resolve(condition, _expressionFields, _registry)))
            return GenerateNullableComparison(comparison);
        return condition switch
        {
            WhenNode or ThenNode or ElseNode => GenerateConditionExpression(((UnaryNode)condition).Expression),
            CaseNode choice => GenerateCaseExpression(choice),
            CastNode cast => GenerateStrictCastExpression(cast),
            NullNode => "null",
            NotNode { Expression: NullNode } => "(!((bool?)null))",
            NotNode not => $"(!({GenerateConditionExpression(not.Expression)}))",
            EqualityNode eq => GenerateComparisonExpression(eq, "=="),
            DiffNode diff => GenerateComparisonExpression(diff, "!="),
            GreaterNode gt => GenerateComparisonExpression(gt, ">"),
            GreaterOrEqualNode gte => GenerateComparisonExpression(gte, ">="),
            LessNode lt => GenerateComparisonExpression(lt, "<"),
            LessOrEqualNode lte => GenerateComparisonExpression(lte, "<="),
            AndNode andNode => SchemaLogicalExpression.Emit(andNode, GenerateConditionExpression,
                BinarySchemaExpressionTypes.IsNullable(BinarySchemaExpressionTypes.Resolve(andNode, _expressionFields, _registry))),
            OrNode orNode => SchemaLogicalExpression.Emit(orNode, GenerateConditionExpression,
                BinarySchemaExpressionTypes.IsNullable(BinarySchemaExpressionTypes.Resolve(orNode, _expressionFields, _registry))),
            CoalesceNode coalesce => SchemaCoalesceExpression.Emit(coalesce, _expressionFields, _registry, GenerateConditionExpression, MapClrTypeName),
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
            StringConcatenationNode concatenation => _stringConcatenation.Emit(concatenation, GenerateConditionExpression),
            AddNode add => GenerateArithmeticExpression(add, "+"),
            HyphenNode hyp => GenerateArithmeticExpression(hyp, "-"),
            StarNode star => GenerateArithmeticExpression(star, "*"),
            FSlashNode slash => GenerateArithmeticExpression(slash, "/"),
            ModuloNode mod => GenerateArithmeticExpression(mod, "%"),
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

    private string GenerateArithmeticExpression(BinaryNode node, string operation)
    {
        var leftType = BinarySchemaExpressionTypes.Resolve(node.Left, _expressionFields, _registry);
        var rightType = BinarySchemaExpressionTypes.Resolve(node.Right, _expressionFields, _registry);
        var resultType = BinarySchemaExpressionTypes.Resolve(node, _expressionFields, _registry);
        return BinarySchemaArithmeticEmitter.Emit(new(leftType, rightType, resultType,
            GenerateConditionExpression(node.Left), GenerateConditionExpression(node.Right), operation));
    }

    private string GenerateNullableComparison(BinaryNode node)
    {
        var operation = node switch { EqualityNode => "==", DiffNode => "!=", GreaterNode => ">", GreaterOrEqualNode => ">=", LessNode => "<", LessOrEqualNode => "<=", _ => throw new InvalidOperationException("Expected comparison.") };
        var comparison = GenerateComparisonCode(node, "__left", "__right", operation);
        return $"global::Musoq.Evaluator.Operators.SqlCompare({ComparisonOperand(node.Left, node.Right)}, {ComparisonOperand(node.Right, node.Left)}, static (__left, __right) => {comparison[1..^1]})";
    }

    private string GenerateComparisonExpression(BinaryNode node, string operation) =>
        GenerateComparisonCode(node, GenerateConditionExpression(node.Left), GenerateConditionExpression(node.Right), operation);

    private string GenerateComparisonCode(BinaryNode node, string left, string right, string operation)
    {
        var leftType = BinarySchemaExpressionTypes.Unwrap(BinarySchemaExpressionTypes.Resolve(node.Left, _expressionFields, _registry));
        var rightType = BinarySchemaExpressionTypes.Unwrap(BinarySchemaExpressionTypes.Resolve(node.Right, _expressionFields, _registry));
        var nullable = BinarySchemaExpressionTypes.MayBeNull(node.Left, _expressionFields, _registry) ||
            BinarySchemaExpressionTypes.MayBeNull(node.Right, _expressionFields, _registry);
        return BinarySchemaComparisonEmitter.Emit(new(leftType, rightType, left, right, operation, nullable));
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

        var intrinsic = SchemaIntrinsicCodeEmitter.ResolveName(methodName);
        if (intrinsic is not null)
        {
            _intrinsicCallCount++;
            var operation = $"{_intrinsicTypeName}.{intrinsic}({string.Join(", ", argExpressions)})";
            var factory = _registry.TryGetSchema(_currentSchemaName, out var registration) && registration?.Node is TextSchemaNode
                ? "ForText" : "ForBytes";
            return $"{_intrinsicTypeName}.Invoke(() => {operation}, (__code, __error) => ParseException.{factory}(__code, SchemaName, \"{EscapeString(_activeSchemaFieldName)}\", ParsePosition, __error.Message, __error))";
        }

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
