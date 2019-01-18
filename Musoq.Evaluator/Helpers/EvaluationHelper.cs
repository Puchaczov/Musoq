using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Reflection;

namespace Musoq.Evaluator.Helpers
{
    public static class EvaluationHelper
    {
        public static RowSource ConvertTableToSource(Table table)
        {
            return new TableRowSource(table);
        }

        public static RowSource ConvertTableToSource(List<Group> list)
        {
            return new ListRowSource(list);
        }

        public static Table GetSpecificTableDescription(ISchemaTable table)
        {
            var newTable = new Table("desc", new[]
            {
                new Column("Name", typeof(string), 0),
                new Column("Index", typeof(int), 1),
                new Column("Type", typeof(string), 2)
            });

            foreach (var column in table.Columns)
            {
                foreach (var complexField in CreateTypeComplexDescription(column.ColumnName, column.ColumnType))
                {
                    newTable.Add(new ObjectsRow(new object[]{ complexField.FieldName, column.ColumnIndex, complexField.Type.FullName }));
                }
                
            }

            return newTable;
        }

        public static Table GetSpecificSchemaDescriptions(ISchema schema)
        {
            return CreateTableFromConstructors(() => schema.GetRawConstructors());
        }

        public static Table GetConstructorsForSpecificMethod(ISchema schema, string methodName)
        {
            return CreateTableFromConstructors(() => schema.GetRawConstructors(methodName));
        }

        private static Table CreateTableFromConstructors(Func<SchemaMethodInfo[]> getConstructors)
        {
            var maxColumns = 0;
            var values = new List<List<string>>();

            foreach (var constructor in getConstructors())
            {
                var row = new List<string>();
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

                foreach (var prop in current.Type.GetProperties())
                {
                    if (prop.MemberType != MemberTypes.Property)
                        continue;

                    var complexName = $"{current.FieldName}.{prop.Name}";
                    output.Add((complexName, prop.PropertyType));

                    if(prop.PropertyType == current.Type)
                        continue;

                    if (!(prop.PropertyType.IsPrimitive || prop.PropertyType == typeof(string) || prop.PropertyType == typeof(object)))
                    {
                        fields.Enqueue((complexName, prop.PropertyType, current.Level + 1));
                    }
                }
            }

            return output;
        }

        public static string GetCastableType(Type type)
        {
            if (type.IsGenericType) return GetFriendlyTypeName(type);

            return $"{type.Namespace}.{type.Name}";
        }

        public static Type[] GetNestedTypes(Type type)
        {
            if (!type.IsGenericType)
                return new[] {type};

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

            if (!type.IsGenericType) return type.FullName;

            var builder = new StringBuilder();
            var name = type.Name;
            var index = name.IndexOf("`", StringComparison.Ordinal);
            builder.AppendFormat("{0}.{1}", type.Namespace, name.Substring(0, index));
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
    }

    public class ListRowSource : RowSource
    {
        private readonly List<Group> _list;

        public ListRowSource(List<Group> list)
        {
            _list = list;
        }

        public override IEnumerable<IObjectResolver> Rows
        {
            get
            {
                foreach (var item in _list)
                    yield return new GroupResolver(item);
            }
        }
    }

    public class GroupResolver : IObjectResolver
    {
        private readonly Group _item;

        public GroupResolver(Group item)
        {
            _item = item;
        }

        public object Context { get; }

        public object this[string name]
        {
            get
            {
                if (name == "none")
                    return _item;

                return _item.GetValue<object>(name);
            }
        }

        public object this[int index] => null;

        public bool HasColumn(string name)
        {
            return false;
        }
    }
}