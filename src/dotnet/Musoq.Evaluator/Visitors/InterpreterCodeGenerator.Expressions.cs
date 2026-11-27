using System.Collections.Generic;
using System.Linq;
using System.Text;
using Musoq.Evaluator.Helpers;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private RepeatExpressionBinding? _repeatExpressionBinding;

    private string GenerateSizeExpression(Node sizeExpression, ParseErrorCode errorCode = ParseErrorCode.InvalidSize)
    {
        if (
            sizeExpression is IntegerNode integer
            && int.TryParse(
                Convert.ToString(integer.ObjValue, System.Globalization.CultureInfo.InvariantCulture),
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out var value
            )
        )
            return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var (typeName, expression) = BindSizeExpression(sizeExpression);
        return $"ResolveExpressionSize<{typeName}>(() => checked(({typeName}?)({expression})), ParseErrorCode.{errorCode})";
    }

    private (string TypeName, string Expression) BindSizeExpression(Node sizeExpression)
    {
        var type = BinarySchemaExpressionTypes.Unwrap(
            BinarySchemaExpressionTypes.Resolve(sizeExpression, _expressionFields, _registry)
        );
        if (!BinaryOperatorTypeRules.IsNumericType(type))
            throw new Musoq.Evaluator.Exceptions.QuerySyntaxException(
                "Size and position expressions must be numeric.",
                sizeExpression.SpanOrEmpty(),
                Musoq.Parser.Diagnostics.DiagnosticCode.MQ4006_InvalidFieldConstraint
            );
        var typeName = MapClrTypeName(type);
        return (typeName, GenerateConditionExpression(sizeExpression));
    }

    private string StatusSizeDeclaration(
        string variable,
        Node expression,
        ParseErrorCode errorCode = ParseErrorCode.InvalidSize
    )
    {
        if (
            expression is IntegerNode integer
            && int.TryParse(
                Convert.ToString(integer.ObjValue, System.Globalization.CultureInfo.InvariantCulture),
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out var value
            )
        )
            return $"var {variable} = {value.ToString(System.Globalization.CultureInfo.InvariantCulture)};\n";
        var binding = BindSizeExpression(expression);
        var argument = $"({binding.TypeName}?)({binding.Expression})";
        var call =
            expression is IdentifierNode
                ? $"base.ResolveSizeValueStatus<{binding.TypeName}>({argument}, ParseErrorCode.{errorCode})"
                : $"base.ResolveSizeExpressionStatus<{binding.TypeName}>(() => checked({argument}), ParseErrorCode.{errorCode})";
        return StatusRead(variable, call);
    }

    private string GenerateComputedFieldCode(ComputedFieldNode field, List<SchemaFieldNode>? contextFields = null)
    {
        var builder = new StringBuilder();
        var localVar = GetLocalVarName(field.Name);
        _activeSchemaFieldName = field.Name;
        var expression = GenerateConditionExpression(field.Expression);
        var typeName = InferComputedFieldTypeName(field.Expression, contextFields);

        if (
            contextFields != null
            && BinarySchemaExpressionTypes.IsNullable(
                BinarySchemaExpressionTypes.Resolve(field.Expression, contextFields, _registry)
            )
            && !IsReferenceType(typeName)
        )
            typeName += "?";

        builder.AppendLine(
            System.Globalization.CultureInfo.InvariantCulture,
            $"var {localVar} = ({typeName})({expression});"
        );

        return builder.ToString();
    }

    private string GenerateConditionExpression(Node condition)
    {
        if (_repeatExpressionBinding?.EmitReference(condition, GenerateIndexValue) is { } reference)
            return reference;
        if (
            condition is BinaryNode comparison
            && comparison
                is EqualityNode
                    or DiffNode
                    or GreaterNode
                    or GreaterOrEqualNode
                    or LessNode
                    or LessOrEqualNode
            && BinarySchemaExpressionTypes.IsNullable(
                BinarySchemaExpressionTypes.Resolve(condition, _expressionFields, _registry)
            )
        )
            return GenerateNullableComparison(comparison);
        return condition switch
        {
            WhenNode or ThenNode or ElseNode => GenerateConditionExpression(((UnaryNode)condition).Expression),
            CaseNode choice => GenerateCaseExpression(choice),
            IsNullNode predicate => GenerateNullPredicate(predicate),
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
            AndNode andNode => SchemaLogicalExpression.Emit(
                andNode,
                GenerateConditionExpression,
                BinarySchemaExpressionTypes.IsNullable(
                    BinarySchemaExpressionTypes.Resolve(andNode, _expressionFields, _registry)
                )
            ),
            OrNode orNode => SchemaLogicalExpression.Emit(
                orNode,
                GenerateConditionExpression,
                BinarySchemaExpressionTypes.IsNullable(
                    BinarySchemaExpressionTypes.Resolve(orNode, _expressionFields, _registry)
                )
            ),
            CoalesceNode coalesce => SchemaCoalesceExpression.Emit(
                coalesce,
                _expressionFields,
                _registry,
                GenerateConditionExpression,
                InferComputedFieldTypeName
            ),
            NumericLiteralNode integer
                when integer is IntegerNode or HexIntegerNode or BinaryIntegerNode or OctalIntegerNode =>
                FormatIntegerLiteral(integer),
            DecimalNode decNode => decNode.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) + "m",
            BooleanNode booleanNode => booleanNode.Value ? "true" : "false",
            WordNode wordNode => $"\"{EscapeString(wordNode.Value)}\"",
            StringNode stringNode => $"\"{EscapeString(stringNode.Value)}\"",
            DotNode dotNode => GenerateDotExpression(dotNode),
            ArrayIndexNode arrayIndex => GenerateIndexedExpression(arrayIndex),
            AccessColumnNode acNode => GenerateFieldReference(acNode.Name),
            IdentifierNode idNode => GenerateFieldReference(idNode.Name),
            AccessMethodNode methodNode => GenerateMethodCallExpression(methodNode),
            StringConcatenationNode concatenation => _stringConcatenation.Emit(
                concatenation,
                GenerateConditionExpression
            ),
            AddNode add => GenerateArithmeticExpression(add, "+"),
            HyphenNode hyp => GenerateArithmeticExpression(hyp, "-"),
            StarNode star => GenerateArithmeticExpression(star, "*"),
            FSlashNode slash => GenerateArithmeticExpression(slash, "/"),
            ModuloNode mod => GenerateArithmeticExpression(mod, "%"),

            BitwiseAndNode bitwiseAnd => GenerateArithmeticExpression(bitwiseAnd, "&"),
            BitwiseOrNode bitwiseOr => GenerateArithmeticExpression(bitwiseOr, "|"),
            BitwiseXorNode bitwiseXor => GenerateArithmeticExpression(bitwiseXor, "^"),
            LeftShiftNode leftShift => GenerateArithmeticExpression(leftShift, "<<"),
            RightShiftNode rightShift => GenerateArithmeticExpression(rightShift, ">>"),
            _ => condition.ToString() ?? "false",
        };
    }

    private string GenerateArithmeticExpression(BinaryNode node, string operation)
    {
        var leftType = BinarySchemaExpressionTypes.Resolve(node.Left, _expressionFields, _registry);
        var rightType = BinarySchemaExpressionTypes.Resolve(node.Right, _expressionFields, _registry);
        var resultType = BinarySchemaExpressionTypes.Resolve(node, _expressionFields, _registry);
        return BinarySchemaArithmeticEmitter.Emit(
            new(
                leftType,
                rightType,
                resultType,
                GenerateConditionExpression(node.Left),
                GenerateConditionExpression(node.Right),
                operation
            )
        );
    }

    private string GenerateNullableComparison(BinaryNode node)
    {
        var operation = node switch
        {
            EqualityNode => "==",
            DiffNode => "!=",
            GreaterNode => ">",
            GreaterOrEqualNode => ">=",
            LessNode => "<",
            LessOrEqualNode => "<=",
            _ => throw new InvalidOperationException("Expected comparison."),
        };
        var comparison = GenerateComparisonCode(node, "__left", "__right", operation);
        return $"global::Musoq.Evaluator.Operators.SqlCompare({ComparisonOperand(node.Left, node.Right)}, {ComparisonOperand(node.Right, node.Left)}, static (__left, __right) => {comparison[1..^1]})";
    }

    private string GenerateComparisonExpression(BinaryNode node, string operation) =>
        GenerateComparisonCode(
            node,
            GenerateConditionExpression(node.Left),
            GenerateConditionExpression(node.Right),
            operation
        );

    private string GenerateComparisonCode(BinaryNode node, string left, string right, string operation)
    {
        var leftType = BinarySchemaExpressionTypes.Unwrap(
            BinarySchemaExpressionTypes.Resolve(node.Left, _expressionFields, _registry)
        );
        var rightType = BinarySchemaExpressionTypes.Unwrap(
            BinarySchemaExpressionTypes.Resolve(node.Right, _expressionFields, _registry)
        );
        if (operation is "==" or "!=")
        {
            if (rightType == typeof(char) && node.Left is ConstantValueNode { ObjValue: string { Length: 1 } })
                (leftType, left) = (typeof(char), $"({left})[0]");
            if (leftType == typeof(char) && node.Right is ConstantValueNode { ObjValue: string { Length: 1 } })
                (rightType, right) = (typeof(char), $"({right})[0]");
            // Signed 32-bit binary signatures may be written as their unsigned hexadecimal bit patterns.
            if (IsInt32Signature(node.Left, rightType))
                (leftType, left) = (typeof(int), $"unchecked((int)({left}))");
            if (IsInt32Signature(node.Right, leftType))
                (rightType, right) = (typeof(int), $"unchecked((int)({right}))");
        }
        var nullable =
            BinarySchemaExpressionTypes.MayBeNull(node.Left, _expressionFields, _registry)
            || BinarySchemaExpressionTypes.MayBeNull(node.Right, _expressionFields, _registry);
        return BinarySchemaComparisonEmitter.Emit(new(leftType, rightType, left, right, operation, nullable));
    }

    private static bool IsInt32Signature(Node literal, Type otherType) =>
        otherType == typeof(int)
        && literal is HexIntegerNode { ObjValue: long number }
        && number is > int.MaxValue and <= uint.MaxValue;

    private string ComparisonOperand(Node expression, Node other)
    {
        if (expression is not NullNode)
            return GenerateConditionExpression(expression);
        var type = BinarySchemaExpressionTypes.Unwrap(
            BinarySchemaExpressionTypes.Resolve(other, _expressionFields, _registry)
        );
        var typeName = MapClrTypeName(type) + (type.IsValueType ? "?" : string.Empty);
        return $"(({typeName})null)";
    }

    private string GenerateCaseExpression(CaseNode node)
    {
        var type = BinarySchemaExpressionTypes.Resolve(node, _expressionFields, _registry);
        var typeName =
            InferComputedFieldTypeName(node, _expressionFields, type)
            + (BinarySchemaExpressionTypes.IsNullable(type) ? "?" : string.Empty);
        var result = $"(({typeName})({GenerateConditionExpression(node.Else)}))";
        foreach (var pair in node.WhenThenPairs.Reverse())
            result =
                $"(({GenerateConditionExpression(pair.When)}) == true ? ({typeName})({GenerateConditionExpression(pair.Then)}) : {result})";
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
            var factory =
                _registry.TryGetSchema(_currentSchemaName, out var registration) && registration?.Node is TextSchemaNode
                    ? "ForText"
                    : "ForBytes";
            return $"{_intrinsicTypeName}.Invoke(() => {operation}, (__code, __error) => ParseException.{factory}(__code, SchemaName, \"{EscapeString(_activeSchemaFieldName)}\", ParsePosition, __error.Message, __error))";
        }

        return $"{methodName}({string.Join(", ", argExpressions)})";
    }

    private string GenerateFieldReference(string name) =>
        _parentContext?.ImplicitNames.Contains(name) == true
            ? $"{_parentContext.StorageName}.{_parentContext.Member("parent." + name)}"
            : GetLocalVarName(name);

    private string GenerateDotExpression(DotNode dotNode)
    {
        if (_parentContext?.BoundPaths.TryGetValue(dotNode, out var path) == true)
            return $"{_parentContext.StorageName}.{_parentContext.Member(path)}";
        var root = GenerateConditionExpression(dotNode.Root);
        var memberName = GetPropertyNameFromExpression(dotNode.Expression);
        var property = EscapeCSharpIdentifier(memberName);
        if (
            _registry.EmbeddedTextShapes?.Expression(dotNode.Root, _expressionFields) is
            { Alternatives.Count: > 0 } choice
        )
            return GenerateTextSwitchMember(root, choice, memberName, dotNode.Root);
        var rootType = BinarySchemaExpressionTypes.Resolve(dotNode.Root, _expressionFields, _registry);
        if (
            !BinarySchemaExpressionTypes.Unwrap(rootType).IsValueType
            && BinarySchemaExpressionTypes.MayBeNull(dotNode, _expressionFields, _registry)
        )
            return $"({root})?.{property}";
        return $"{root}.{property}";
    }

    private static string GenerateTextSwitchMember(
        string receiver,
        TextResultShape choice,
        string memberName,
        Node source
    )
    {
        var value = TextSwitchPropertyResolver.RequireMember(choice, source.ToString(), memberName);
        var type = value.TypeName is { } name ? LocalInterpreterType(name) : MapClrTypeName(value.ClrType);
        var property = EscapeCSharpIdentifier(memberName);
        var cases = choice
            .Alternatives.Where(arm => arm.TypeName != null && arm.Member(memberName) != null)
            .Select(arm => $"{LocalInterpreterType(arm.TypeName!)} __arm => ({type})__arm.{property}")
            .Distinct(StringComparer.Ordinal);
        return $"(({receiver}) switch {{ {string.Join(", ", cases)}, _ => ({type})null! }})";
    }

    private static string LocalInterpreterType(string qualified) =>
        qualified.Replace(TextResultTypeNames.NamespacePrefix, string.Empty, StringComparison.Ordinal);

    private string GenerateIndexedExpression(ArrayIndexNode indexed)
    {
        var method =
            BinarySchemaExpressionTypes.Resolve(indexed.Array, _expressionFields, _registry) == typeof(string)
                ? nameof(SafeArrayAccess.GetStringCharacter)
                : nameof(SafeArrayAccess.GetArrayElement);
        return $"global::Musoq.Evaluator.Helpers.SafeArrayAccess.{method}({GenerateConditionExpression(indexed.Array)}, {GenerateIndexValue(indexed.Index)})";
    }

    private string GenerateIndexValue(Node index)
    {
        var value = GenerateConditionExpression(index);
        return BinarySchemaExpressionTypes.Resolve(index, _expressionFields, _registry) == typeof(int)
            ? value
            : $"checked((int)({value}))";
    }

    private static string EscapeString(string value) => InterpretationStringLiterals.EscapeCSharpString(value);

    private static string GetPropertyNameFromExpression(Node expression)
    {
        return expression switch
        {
            AccessColumnNode acNode => acNode.Name,
            IdentifierNode idNode => idNode.Name,
            _ => expression.ToString() ?? "Unknown",
        };
    }
}
