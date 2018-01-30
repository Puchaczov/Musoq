using System;
using System.Reflection;
using FQL.Schema.DataSources;

namespace FQL.Schema
{
    public interface ISchema
    {
        string Name { get; }

        ISchemaTable GetTableByName(string name, string[] parameters);

        RowSource GetRowSource(string name, string[] parameters);

        MethodInfo ResolveMethod(string method, Type[] parameters);

        MethodInfo ResolveAggregationMethod(string method, Type[] parameters);

        bool TryResolveAggreationMethod(string method, Type[] parameters, out MethodInfo methodInfo);

        MethodInfo ResolveProperty(string property);
    }
}