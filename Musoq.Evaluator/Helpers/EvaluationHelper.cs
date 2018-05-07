using System;
using System.Collections.Generic;
using System.Text;
using Musoq.Evaluator.Instructions;
using Musoq.Evaluator.Instructions.Converts;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Helpers
{
    public static class EvaluationHelper
    {
        private static readonly IDictionary<(Type, Type), Instruction[]> _convertionsMap;
        static EvaluationHelper()
        {
            _convertionsMap = new Dictionary<(Type, Type), Instruction[]>
            {
                {(typeof(decimal), typeof(decimal)), new Instruction[0]},
                {(typeof(decimal), typeof(long)), new Instruction[] {new ConvertToDecimal()}},
                {(typeof(decimal), typeof(int)), new Instruction[] {new ConvertToDecimal()}},
                {(typeof(decimal), typeof(short)), new Instruction[] {new ConvertToDecimal()}},
                {(typeof(long), typeof(decimal)), new Instruction[] {new ConvertToDecimal()}},
                {(typeof(long), typeof(long)), new Instruction[0]},
                {(typeof(long), typeof(int)), new Instruction[0]},
                {(typeof(long), typeof(short)), new Instruction[0]},
                {(typeof(int), typeof(decimal)), new Instruction[] {new ConvertToDecimal()}},
                {(typeof(int), typeof(long)), new Instruction[0]},
                {(typeof(int), typeof(int)), new Instruction[0]},
                {(typeof(int), typeof(short)), new Instruction[0]},
                {(typeof(short), typeof(decimal)), new Instruction[] {new ConvertToDecimal()}},
                {(typeof(short), typeof(long)), new Instruction[0]},
                {(typeof(short), typeof(int)), new Instruction[0]},
                {(typeof(short), typeof(short)), new Instruction[0]},
                {(typeof(string), typeof(string)), new Instruction[0]},
                {(typeof(bool), typeof(bool)), new Instruction[0]},
                {(typeof(DateTimeOffset), typeof(DateTimeOffset)), new Instruction[0]}
            };
        }

        public static Instruction[] GetConvertingInstructions(Type left, Type right)
        {
            return _convertionsMap[(left, right)];
        }

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
