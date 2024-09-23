using System;
using System.Reflection;
using Musoq.Schema.DataSources;

namespace Musoq.Schema;

public interface ISchema
{
    string Name { get; }

    ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters);

    RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters);

    Reflection.SchemaMethodInfo[] GetConstructors(string methodName);

    Reflection.SchemaMethodInfo[] GetConstructors();

    Reflection.SchemaMethodInfo[] GetRawConstructors();

    Reflection.SchemaMethodInfo[] GetRawConstructors(string methodName);

    bool TryResolveMethod(string method, Type[] parameters, Type entityType, out MethodInfo methodInfo);

    bool TryResolveRawMethod(string method, Type[] parameters, out MethodInfo methodInfo);

    bool TryResolveAggregationMethod(string method, Type[] parameters, Type entityType, out MethodInfo methodInfo);
}