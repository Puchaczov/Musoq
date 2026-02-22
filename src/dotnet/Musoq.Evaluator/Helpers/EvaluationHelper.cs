using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tables;
using Musoq.Parser.Nodes;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Helpers;
using Musoq.Schema.Reflection;
using Group = Musoq.Plugins.Group;

namespace Musoq.Evaluator.Helpers;

public static class EvaluationHelper
{
    private static readonly ConcurrentDictionary<Type, string> CastableTypeCache = new();
    private static readonly ConcurrentDictionary<string, XmlDocument> XmlDocCache = new();
    private static readonly Regex WhitespaceNormalizerRegex = new(@"\s+", RegexOptions.Compiled);

    public static RowSource ConvertEnumerableToSource<T>(IEnumerable<T> enumerable)
    {
        if (enumerable is null)
            return new GenericRowsSource<T>(Enumerable.Empty<T>());

        if (typeof(T).IsPrimitive || typeof(T) == typeof(string))
            return new GenericRowsSource<PrimitiveTypeEntity<T>>(enumerable.Select(f => new PrimitiveTypeEntity<T>(f)));

        return new GenericRowsSource<T>(enumerable);
    }

    public static RowSource ConvertTableToSource(Table table, bool skipContext)
    {
        return new TableRowSource(table, skipContext);
    }

    public static RowSource ConvertTableToSource(List<Group> list)
    {
        return new ListRowSource(list);
    }

    public static Table ToDistinctTable(Table table)
    {
        var distinctTable = new Table(table.Name, table.Columns.ToArray());
        var seenRows = new HashSet<Row>(table.Count);

        foreach (var row in table)
            if (seenRows.Add(row))
                distinctTable.Add(row);

        return distinctTable;
    }

    public static IEnumerable<T> WrapScalarForCrossApply<T>(T value, bool isCrossApply) where T : class
    {
        if (value == null)
            return isCrossApply ? [] : [null!];

        return [value];
    }

    public static Table GetSpecificTableDescription(ISchemaTable table)
    {
        var newTable = new Table("desc", [
            new Column("Name", typeof(string), 0),
            new Column("Index", typeof(int), 1),
            new Column("Type", typeof(string), 2)
        ]);

        foreach (var column in table.Columns)
        foreach (var complexField in CreateTypeComplexDescription(column.ColumnName, column.ColumnType))
            newTable.Add(new ObjectsRow([complexField.FieldName, column.ColumnIndex, complexField.Type.FullName]));

        return newTable;
    }

    public static Table GetSpecificColumnDescription(ISchemaTable table, string columnName)
    {
        var newTable = new Table("desc", [
            new Column("Name", typeof(string), 0),
            new Column("Index", typeof(int), 1),
            new Column("Type", typeof(string), 2)
        ]);


        var pathParts = columnName.Split('.');
        var rootColumnName = pathParts[0];


        var targetColumn = table.Columns.FirstOrDefault(c =>
            string.Equals(c.ColumnName, rootColumnName, StringComparison.OrdinalIgnoreCase));

        if (targetColumn == null)
            throw new UnknownColumnOrAliasException($"Column '{rootColumnName}' does not exist in the table.");


        var canonicalPathParts = new List<string> { targetColumn.ColumnName };


        var currentType = targetColumn.ColumnType;


        for (var i = 1; i < pathParts.Length; i++)
        {
            if (currentType.IsArray)
                currentType = currentType.GetElementType()!;
            else if (IsGenericEnumerable(currentType, out var elementTypeFromEnumerable))
                currentType = elementTypeFromEnumerable;

            var propertyName = pathParts[i];
            var property = currentType.GetProperties()
                .FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase));

            if (property == null)
                throw new UnknownColumnOrAliasException(
                    $"Property '{propertyName}' does not exist on type '{currentType.Name}'.");

            canonicalPathParts.Add(property.Name);
            currentType = property.PropertyType;
        }

        var canonicalPath = string.Join(".", canonicalPathParts);


        Type elementType;

        if (currentType.IsArray)
            elementType = currentType.GetElementType()!;
        else if (IsGenericEnumerable(currentType, out var genericElementType))
            elementType = genericElementType;
        else if (currentType.IsPrimitive || currentType == typeof(string) || currentType == typeof(object))
            throw new ColumnMustBeAnArrayOrImplementIEnumerableException();
        else
            elementType = currentType;


        var prefixLength = canonicalPath.Length;

        foreach (var complexField in CreateTypeComplexDescription(canonicalPath, elementType))
        {
            var relativeFieldName =
                complexField.FieldName.Length > prefixLength && complexField.FieldName[prefixLength] == '.'
                    ? complexField.FieldName.Substring(prefixLength + 1)
                    : complexField.FieldName == canonicalPath
                        ? canonicalPath.Substring(canonicalPath.LastIndexOf('.') + 1)
                        : complexField.FieldName;

            newTable.Add(new ObjectsRow([relativeFieldName, targetColumn.ColumnIndex, complexField.Type.FullName]));
        }

        return newTable;
    }

    private static bool IsGenericEnumerable(Type type, out Type elementType)
    {
        elementType = null!;

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

    public static Table GetSpecificSchemaDescriptions(ISchema schema, RuntimeContext runtimeContext)
    {
        return CreateTableFromConstructors(() => schema.GetRawConstructors(runtimeContext));
    }

    public static Table GetConstructorsForSpecificMethod(ISchema schema, string methodName,
        RuntimeContext runtimeContext)
    {
        return CreateTableFromConstructors(() => schema.GetRawConstructors(methodName, runtimeContext));
    }

    public static Table GetMethodsForSchema(ISchema schema, RuntimeContext runtimeContext)
    {
        runtimeContext.EndWorkToken.ThrowIfCancellationRequested();

        var libraryMethods = schema.GetAllLibraryMethods();

        var newTable = new Table("desc", [
            new Column("Method", typeof(string), 0),
            new Column("Description", typeof(string), 1),
            new Column("Category", typeof(string), 2),
            new Column("Source", typeof(string), 3)
        ]);


        var methodRows =
            new List<(string Signature, string Description, string Category, string Source, int SortOrder)>();

        foreach (var (methodName, methodInfos) in libraryMethods)
        foreach (var methodInfo in methodInfos)
        {
            runtimeContext.EndWorkToken.ThrowIfCancellationRequested();

            var bindableAttr = methodInfo.GetCustomAttribute<BindableMethodAttribute>();
            if (bindableAttr?.IsInternal == true)
                continue;

            if (methodInfo.GetCustomAttribute<AggregationSetMethodAttribute>() != null)
                continue;

            var signature = CSharpTypeNameHelper.FormatMethodSignature(methodInfo);
            var description = GetXmlDocumentation(methodInfo);
            var category = GetMethodCategory(methodInfo);
            var source = GetMethodSource(methodInfo);
            var sortOrder = source == "Schema" ? 0 : 1;

            methodRows.Add((signature, description, category, source, sortOrder));
        }


        var sortedRows = methodRows
            .OrderBy(row => row.SortOrder)
            .ThenBy(row => row.Category)
            .ThenBy(row => row.Signature);

        foreach (var row in sortedRows)
            newTable.Add(new ObjectsRow([row.Signature, row.Description, row.Category, row.Source]));

        return newTable;
    }

    private static string GetMethodCategory(MethodInfo methodInfo)
    {
        var categoryAttr = methodInfo.GetCustomAttribute<MethodCategoryAttribute>();
        return categoryAttr?.Category ?? "Unknown";
    }

    private static string GetMethodSource(MethodInfo methodInfo)
    {
        var declaringType = methodInfo.DeclaringType;
        if (declaringType == null)
            return "Unknown";


        if (declaringType == typeof(LibraryBase))
            return "Library";


        return "Schema";
    }

    private static string GetXmlDocumentation(MethodInfo methodInfo)
    {
        try
        {
            var assembly = methodInfo.DeclaringType?.Assembly;
            if (assembly == null)
                return string.Empty;

            var assemblyPath = assembly.Location;
            if (string.IsNullOrEmpty(assemblyPath))
                return string.Empty;

            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");

            var xmlDoc = XmlDocCache.GetOrAdd(xmlPath, static path =>
            {
                if (!File.Exists(path))
                    return null;

                var doc = new XmlDocument();
                doc.Load(path);
                return doc;
            });

            if (xmlDoc == null)
                return string.Empty;

            var memberName = GetMemberName(methodInfo);
            var node = xmlDoc.SelectSingleNode($"//member[@name='{memberName}']/summary");

            if (node == null)
                return string.Empty;

            var text = node.InnerText.Trim();
            text = WhitespaceNormalizerRegex.Replace(text, " ");

            return text;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string GetMemberName(MethodInfo method)
    {
        var declaringType = method.DeclaringType;
        if (declaringType == null)
            return string.Empty;

        var sb = new StringBuilder();
        sb.Append("M:");
        sb.Append(declaringType.FullName);
        sb.Append('.');
        sb.Append(method.Name);

        var parameters = method.GetParameters();
        if (parameters.Length <= 0)
            return sb.ToString();

        sb.Append('(');
        for (var i = 0; i < parameters.Length; i++)
        {
            if (i > 0)
                sb.Append(',');

            var paramType = parameters[i].ParameterType;
            sb.Append(GetTypeName(paramType));
        }

        sb.Append(')');

        return sb.ToString();
    }

    private static string GetTypeName(Type type)
    {
        if (type.IsGenericType)
        {
            var genericTypeName = type.GetGenericTypeDefinition().FullName;
            var tickIndex = genericTypeName.IndexOf('`');
            if (tickIndex > 0)
                genericTypeName = genericTypeName.Substring(0, tickIndex);

            var genericArgs = type.GetGenericArguments();
            return $"{genericTypeName}{{{string.Join(",", genericArgs.Select(GetTypeName))}}}";
        }

        if (!type.IsArray)
            return type.FullName ?? type.Name;

        var elementType = type.GetElementType();
        return GetTypeName(elementType) + "[]";
    }

    private static Table CreateTableFromConstructors(Func<SchemaMethodInfo[]> getConstructors)
    {
        var maxColumns = 0;
        var values = new List<List<object>>();

        foreach (var constructor in getConstructors())
        {
            var row = new List<object>();
            values.Add(row);

            row.Add(constructor.MethodName);

            if (constructor.ConstructorInfo.Arguments.Length > maxColumns)
                maxColumns = constructor.ConstructorInfo.Arguments.Length;

            foreach (var param in constructor.ConstructorInfo.Arguments)
                row.Add($"{param.Name}: {param.Type.FullName}");
        }

        maxColumns += 1;

        foreach (var row in values)
            if (maxColumns > row.Count)
                row.AddRange(new string[maxColumns - row.Count]);

        var columns = new Column[maxColumns];
        columns[0] = new Column("Name", typeof(string), 0);

        for (var i = 1; i < columns.Length; i++) columns[i] = new Column($"Param {i - 1}", typeof(string), i);

        var descTable = new Table("desc", columns);

        foreach (var row in values) descTable.Add(new ObjectsRow(row.ToArray()));

        return descTable;
    }

    public static IEnumerable<(string FieldName, Type Type)> CreateTypeComplexDescription(
        string initialFieldName, Type type)
    {
        var output = new List<(string FieldName, Type Type)>();
        var fields = new Queue<(string FieldName, Type Type, int Level)>();

        fields.Enqueue((initialFieldName, type, 0));
        output.Add((initialFieldName, type));

        while (fields.Count > 0)
        {
            var current = fields.Dequeue();

            if (current.Level > 3)
                continue;


            if (current.Type.IsPrimitive || current.Type == typeof(string) || current.Type == typeof(object))
                continue;

            foreach (var prop in current.Type.GetProperties())
            {
                if (prop.MemberType != MemberTypes.Property)
                    continue;

                var complexName = $"{current.FieldName}.{prop.Name}";


                output.Add((complexName, prop.PropertyType));


                // Note: We only skip arrays, not other IEnumerable types like List<T> or Dictionary<K,V>

                if (prop.PropertyType.IsArray)
                    continue;


                if (prop.PropertyType.IsPrimitive || prop.PropertyType == typeof(string) ||
                    prop.PropertyType == typeof(object))
                    continue;


                if (prop.PropertyType == current.Type)
                    continue;

                fields.Enqueue((complexName, prop.PropertyType, current.Level + 1));
            }
        }

        return output;
    }

    public static string GetCastableType(Type type)
    {
        if (type is NullNode.NullType) return "object";


        if (type == typeof(string)) return "string";
        if (type == typeof(int)) return "int";
        if (type == typeof(long)) return "long";
        if (type == typeof(short)) return "short";
        if (type == typeof(byte)) return "byte";
        if (type == typeof(ulong)) return "ulong";
        if (type == typeof(uint)) return "uint";
        if (type == typeof(ushort)) return "ushort";
        if (type == typeof(sbyte)) return "sbyte";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(decimal)) return "decimal";
        if (type == typeof(double)) return "double";
        if (type == typeof(float)) return "float";
        if (type == typeof(char)) return "char";
        if (type == typeof(object)) return "object";
        if (type == typeof(void)) return "void";


        return CastableTypeCache.GetOrAdd(type, ComputeCastableType);
    }

    private static string ComputeCastableType(Type type)
    {
        if (type.IsGenericType) return GetFriendlyTypeName(type);
        if (type.IsNested) return $"{GetCastableType(type.DeclaringType)}.{type.Name}";

        return ReplacePlusWithDotForNestedClasses(type.FullName);
    }

    public static Type[] GetNestedTypes(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type), @"Type cannot be null");

        if (!type.IsGenericType)
            return [type];

        var types = new Stack<Type>();

        types.Push(type);
        var finalTypes = new List<Type>();

        while (types.Count > 0)
        {
            var cType = types.Pop();
            finalTypes.Add(cType);

            if (cType.IsGenericType)
                foreach (var argType in cType.GetGenericArguments())
                    types.Push(argType);
        }

        return finalTypes.ToArray();
    }

    private static string GetFriendlyTypeName(Type type)
    {
        if (type.IsGenericParameter) return type.Name;

        if (!type.IsGenericType) return GetCastableType(type);

        var builder = new StringBuilder();
        var name = type.Name;
        var index = name.IndexOf('`');
        builder.Append($"{type.Namespace}.{name[..index]}");
        builder.Append('<');
        var first = true;
        foreach (var arg in type.GetGenericArguments())
        {
            if (!first) builder.Append(", ");
            builder.Append(GetCastableType(arg));
            first = false;
        }

        builder.Append('>');
        return builder.ToString();
    }

    private static string ReplacePlusWithDotForNestedClasses(string fullName)
    {
        return fullName.Replace("+", ".");
    }

    public static string RemapPrimitiveTypes(string typeName)
    {
        if (typeName.EndsWith("?"))
        {
            var baseType = RemapPrimitiveTypes(typeName[..^1]);
            return $"System.Nullable`1[{baseType}]";
        }

        switch (typeName.ToLowerInvariant())
        {
            case "byte":
                return "System.Byte";
            case "sbyte":
                return "System.SByte";
            case "short":
                return "System.Int16";
            case "int":
                return "System.Int32";
            case "long":
                return "System.Int64";
            case "ushort":
                return "System.UInt16";
            case "uint":
                return "System.UInt32";
            case "ulong":
                return "System.UInt64";
            case "string":
                return "System.String";
            case "char":
                return "System.Char";
            case "boolean":
            case "bool":
            case "bit":
                return "System.Boolean";
            case "float":
                return "System.Single";
            case "double":
                return "System.Double";
            case "decimal":
            case "money":
                return "System.Decimal";
            case "object":
                return "System.Object";
            case "datetime":
                return "System.DateTime";
            case "datetimeoffset":
                return "System.DateTimeOffset";
            case "timespan":
                return "System.TimeSpan";
            case "guid":
                return "System.Guid";
        }

        return typeName;
    }

    public static Type RemapPrimitiveTypeAsNullable(string typeName)
    {
        switch (typeName)
        {
            case "System.Byte":
                return typeof(byte?);
            case "System.SByte":
                return typeof(sbyte?);
            case "System.Int16":
                return typeof(short?);
            case "System.Int32":
                return typeof(int?);
            case "System.Int64":
                return typeof(long?);
            case "System.UInt16":
                return typeof(ushort?);
            case "System.UInt32":
                return typeof(uint?);
            case "System.UInt64":
                return typeof(ulong?);
            case "System.String":
                return typeof(string);
            case "System.Char":
                return typeof(char?);
            case "System.Boolean":
                return typeof(bool?);
            case "System.Single":
                return typeof(float?);
            case "System.Double":
                return typeof(double?);
            case "System.Decimal":
                return typeof(decimal?);
            case "System.Object":
                return typeof(object);
            case "System.DateTime":
                return typeof(DateTime?);
            case "System.DateTimeOffset":
                return typeof(DateTimeOffset?);
            case "System.TimeSpan":
                return typeof(TimeSpan?);
            case "System.Guid":
                return typeof(Guid?);
        }

        return Type.GetType(typeName);
    }

    /// <summary>
    ///     Flattens multiple context arrays into a single array, handling null contexts by inserting a null value.
    /// </summary>
    /// <param name="contexts">The context arrays to flatten.</param>
    /// <returns>
    ///     A single flattened array containing all context objects from the input arrays, with nulls for any null context
    ///     arrays.
    /// </returns>
    public static object[] FlattenContexts(params object[][] contexts)
    {
        var size = 0;
        for (var i = 0; i < contexts.Length; i++)
            if (contexts[i] != null)
                size += contexts[i].Length;
            else
                size += 1;

        var result = new object[size];
        var offset = 0;
        for (var i = 0; i < contexts.Length; i++)
            if (contexts[i] != null)
            {
                Array.Copy(contexts[i], 0, result, offset, contexts[i].Length);
                offset += contexts[i].Length;
            }
            else
            {
                result[offset++] = null;
            }

        return result;
    }

    private class GenericRowsSource<T>(IEnumerable<T> entities) : RowSource
    {
        static GenericRowsSource()
        {
            var type = typeof(T);
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var nameToIndexMap = new Dictionary<string, int>();
            var indexToObjectAccessMap = new Dictionary<int, Func<T, object>>();

            for (var i = 0; i < properties.Length; i++)
            {
                var property = properties[i];

                nameToIndexMap.Add(property.Name, i);
                indexToObjectAccessMap.Add(i, entity => property.GetValue(entity));
            }

            NameToIndexMap = nameToIndexMap;
            IndexToObjectAccessMap = indexToObjectAccessMap;
        }

        // ReSharper disable once StaticMemberInGenericType
        private static IReadOnlyDictionary<string, int> NameToIndexMap { get; }

        private static IReadOnlyDictionary<int, Func<T, object>> IndexToObjectAccessMap { get; }

        public override IEnumerable<IObjectResolver> Rows => entities.Select<T, IObjectResolver>(entity =>
        {
            // For object arrays, check if the actual runtime object is a dictionary (ExpandoObject)
            if (typeof(T) == typeof(object) && entity is IDictionary<string, object> dict)
                return new DynamicObjectResolver(dict);

            // For object arrays with dynamically generated types (e.g., interpreters),
            // use reflection on the actual runtime type
            if (typeof(T) == typeof(object) && entity != null && NameToIndexMap.Count == 0)
                return new DynamicTypeResolver(entity);

            return new EntityResolver<T>(entity, NameToIndexMap, IndexToObjectAccessMap);
        });
    }

    /// <summary>
    ///     Resolver for dynamically generated types (interpreter types).
    ///     Uses reflection on the actual runtime type.
    /// </summary>
    private class DynamicTypeResolver : IObjectResolver
    {
        private readonly object _entity;
        private readonly IReadOnlyDictionary<string, PropertyInfo> _propertyMap;

        public DynamicTypeResolver(object entity)
        {
            _entity = entity;
            var type = entity.GetType();
            _propertyMap = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .ToDictionary(p => p.Name, p => p);
            Contexts = [entity];
        }

        public object[] Contexts { get; }

        public object this[string name] => _propertyMap.TryGetValue(name, out var prop) ? prop.GetValue(_entity) : null;

        public object this[int index] => null;

        public bool HasColumn(string name)
        {
            return _propertyMap.ContainsKey(name);
        }
    }

    /// <summary>
    ///     Resolver for dynamic objects (ExpandoObject) that implements IDictionary&lt;string, object&gt;.
    /// </summary>
    private class DynamicObjectResolver : IObjectResolver
    {
        private readonly IDictionary<string, object> _dict;

        public DynamicObjectResolver(IDictionary<string, object> dict)
        {
            _dict = dict;
            Contexts = [dict];
        }

        public object[] Contexts { get; }

        public object this[string name] => _dict.TryGetValue(name, out var value) ? value : null;

        public object this[int index] => null;

        public bool HasColumn(string name)
        {
            return _dict.ContainsKey(name);
        }
    }

    private class ListRowSource(List<Group> list) : RowSource
    {
        public override IEnumerable<IObjectResolver> Rows => list.Select(item => new GroupResolver(item));
    }

    private class GroupResolver : IObjectResolver
    {
        private static readonly object[] EmptyContexts = [];
        private readonly Group _item;

        public GroupResolver(Group item)
        {
            _item = item;
        }

        public object[] Contexts => EmptyContexts;

        public object this[string name] => name == "none" ? _item : _item.GetValue<object>(name);

        public object this[int index] => null;

        public bool HasColumn(string name)
        {
            return false;
        }
    }
}
