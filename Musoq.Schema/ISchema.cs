using System;
using System.Reflection;
using Musoq.Schema.DataSources;

namespace Musoq.Schema
{
    public interface ISchema
    {
        string Name { get; }

        ISchemaTable GetTableByName(string name, params object[] parameters);

        RowSource GetRowSource(string name, params object[] parameters);

        MethodInfo ResolveMethod(string method, Type[] parameters);

        bool TryResolveAggreationMethod(string method, Type[] parameters, out MethodInfo methodInfo);
    }
}