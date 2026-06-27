using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.Helpers;

public static partial class EvaluationHelper
{
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
}
