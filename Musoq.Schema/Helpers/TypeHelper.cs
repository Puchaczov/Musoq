﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Musoq.Schema.Attributes;
using Musoq.Schema.DataSources;

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
        /// Checks if the can be considered as contextual value type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>True if is pure value type. False if is Nullable[T] type or is reference type.</returns>
        public static bool IsTrueValueType(this Type type)
        {
            var isValueType = type.IsValueType;

            if (!isValueType)
                return false;

            var isNullableType = Nullable.GetUnderlyingType(type) != null;

            if (isNullableType)
                return false;

            return true;
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
        /// Determine if method has parameters.
        /// </summary>
        /// <param name="paramsParameters"></param>
        /// <returns>True if has parameters, otherwise false.</returns>
        public static bool HasParameters(this ParameterInfo[] paramsParameters)
        {
            return paramsParameters != null && paramsParameters.Length > 0;
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
            return parameters.Where(f =>
            {
                var attributes = f.GetCustomAttributes();
                return attributes.Any(g => g.GetType().IsAssignableTo(typeof(TType)));
            }).ToArray();
        }
        
        /// <summary>
        /// Gets the parameters that are annotated by some attribute
        /// </summary>
        /// <param name="parameterInfo">Parameter that attributes will be filtered.</param>
        /// <typeparam name="TAttribute">Base type.</typeparam>
        /// <returns>Attribute that specify condition.</returns>
        public static TAttribute GetCustomAttributeThatInherits<TAttribute>(this ParameterInfo parameterInfo)
            where TAttribute : Attribute
        {
            var attributes = parameterInfo.GetCustomAttributes();
            var foundAttribute = attributes.FirstOrDefault(g => g.GetType().IsAssignableTo(typeof(TAttribute)));
            return (TAttribute)foundAttribute;
        }

        /// <summary>
        ///     Gets the dictionaries describing the type.
        /// </summary>
        /// <typeparam name="TType">Type to describe.</typeparam>
        /// <returns>Mapped entity.</returns>
        public static (IDictionary<string, int> NameToIndexMap, IDictionary<int, Func<TType, object>> IndexToMethodAccessMap, ISchemaColumn[] Columns) GetEntityMap<TType>()
        {
            var columnIndex = 0;

            var nameToIndexMap = new Dictionary<string, int>();
            var indexToMethodAccess = new Dictionary<int, Func<TType, object>>();
            var columns = new List<ISchemaColumn>();

            var type = typeof(TType);
            foreach (var member in type.GetMembers())
            {
                if (member.GetCustomAttribute<EntityPropertyAttribute>() == null)
                    continue;

                if(member.MemberType != MemberTypes.Property)
                    continue;

                var property = (PropertyInfo)member;

                Func<TType, object> del;
                if (property.PropertyType.IsValueType)
                {
                    var dynMethod = new DynamicMethod($"Dynamic_Get_{typeof(TType).Name}_{property.Name}", typeof(object), new[] { typeof(TType) }, typeof(TType).Module);
                    var ilGen = dynMethod.GetILGenerator();
                    ilGen.Emit(OpCodes.Ldarg_0);
                    ilGen.Emit(OpCodes.Callvirt, property.GetGetMethod());
                    ilGen.Emit(OpCodes.Box, property.PropertyType);
                    ilGen.Emit(OpCodes.Ret);

                    del = (Func<TType, object>)dynMethod.CreateDelegate(typeof(Func<TType, object>));
                }
                else
                {
                    del = (Func<TType, object>)Delegate.CreateDelegate(typeof(Func<TType, object>), null, property.GetGetMethod());
                }

                nameToIndexMap.Add(property.Name, columnIndex);
                indexToMethodAccess.Add(columnIndex, (instance) => del(instance));
                columns.Add(new SchemaColumn(property.Name, columnIndex, property.PropertyType));

                columnIndex += 1;
            }

            return (nameToIndexMap, indexToMethodAccess, columns.ToArray());
        }

        public static (bool SupportsInterCommunicator, (string Name, Type Type)[] Parameters) GetParametersForConstructor(ConstructorInfo constructor)
        {
            var parameters = constructor.GetParameters();
            var filteredConstructors = new List<(string Name, Type Type)>();
            var supportsInterCommunicator = false;

            foreach(var param in parameters)
            {
                if (param.ParameterType != typeof(RuntimeContext))
                    filteredConstructors.Add((param.Name, param.ParameterType));
                else
                    supportsInterCommunicator = true;
            }

            return (supportsInterCommunicator, filteredConstructors.ToArray());
        }

        public static Reflection.ConstructorInfo[] GetConstructorsFor<TType>()
        {
            var constructors = new List<Reflection.ConstructorInfo>();

            var type = typeof(TType);
            var allConstructors = type.GetConstructors();

            foreach (var constr in allConstructors)
            {
                var paramsInfo = GetParametersForConstructor(constr);
                constructors.Add(new Reflection.ConstructorInfo(constr, type, paramsInfo.SupportsInterCommunicator, paramsInfo.Parameters));
            }

            return constructors.ToArray();
        }

        public static Reflection.SchemaMethodInfo[] GetSchemaMethodInfosForType<TType>(string typeIdentifier)
        {
            return GetConstructorsFor<TType>().Select(constr => new Reflection.SchemaMethodInfo(typeIdentifier, constr)).ToArray();
        }
    }
}