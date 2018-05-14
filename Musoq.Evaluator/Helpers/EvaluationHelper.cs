using System;
using System.Collections.Generic;
using System.Text;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Helpers
{
    public static class EvaluationHelper
    {
        public static TableRowSource ConvertTableToSource(Table table) => new TableRowSource(table);

        public static string GetCastableType(Type type)
        {
            if (type.IsGenericType)
            {
                return GetFriendlyTypeName(type);
            }

            return $"{type.Namespace}.{type.Name}";
        }

        public static Type[] GetNestedTypes(Type type)
        {
            if (!type.IsGenericType)
                return new []{ type };

            var types = new Stack<Type>();

            types.Push(type);
            var finalTypes = new List<Type>();

            while (types.Count > 0)
            {
                var cType = types.Pop();
                finalTypes.Add(cType);

                if (cType.IsGenericType)
                {
                    foreach (var argType in cType.GetGenericArguments())
                        types.Push(argType);
                }
            }

            return finalTypes.ToArray();
        }

        /// <summary>
        /// From http://stackoverflow.com/questions/401681/how-can-i-get-the-correct-text-definition-of-a-generic-type-using-reflection
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static string GetFriendlyTypeName(Type type)
        {
            if (type.IsGenericParameter)
            {
                return type.Name;
            }

            if (!type.IsGenericType)
            {
                return type.FullName;
            }

            var builder = new StringBuilder();
            var name = type.Name;
            var index = name.IndexOf("`");
            builder.AppendFormat("{0}.{1}", type.Namespace, name.Substring(0, index));
            builder.Append('<');
            var first = true;
            foreach (var arg in type.GetGenericArguments())
            {
                if (!first)
                {
                    builder.Append(',');
                }
                builder.Append(GetFriendlyTypeName(arg));
                first = false;
            }
            builder.Append('>');
            return builder.ToString();
        }

    }
}
