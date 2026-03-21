using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Parser;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Visitors;

/// <summary>
///     Utility methods extracted from BuildMetadataAndInferTypesVisitor to improve maintainability and testability.
/// </summary>
public static class BuildMetadataAndInferTypesVisitorUtilities
{
    private static readonly ConcurrentDictionary<Type, bool> HasIndexerCache = new();
    private static readonly ConcurrentDictionary<Type, bool> IsIndexableCache = new();

    /// <summary>
    ///     Finds the closest common parent type between two types in the inheritance hierarchy.
    /// </summary>
    public static Type FindClosestCommonParent(Type first, Type second)
    {
        var type1Ancestors = new HashSet<Type>();

        while (first != null)
        {
            type1Ancestors.Add(first);
            first = first.BaseType;
        }

        while (second != null)
        {
            if (type1Ancestors.Contains(second)) return second;

            second = second.BaseType;
        }

        return typeof(object);
    }

    /// <summary>
    ///     Makes a value type nullable, or returns the type as-is if it's already nullable or a reference type.
    /// </summary>
    public static Type MakeTypeNullable(Type type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));

        if ((type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            || !type.IsValueType)
            return type;

        return typeof(Nullable<>).MakeGenericType(type);
    }

    /// <summary>
    ///     Strips the nullable wrapper from a nullable type, or returns the type as-is if it's not nullable.
    /// </summary>
    public static Type StripNullable(Type type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            return Nullable.GetUnderlyingType(type);

        return type;
    }

    /// <summary>
    ///     Checks if a type has an indexer property (supports array-like access).
    /// </summary>
    public static bool HasIndexer(Type type)
    {
        if (type is null) return false;

        return HasIndexerCache.GetOrAdd(type, static t =>
            t.GetProperties().Any(f => f.GetIndexParameters().Length > 0));
    }

    /// <summary>
    ///     Checks if a type supports indexing (has an indexer property or is an array).
    /// </summary>
    public static bool IsIndexableType(Type type)
    {
        if (type == null) return false;

        return IsIndexableCache.GetOrAdd(type, static t =>
        {
            try
            {
                if (t.IsArray)
                    return true;

                if (t == typeof(string))
                    return true;

                return t.GetProperties().Any(p => p.GetIndexParameters().Length > 0);
            }
            catch (Exception ex) when (ex is NotSupportedException || ex is TypeLoadException)
            {
                return false;
            }
        });
    }

    /// <summary>
    ///     Checks if a type is a primitive type that cannot have property access.
    /// </summary>
    public static bool IsPrimitiveType(Type type)
    {
        if (type == null) return false;

        return type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime) ||
               type == typeof(DateTimeOffset);
    }

    /// <summary>
    ///     Checks if a type is a valid query expression type.
    ///     Valid types are primitive types (numeric, bool, char), string, decimal, DateTime, DateTimeOffset, Guid, TimeSpan,
    ///     and null.
    ///     Nullable versions of these types are also valid.
    ///     Arrays and complex types (classes, structs) are not valid.
    /// </summary>
    public static bool IsValidQueryExpressionType(Type type)
    {
        if (type == null) return false;

        if (type.FullName == typeof(NullNode.NullType).FullName) return true;

        if (type.IsArray) return false;

        var typeToCheck = StripNullable(type);

        return IsPrimitiveType(typeToCheck) ||
               typeToCheck == typeof(Guid) ||
               typeToCheck == typeof(TimeSpan);
    }

    /// <summary>
    ///     Checks if a column should be included when expanding the star (*) operator.
    ///     Filters out arrays and non-primitive types.
    ///     <para>
    ///         In this context, a "primitive type" is defined by the <see cref="IsPrimitiveType" /> method,
    ///         which returns true for .NET primitive types, as well as <see cref="string" />, <see cref="decimal" />,
    ///         <see cref="DateTime" />, and <see cref="DateTimeOffset" />.
    ///     </para>
    /// </summary>
    public static bool ShouldIncludeColumnInStarExpansion(Type columnType)
    {
        if (columnType == null) return false;

        if (columnType.IsArray)
            return false;

        var typeToCheck = StripNullable(columnType);

        return IsPrimitiveType(typeToCheck);
    }

    /// <summary>
    ///     Checks if a type is a generic enumerable and returns the element type.
    /// </summary>
    public static bool IsGenericEnumerable(Type type, out Type elementType)
    {
        elementType = null;

        if (!type.IsGenericType) return false;

        var interfaces = type.GetInterfaces().Concat([type]);

        foreach (var interfaceType in interfaces)
        {
            if (!interfaceType.IsGenericType ||
                interfaceType.GetGenericTypeDefinition() != typeof(IEnumerable<>)) continue;

            elementType = interfaceType.GetGenericArguments()[0];
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Checks if a type is an array and returns the element type.
    /// </summary>
    public static bool IsArray(Type type, out Type elementType)
    {
        elementType = null;

        if (!type.IsArray) return false;

        elementType = type.GetElementType();
        return true;
    }

    /// <summary>
    ///     Creates position indexes for set operation fields.
    /// </summary>
    public static int[] CreateSetOperatorPositionIndexes(QueryNode node, string[] keys)
    {
        var indexes = new int[keys.Length];

        for (var i = 0; i < keys.Length; i++)
            indexes[i] = TryGetSetOperatorFieldPosition(node, keys[i], out var position) ? position : 0;

        return indexes;
    }

    public static bool TryGetSetOperatorFieldPosition(QueryNode node, string key, out int position)
    {
        position = 0;

        for (var i = 0; i < node.Select.Fields.Length; i++)
        {
            if (!MatchesSetOperatorKey(node.Select.Fields[i], key))
                continue;

            position = i;
            return true;
        }

        return false;
    }

    private static bool MatchesSetOperatorKey(FieldNode field, string key)
    {
        if (string.Equals(field.FieldName, key, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!string.IsNullOrWhiteSpace(field.FieldName) &&
            field.FieldName.EndsWith($".{key}", StringComparison.OrdinalIgnoreCase))
            return true;

        var expressionText = field.Expression.ToString();

        if (string.Equals(expressionText, key, StringComparison.OrdinalIgnoreCase))
            return true;

        return expressionText.EndsWith($".{key}", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsConstantExpression(Node expression)
    {
        return expression is IntegerNode
            or DecimalNode
            or WordNode
            or StringNode
            or NullNode;
    }

    internal static bool ContainsAggregateFunction(Node expression)
    {
        var stack = new Stack<Node>();
        stack.Push(expression);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            if (current is WindowFunctionNode)
                return true;

            if (current is AccessMethodNode methodNode && methodNode.IsAggregateMethod())
                return true;

            switch (current)
            {
                case AccessMethodNode method:
                    foreach (var arg in method.Arguments.Args)
                        stack.Push(arg);
                    if (method.ExtraAggregateArguments != null)
                        foreach (var arg in method.ExtraAggregateArguments.Args)
                            stack.Push(arg);
                    break;
                case BinaryNode binary:
                    stack.Push(binary.Left);
                    stack.Push(binary.Right);
                    break;
                case UnaryNode unary:
                    stack.Push(unary.Expression);
                    break;
                case FieldNode field:
                    stack.Push(field.Expression);
                    break;
                case CaseNode caseNode:
                    foreach (var whenThen in caseNode.WhenThenPairs)
                    {
                        stack.Push(whenThen.When);
                        stack.Push(whenThen.Then);
                    }

                    if (caseNode.Else != null)
                        stack.Push(caseNode.Else);
                    break;
            }
        }

        return false;
    }

    internal static void CollectColumnNames(Node expression, HashSet<string> columnNames)
    {
        var stack = new Stack<Node>();
        stack.Push(expression);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            switch (current)
            {
                case AccessColumnNode columnNode:
                    columnNames.Add(columnNode.Name);
                    break;
                case BinaryNode binary:
                    stack.Push(binary.Left);
                    stack.Push(binary.Right);
                    break;
                case UnaryNode unary:
                    stack.Push(unary.Expression);
                    break;
                case FieldNode field:
                    stack.Push(field.Expression);
                    break;
                case AccessMethodNode method:
                    foreach (var arg in method.Arguments.Args)
                        stack.Push(arg);
                    break;
            }
        }
    }

    internal static void FindNonGroupedColumns(
        Node expression,
        HashSet<string> groupByExpressions,
        HashSet<string> groupByColumnNames,
        List<string> nonGroupedColumns)
    {
        var stack = new Stack<Node>();
        stack.Push(expression);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            if (groupByExpressions.Contains(current.ToString()))
                continue;

            if (current is AccessMethodNode methodNode && methodNode.IsAggregateMethod())
                continue;

            switch (current)
            {
                case AccessColumnNode columnNode:
                    if (!groupByColumnNames.Contains(columnNode.Name))
                        nonGroupedColumns.Add(columnNode.Name);
                    break;
                case AccessMethodNode method:
                    foreach (var arg in method.Arguments.Args)
                        stack.Push(arg);
                    break;
                case BinaryNode binary:
                    stack.Push(binary.Left);
                    stack.Push(binary.Right);
                    break;
                case UnaryNode unary:
                    stack.Push(unary.Expression);
                    break;
                case FieldNode field:
                    stack.Push(field.Expression);
                    break;
                case CaseNode caseNode:
                    foreach (var whenThen in caseNode.WhenThenPairs)
                    {
                        stack.Push(whenThen.When);
                        stack.Push(whenThen.Then);
                    }

                    if (caseNode.Else != null)
                        stack.Push(caseNode.Else);
                    break;
            }
        }
    }

    internal static bool ReferencesConditionalField(Node expression, IReadOnlyList<SchemaFieldNode> contextFields)
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

    internal static Type InferComputedFieldType(Node expression, List<ISchemaColumn> contextColumns)
    {
        if (expression is EqualityNode or DiffNode or GreaterNode or GreaterOrEqualNode
            or LessNode or LessOrEqualNode or AndNode or OrNode)
            return typeof(bool);

        if (expression is WordNode)
            return typeof(string);

        if (expression is AccessMethodNode methodNode)
            if (methodNode.Name.Equals("ToString", StringComparison.OrdinalIgnoreCase))
                return typeof(string);

        if (expression is BinaryNode binaryNode)
        {
            var leftType = InferOperandType(binaryNode.Left, contextColumns);
            var rightType = InferOperandType(binaryNode.Right, contextColumns);

            if (expression is AddNode && (leftType == typeof(string) || rightType == typeof(string)))
                return typeof(string);

            if (BinaryOperatorTypeRules.IsNumericType(leftType) &&
                BinaryOperatorTypeRules.IsNumericType(rightType))
                return BinaryOperatorTypeRules.GetWiderNumericType(leftType, rightType);

            return typeof(int);
        }

        return typeof(object);
    }

    internal static Type InferOperandType(Node operand, List<ISchemaColumn> contextColumns)
    {
        if (operand is BinaryNode binaryOp) return InferComputedFieldType(binaryOp, contextColumns);

        if (operand is IdentifierNode identifier)
        {
            var column = contextColumns.FirstOrDefault(c =>
                c.ColumnName.Equals(identifier.Name, StringComparison.OrdinalIgnoreCase));
            return column?.ColumnType ?? typeof(object);
        }

        if (operand is IntegerNode) return typeof(int);

        if (operand is WordNode) return typeof(string);

        if (operand is AccessMethodNode methodNode)
            if (methodNode.Name.Equals("ToString", StringComparison.OrdinalIgnoreCase))
                return typeof(string);

        return typeof(object);
    }

    internal static ISchemaTable CreateEmptyTable()
    {
        return new DynamicTable([]);
    }

    internal static FieldNode[] ResolveFieldsForCache(FieldNode[] leftFields, FieldNode[] rightFields)
    {
        var resolved = new FieldNode[leftFields.Length];

        for (var i = 0; i < leftFields.Length; i++)
            resolved[i] = leftFields[i].Expression.ReturnType is NullNode.NullType
                ? rightFields[i]
                : leftFields[i];

        return resolved;
    }

    internal static void PrepareAndThrowUnknownColumnExceptionMessage(string identifier, ISchemaColumn[] columns,
        TextSpan span = default)
    {
        var library = new TransitionLibrary();
        var candidates = new StringBuilder();

        var candidatesColumns = columns.Where(col =>
            library.Soundex(col.ColumnName) == library.Soundex(identifier) ||
            library.LevenshteinDistance(col.ColumnName, identifier) < 3).ToArray();

        for (var i = 0; i < candidatesColumns.Length - 1; i++)
        {
            var candidate = candidatesColumns[i];
            candidates.Append(candidate.ColumnName);
            candidates.Append(", ");
        }

        if (candidatesColumns.Length > 0)
        {
            candidates.Append(candidatesColumns[^1].ColumnName);

            throw new UnknownColumnOrAliasException(
                identifier,
                $"Did you mean to use [{candidates}]?",
                span);
        }

        throw new UnknownColumnOrAliasException(identifier, string.Empty, span);
    }

    internal static void PrepareAndThrowUnknownPropertyExceptionMessage(string identifier, PropertyInfo[] properties,
        TextSpan span = default)
    {
        var library = new TransitionLibrary();
        var candidates = new StringBuilder();

        var candidatesProperties = properties.Where(prop =>
            library.Soundex(prop.Name) == library.Soundex(identifier) ||
            library.LevenshteinDistance(prop.Name, identifier) < 3).ToArray();

        for (var i = 0; i < candidatesProperties.Length - 1; i++)
        {
            var candidate = candidatesProperties[i];
            candidates.Append(candidate.Name);
            candidates.Append(", ");
        }

        if (candidatesProperties.Length > 0)
        {
            candidates.Append(candidatesProperties[^1].Name);

            throw new UnknownPropertyException(
                identifier,
                $"Did you mean to use [{candidates}]?",
                span);
        }

        throw new UnknownPropertyException(identifier, "unknown", span);
    }

    internal static bool IsInterpretFunction(string functionName)
    {
        return functionName.Equals("Interpret", StringComparison.OrdinalIgnoreCase) ||
               functionName.Equals("Parse", StringComparison.OrdinalIgnoreCase) ||
               functionName.Equals("InterpretAt", StringComparison.OrdinalIgnoreCase) ||
               functionName.Equals("TryInterpret", StringComparison.OrdinalIgnoreCase) ||
               functionName.Equals("TryParse", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsInterpretOrParseFunction(string methodName)
    {
        return methodName.Equals("Interpret", StringComparison.OrdinalIgnoreCase) ||
               methodName.Equals("Parse", StringComparison.OrdinalIgnoreCase) ||
               methodName.Equals("InterpretAt", StringComparison.OrdinalIgnoreCase) ||
               methodName.Equals("TryInterpret", StringComparison.OrdinalIgnoreCase) ||
               methodName.Equals("TryParse", StringComparison.OrdinalIgnoreCase) ||
               methodName.Equals("PartialInterpret", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool AreSameMethod(MethodInfo left, MethodInfo right)
    {
        return left.Module.Equals(right.Module) && left.MetadataToken == right.MetadataToken;
    }

    internal static Type[] GetArgumentTypes(ArgsListNode args)
    {
        var argTypes = new Type[args.Args.Length];

        for (var i = 0; i < args.Args.Length; i++)
            argTypes[i] = args.Args[i].ReturnType;

        return argTypes;
    }

    internal static Type[] GetGroupArgumentTypes(ArgsListNode args)
    {
        var groupArgCount = args.Args.Length > 0 ? args.Args.Length : 1;
        var groupArgTypes = new Type[groupArgCount];
        groupArgTypes[0] = typeof(string);

        for (var i = 1; i < args.Args.Length; i++)
            groupArgTypes[i] = args.Args[i].ReturnType;

        return groupArgTypes;
    }

    internal static string? ExtractSchemaNameFromArgs(ArgsListNode args, string? functionName = null)
    {
        var schemaArgIndex = functionName?.Equals("InterpretAt", StringComparison.OrdinalIgnoreCase) == true ? 2 : 1;

        if (args.Args.Length <= schemaArgIndex)
            throw new InvalidOperationException(
                $"Interpret function '{functionName ?? "unknown"}' requires at least {schemaArgIndex + 1} arguments, got {args.Args.Length}.");

        var schemaArg = args.Args[schemaArgIndex];

        if (schemaArg is StringNode stringNode)
            return stringNode.Value;

        if (schemaArg is WordNode wordNode)
            return wordNode.Value;

        if (schemaArg is IdentifierNode identifierNode)
            throw new InvalidOperationException(
                $"Schema name '{identifierNode.Name}' must be quoted. Use '{functionName ?? "Parse"}(source, \'{identifierNode.Name}\')' instead of '{functionName ?? "Parse"}(source, {identifierNode.Name})'.");

        throw new InvalidOperationException(
            $"Expected schema name as a quoted string at argument index {schemaArgIndex}, got {schemaArg?.GetType().Name ?? "null"}.");
    }

    internal static Exception SetOperatorDoesNotHaveKeysException(string setOperator)
    {
        return new SetOperatorMustHaveKeyColumnsException(setOperator);
    }

    private static readonly Dictionary<string, string> DialectColumnHints =
        new[] { "TOP", "FIRST", "LIMIT" }.ToDictionary(
            keyword => keyword,
            keyword => $"Musoq does not support {keyword}. Use TAKE after the FROM clause instead. " +
                       "Example: SELECT Name FROM #schema.method() alias TAKE 5",
            StringComparer.OrdinalIgnoreCase);

    internal static string? GetDialectColumnHint(string identifier)
    {
        return DialectColumnHints.GetValueOrDefault(identifier);
    }

    internal static bool TryReduceDimensions(MethodInfo method, ArgsListNode args, out MethodInfo reducedMethod)
    {
        var parameters = method.GetParameters();
        var paramsParameter = parameters
            .FirstOrDefault(f => f.GetCustomAttribute<ParamArrayAttribute>() != null);

        if (paramsParameter is null)
        {
            reducedMethod = null;
            return false;
        }

        var paramsParameterIndex = paramsParameter.Position;
        var typesToReduce = args.Args.Skip(paramsParameterIndex).Select(f => f.ReturnType).ToArray();

        var nonNullTypes = typesToReduce.Where(t => t is not NullNode.NullType).ToArray();

        Type typeToReduce;
        if (nonNullTypes.Length > 1)
            typeToReduce = nonNullTypes.First().MakeArrayType();
        else if (nonNullTypes.Length == 1)
            typeToReduce = nonNullTypes.First();
        else
            typeToReduce = typeof(object);

        var lastNonNullType = typeToReduce;
        while (typeToReduce is not null)
        {
            lastNonNullType = typeToReduce;
            typeToReduce = typeToReduce.GetElementType();
        }

        reducedMethod = method.MakeGenericMethod(lastNonNullType);
        return true;
    }

    internal static bool TryConstructGenericMethod(MethodInfo methodInfo, ArgsListNode args, Type entity,
        out MethodInfo constructedMethod)
    {
        var genericArguments = methodInfo.GetGenericArguments();
        var genericArgumentsDistinct = new List<Type>();
        var parameters = methodInfo.GetParameters();

        foreach (var genericArgument in genericArguments)
        {
            var i = 0;
            var shiftArgsWhenInjectSpecificSourcePresent = 0;

            if (parameters[0].GetCustomAttribute<InjectSpecificSourceAttribute>() != null)
            {
                i = 1;
                shiftArgsWhenInjectSpecificSourcePresent = 1;
                if ((genericArgument.IsGenericParameter || genericArgument.IsGenericMethodParameter) &&
                    parameters[0].ParameterType.IsGenericParameter) genericArgumentsDistinct.Add(entity);
            }

            for (; i < parameters.Length; i++)
            {
                var parameter = parameters[i];

                if (parameter.IsOptional &&
                    args.Args.Length < parameters.Length - shiftArgsWhenInjectSpecificSourcePresent) continue;

                var returnType = args.Args.Where((_, index) => index == i - shiftArgsWhenInjectSpecificSourcePresent)
                    .Single().ReturnType;
                var elementType = returnType.GetElementType();

                if (returnType.IsGenericType && parameter.ParameterType.IsGenericType &&
                    returnType.GetGenericTypeDefinition() == parameter.ParameterType.GetGenericTypeDefinition())
                {
                    genericArgumentsDistinct.Add(returnType.GetGenericArguments()[0]);
                    continue;
                }

                if (parameter.ParameterType.IsGenericType &&
                    parameter.ParameterType.IsAssignableTo(typeof(IEnumerable<>).MakeGenericType(genericArgument)) &&
                    elementType is not null)
                {
                    genericArgumentsDistinct.Add(elementType);
                    continue;
                }

                if (parameter.ParameterType.IsGenericType)
                {
                    var assignableInterfaces = returnType
                        .GetInterfaces()
                        .Where(type => type.IsConstructedGenericType)
                        .Select(type => new { type, definition = type.GetGenericTypeDefinition() })
                        .ToArray();

                    var firstAssignableInterface =
                        assignableInterfaces.FirstOrDefault(f => f.definition.IsAssignableFrom(typeof(IEnumerable<>)));

                    if (firstAssignableInterface is null) continue;

                    var elementTypeOfFirstAssignableInterface = firstAssignableInterface.type.GetElementType() ??
                                                                firstAssignableInterface.type.GetGenericArguments()[0];

                    genericArgumentsDistinct.Add(elementTypeOfFirstAssignableInterface);
                }

                if (parameter.ParameterType == genericArgument) genericArgumentsDistinct.Add(returnType);
            }
        }

        var hasNullType = genericArgumentsDistinct.Any(t => t is NullNode.NullType);

        var genericArgumentsConcreteTypes = genericArgumentsDistinct
            .Where(t => t is not NullNode.NullType)
            .Distinct()
            .ToArray();

        if (genericArgumentsConcreteTypes.Length == 0)
            genericArgumentsConcreteTypes = [typeof(object)];
        else if (hasNullType)
        {
            for (var i = 0; i < genericArgumentsConcreteTypes.Length; i++)
            {
                if (genericArgumentsConcreteTypes[i].IsValueType &&
                    Nullable.GetUnderlyingType(genericArgumentsConcreteTypes[i]) == null)
                {
                    genericArgumentsConcreteTypes[i] =
                        typeof(Nullable<>).MakeGenericType(genericArgumentsConcreteTypes[i]);
                }
            }
        }

        constructedMethod = methodInfo.MakeGenericMethod(genericArgumentsConcreteTypes);
        return true;
    }

    internal static ISchemaTable TurnTypeIntoTable(Type type)
    {
        var columns = new List<ISchemaColumn>();

        Type nestedType;
        if (type.IsArray)
        {
            nestedType = type.GetElementType();
        }
        else if (IsGenericEnumerable(type, out nestedType))
        {
        }
        else
        {
            throw new ColumnMustBeAnArrayOrImplementIEnumerableException();
        }

        if (nestedType == null) throw new InvalidOperationException("Element type is null.");

        if (nestedType.IsPrimitive || nestedType == typeof(string))
            return new DynamicTable([new SchemaColumn(nameof(PrimitiveTypeEntity<int>.Value), 0, nestedType)]);

        foreach (var property in nestedType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            columns.Add(new SchemaColumn(property.Name, columns.Count, property.PropertyType));

        return new DynamicTable(columns.ToArray(), nestedType);
    }

    internal static string GetArrayElementIntendedTypeName(string arrayIntendedTypeName)
    {
        if (string.IsNullOrEmpty(arrayIntendedTypeName))
            return null;

        if (arrayIntendedTypeName.EndsWith("[]"))
            return arrayIntendedTypeName.Substring(0, arrayIntendedTypeName.Length - 2);

        return arrayIntendedTypeName;
    }

    internal static void ValidateBindablePropertyAsTable(ISchemaTable table, ISchemaColumn targetColumn)
    {
        var propertyInfo = table.Metadata.TableEntityType.GetProperty(targetColumn.ColumnName);
        var bindablePropertyAsTableAttribute = propertyInfo?.GetCustomAttribute<BindablePropertyAsTableAttribute>();

        if (bindablePropertyAsTableAttribute == null) return;

        var isValid = IsGenericEnumerable(propertyInfo!.PropertyType, out var elementType) ||
                      IsArray(propertyInfo.PropertyType!, out elementType) ||
                      (elementType != null && (elementType.IsPrimitive || elementType == typeof(string)));

        if (!isValid) throw new ColumnMustBeMarkedAsBindablePropertyAsTableException();
    }

    internal static Type FollowProperties(Type type, PropertyFromNode.PropertyNameAndTypePair[] propertiesChain)
    {
        var propertiesWithoutColumnType = propertiesChain.Skip(1);

        foreach (var property in propertiesWithoutColumnType)
        {
            var propertyInfo = type.GetProperty(property.PropertyName);

            if (propertyInfo == null)
            {
                PrepareAndThrowUnknownPropertyExceptionMessage(property.PropertyName, type.GetProperties());
                return null;
            }

            type = propertyInfo.PropertyType;
        }

        return type;
    }

    private static readonly Dictionary<Type, DynamicObjectPropertyTypeHintAttribute[]> TypeHintAttributeCache = new();

    internal static DynamicObjectPropertyTypeHintAttribute[] GetCachedTypeHintAttributes(Type type)
    {
        lock (TypeHintAttributeCache)
        {
            if (TypeHintAttributeCache.TryGetValue(type, out var cached))
                return cached;

            var attributes = type.GetCustomAttributes<DynamicObjectPropertyTypeHintAttribute>().ToArray();
            TypeHintAttributeCache[type] = attributes;
            return attributes;
        }
    }
}
