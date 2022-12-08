using System;
using System.Reflection;
using Musoq.Schema.DataSources;

namespace Musoq.Schema
{
    public interface ISchema
    {
        string Name { get; }

        ISchemaTable GetTableByName(string name, params object[] parameters);

        Reflection.SchemaMethodInfo[] GetConstructors(string methodName);

        Reflection.SchemaMethodInfo[] GetConstructors();

        Reflection.SchemaMethodInfo[] GetRawConstructors();

        Reflection.SchemaMethodInfo[] GetRawConstructors(string methodName);

        RowSource GetRowSource(string name, RuntimeContext interCommunicator, params object[] parameters);

        bool TryResolveMethod(string method, Type[] parameters, out MethodInfo methodInfo);

        bool TryResolveRawMethod(string method, Type[] parameters, out MethodInfo methodInfo);

        bool TryResolveAggregationMethod(string method, Type[] parameters, out MethodInfo methodInfo);
    }
}