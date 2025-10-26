using System;
using System.Reflection;
using System.Threading.Tasks;
using Musoq.Schema.DataSources;
using Musoq.Schema.Reflection;

namespace Musoq.Schema;

public interface ISchema
{
    string Name { get; }

    ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters);

    RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters);

    SchemaMethodInfo[] GetRawConstructors(RuntimeContext runtimeContext);

    SchemaMethodInfo[] GetRawConstructors(string methodName, RuntimeContext runtimeContext);

    bool TryResolveMethod(string method, Type[] parameters, Type entityType, out MethodInfo methodInfo);

    bool TryResolveRawMethod(string method, Type[] parameters, out MethodInfo methodInfo);

    bool TryResolveAggregationMethod(string method, Type[] parameters, Type entityType, out MethodInfo methodInfo);
}