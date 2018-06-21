using System;
using System.Reflection;
using Musoq.Plugins.Attributes;
using Musoq.Schema.Managers;

namespace Musoq.Schema.DataSources
{
    public abstract class SchemaBase : ISchema
    {
        private readonly MethodsAggregator _aggregator;

        protected SchemaBase(string name, MethodsAggregator methodsAggregator)
        {
            Name = name;
            _aggregator = methodsAggregator;
        }

        public string Name { get; }

        public abstract ISchemaTable GetTableByName(string name, params object[] parameters);

        public abstract RowSource GetRowSource(string name, params object[] parameters);

        public bool TryResolveAggreationMethod(string method, Type[] parameters, out MethodInfo methodInfo)
        {
            var founded = _aggregator.TryResolveMethod(method, parameters, out methodInfo);

            if (founded)
                return methodInfo.GetCustomAttribute<AggregationMethodAttribute>() != null;

            return false;
        }

        public MethodInfo ResolveMethod(string method, Type[] parameters)
        {
            return _aggregator.ResolveMethod(method, parameters);
        }
    }
}