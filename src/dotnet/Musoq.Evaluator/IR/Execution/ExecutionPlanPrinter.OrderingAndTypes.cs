using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Musoq.Evaluator.IR.Execution;

public static partial class ExecutionPlanPrinter
{
    private static string FormatDirection(bool descending) => descending ? " DESC" : " ASC";

    private static string FormatDesc(ExecutionReturnDesc desc)
    {
        if (desc.Type == Musoq.Evaluator.IR.Logical.Nodes.DescType.Query)
            return "query Query";

        var schemaName = string.IsNullOrWhiteSpace(desc.SchemaName)
            ? "#?"
            : desc.SchemaName.StartsWith('#')
                ? desc.SchemaName
                : $"#{desc.SchemaName}";
        var methodName = string.IsNullOrWhiteSpace(desc.MethodName)
            ? string.Empty
            : $".{desc.MethodName}()";
        var column = string.IsNullOrWhiteSpace(desc.Column)
            ? string.Empty
            : $" column {desc.Column}";
        var arguments = desc.Arguments.Count == 0
            ? string.Empty
            : $" args ({string.Join(", ", desc.Arguments.Select(FormatExpression))})";

        return $"{schemaName}{methodName} {desc.Type}{column}{arguments}";
    }

    private static string FormatOrderFields(IReadOnlyList<ExecutionOrderField> fields)
    {
        var builder = new StringBuilder();

        for (var index = 0; index < fields.Count; index++)
        {
            if (index > 0)
                builder.Append(", ");

            var field = fields[index];
            builder.Append(field.FieldName);
            builder.Append(field.Descending ? " DESC" : " ASC").Append(FormatNullOrdering(field.NullOrdering));
        }

        return builder.ToString();
    }

    private static string FormatFieldIndexes(IReadOnlyList<int> fieldIndexes)
    {
        return string.Join(", ", fieldIndexes.Select(index => index.ToString(CultureInfo.InvariantCulture)));
    }

    private static string FormatOrderRecordSelection(ExecutionOrderRecordSelection selection)
    {
        return selection switch
        {
            ExecutionFullOrderRecordSelection => string.Empty,
            ExecutionTakeOrderRecordSelection take => $", take {take.Count.ToString(CultureInfo.InvariantCulture)}",
            ExecutionSkipTakeOrderRecordSelection skipTake => $", skip {skipTake.SkipCount.ToString(CultureInfo.InvariantCulture)}, take {skipTake.TakeCount.ToString(CultureInfo.InvariantCulture)}",
            _ => $", {selection.GetType().Name}"
        };
    }

    private static string FormatType(Type type)
    {
        var nullableType = Nullable.GetUnderlyingType(type);
        if (nullableType != null)
            return $"{FormatType(nullableType)}?";

        if (TypeAliases.TryGetValue(type, out var alias))
            return alias;

        if (type.IsArray)
            return $"{FormatType(type.GetElementType()!)}[]";

        if (!type.IsGenericType)
            return type.Name;

        var typeName = type.Name[..type.Name.IndexOf('`', StringComparison.Ordinal)];
        var arguments = string.Join(", ", Array.ConvertAll(type.GetGenericArguments(), FormatType));
        return $"{typeName}<{arguments}>";
    }

    private static string FormatType(ExecutionTypeRef type) => FormatType(type.ClrType);

    private static readonly FrozenDictionary<Type, string> TypeAliases = new Dictionary<Type, string>()
    {
        [typeof(bool)] = "bool",
        [typeof(byte)] = "byte",
        [typeof(sbyte)] = "sbyte",
        [typeof(short)] = "short",
        [typeof(ushort)] = "ushort",
        [typeof(int)] = "int",
        [typeof(uint)] = "uint",
        [typeof(long)] = "long",
        [typeof(ulong)] = "ulong",
        [typeof(float)] = "float",
        [typeof(double)] = "double",
        [typeof(decimal)] = "decimal",
        [typeof(char)] = "char",
        [typeof(string)] = "string",
        [typeof(object)] = "object"
    }.ToFrozenDictionary();
}
