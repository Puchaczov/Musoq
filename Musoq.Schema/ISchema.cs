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

    SchemaMethodInfo[] GetConstructors(string methodName);

    SchemaMethodInfo[] GetConstructors();

    SchemaMethodInfo[] GetRawConstructors();

    SchemaMethodInfo[] GetRawConstructors(string methodName);

    bool TryResolveMethod(string method, Type[] parameters, Type entityType, out MethodInfo methodInfo);

    bool TryResolveRawMethod(string method, Type[] parameters, out MethodInfo methodInfo);

    bool TryResolveAggregationMethod(string method, Type[] parameters, Type entityType, out MethodInfo methodInfo);

    static virtual Task LoadRequiredDependenciesAsync()
    {
        // The default implementation does nothing and it is intended as most of the schemas won't require any additional dependencies.
        return Task.CompletedTask;
    }
}