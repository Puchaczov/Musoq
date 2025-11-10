using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Musoq.Evaluator.Tables;
using Musoq.Parser.Nodes;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Reflection;

namespace Musoq.Evaluator.Helpers;

public static class EvaluationHelper
{
    public static RowSource ConvertEnumerableToSource<T>(IEnumerable<T> enumerable)
    {
        if (typeof(T).IsPrimitive || typeof(T) == typeof(string))
        {
            return new GenericRowsSource<PrimitiveTypeEntity<T>>(enumerable.Select(f => new PrimitiveTypeEntity<T>(f)));
        }
            
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

    public static Table GetSpecificTableDescription(ISchemaTable table)
    {
        var newTable = new Table("desc", [
            new Column("Name", typeof(string), 0),
            new Column("Index", typeof(int), 1),
            new Column("Type", typeof(string), 2)
        ]);

        foreach (var column in table.Columns)
        {
            foreach (var complexField in CreateTypeComplexDescription(column.ColumnName, column.ColumnType))
            {
                newTable.Add(new ObjectsRow([complexField.FieldName, column.ColumnIndex, complexField.Type.FullName]));
            }
                
        }

        return newTable;
    }

    public static Table GetSpecificSchemaDescriptions(ISchema schema, RuntimeContext runtimeContext)
    {
        return CreateTableFromConstructors(() => schema.GetRawConstructors(runtimeContext));
    }

    public static Table GetConstructorsForSpecificMethod(ISchema schema, string methodName, RuntimeContext runtimeContext)
    {
        return CreateTableFromConstructors(() => schema.GetRawConstructors(methodName, runtimeContext));
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
            {
                row.Add($"{param.Name}: {param.Type.FullName}");
            }
        }

        maxColumns += 1;

        foreach (var row in values)
        {
            if (maxColumns > row.Count)
            {
                row.AddRange(new string[maxColumns - row.Count]);
            }
        }

        var columns = new Column[maxColumns];
        columns[0] = new Column("Name", typeof(string), 0);

        for (int i = 1; i < columns.Length; i++)
        {
            columns[i] = new Column($"Param {i - 1}", typeof(string), i);
        }

        var descTable = new Table("desc", columns);

        foreach (var row in values)
        {
            descTable.Add(new ObjectsRow(row.ToArray()));
        }

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

            if(current.Level > 3)
                continue;

            if (current.Type.IsPrimitive || current.Type == typeof(string) || current.Type == typeof(object))
                continue;

            foreach (var prop in current.Type.GetProperties())
            {
                if (prop.MemberType != MemberTypes.Property)
                    continue;

                if (prop.PropertyType.IsPrimitive || prop.PropertyType == typeof(string) || prop.PropertyType == typeof(object))
                    continue;

                var complexName = $"{current.FieldName}.{prop.Name}";
                output.Add((complexName, prop.PropertyType));

                if(prop.PropertyType == current.Type)
                    continue;

                fields.Enqueue((complexName, prop.PropertyType, current.Level + 1));
            }
        }

        return output;
    }

    public static string GetCastableType(Type type)
    {
        if (type is NullNode.NullType) return "System.Object";
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

        if (!type.IsGenericType) return ReplacePlusWithDotForNestedClasses(type.FullName);

        var builder = new StringBuilder();
        var name = type.Name;
        var index = name.IndexOf('`');
        builder.Append($"{type.Namespace}.{name[..index]}");
        builder.Append('<');
        var first = true;
        foreach (var arg in type.GetGenericArguments())
        {
            if (!first) builder.Append(',');
            builder.Append(GetFriendlyTypeName(arg));
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
        switch (typeName.ToLowerInvariant())
        {
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
            case "guid":
                return "System.Guid";
        }

        return typeName;
    }

    public static Type RemapPrimitiveTypeAsNullable(string typeName)
    {
        switch (typeName)
        {
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
            case "System.Guid":
                return typeof(Guid?);
        }

        return Type.GetType(typeName);
    }
        
    private class GenericRowsSource<T>(IEnumerable<T> entities) : RowSource
    {
        // ReSharper disable once StaticMemberInGenericType
        private static IReadOnlyDictionary<string, int> NameToIndexMap { get; }

        private static IReadOnlyDictionary<int, Func<T, object>> IndexToObjectAccessMap { get; }

        public override IEnumerable<IObjectResolver> Rows => entities.Select(entity => new EntityResolver<T>(entity, NameToIndexMap, IndexToObjectAccessMap));

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
    }

    private class ListRowSource(List<Group> list) : RowSource
    {
        public override IEnumerable<IObjectResolver> Rows => list.Select(item => new GroupResolver(item));
    }

    private class GroupResolver(Group item) : IObjectResolver
    {
        public object[] Contexts => [];

        public object this[string name] => name == "none" ? item : item.GetValue<object>(name);

        public object this[int index] => null;

        public bool HasColumn(string name)
        {
            return false;
        }
    }
}