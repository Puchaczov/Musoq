using System;
using System.Linq;
using System.Reflection;

namespace Musoq.Schema.Helpers
{
    public static class TypeHelper
    {
        /// <summary>
        ///     Gets the typename from type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>Name of type.</returns>
        public static string GetTypeName(this Type type)
        {
            return GetUnderlyingNullable(type).Name;
        }

        /// <summary>
        ///     Gets internal type of Nullable or type if not nullable.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The type.</returns>
        public static Type GetUnderlyingNullable(this Type type)
        {
            var nullableType = Nullable.GetUnderlyingType(type);

            var isNullableType = nullableType != null;

            return isNullableType ? nullableType : type;
        }

        /// <summary>
        ///     Gets the optional parameters count.
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static int CountOptionalParameters(this ParameterInfo[] parameters)
        {
            return parameters.Count(f => f.IsOptional);
        }

        /// <summary>
        ///     Gets amount of parameters that are not marked by some attribute.
        /// </summary>
        /// <typeparam name="TType">The attribute type</typeparam>
        /// <param name="parameters">The Parameters</param>
        /// <returns>Count of parameters that aren't marked by attribute</returns>
        public static int CountWithoutParametersAnnotatedBy<TType>(this ParameterInfo[] parameters)
            where TType : Attribute
        {
            return parameters.Count(f => f.GetCustomAttribute<TType>() == null);
        }

        /// <summary>
        ///     Gets parameters that doesn't have annotation of type TType
        /// </summary>
        /// <typeparam name="TType">The annotation</typeparam>
        /// <param name="parameters">Parameters to filter</param>
        /// <returns>Parameters that fit the requirements.</returns>
        public static ParameterInfo[] GetParametersWithoutAttribute<TType>(this ParameterInfo[] parameters)
            where TType : Attribute
        {
            return parameters.Where(f => f.GetCustomAttribute<TType>() == null).ToArray();
        }

        /// <summary>
        ///     Gets the parameters that are annotated by some attribute
        /// </summary>
        /// <typeparam name="TType">The type.</typeparam>
        /// <param name="parameters">Parameters that will be filtered.</param>
        /// <returns>Array of parameters that specify condition.</returns>
        public static ParameterInfo[] GetParametersWithAttribute<TType>(this ParameterInfo[] parameters)
            where TType : Attribute
        {
            return parameters.Where(f => f.GetCustomAttribute<TType>() != null).ToArray();
        }
    }
}