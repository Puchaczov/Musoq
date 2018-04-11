using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Web.Http.Dependencies;
using CacheManager.Core;
using Musoq.Evaluator;
using Musoq.Schema;
using Musoq.Service.Client;
using Musoq.Service.Controllers;
using Musoq.Service.Helpers;
using Musoq.Service.Logging;
using Musoq.Service.Models;

namespace Musoq.Service.Resolvers
{
    public class CustomDependencyResolver : IDependencyResolver
    {
        private readonly IDictionary<Guid, QueryContext> _contexts;
        private readonly IDictionary<Guid, ExecutionState> _states;

        public IDictionary<string, Type> _loadedSchemas;

        private readonly ICacheManager<VirtualMachine> _expressionsCache = CacheFactory.Build<VirtualMachine>("evaluatedExpressions",
            settings => { settings.WithSystemRuntimeCacheHandle("exps"); });

        public CustomDependencyResolver()
        {
            _contexts = new ConcurrentDictionary<Guid, QueryContext>();
            _states = new ConcurrentDictionary<Guid, ExecutionState>();
            LoadSchemas();
        }

        private void LoadSchemas()
        {
            _loadedSchemas = new ConcurrentDictionary<string, Type>();

            var types = new List<Type>();

            types.AddRange(PluginsLoader.LoadDllBasedSchemas());

            foreach (var type in types)
            {
                try
                {
                    ServiceLogger.Instance.Log($"Attempting to load plugin {type.Name}");
                    var schema = (ISchema)Activator.CreateInstance(type);
                    _loadedSchemas.Add($"#{schema.Name.ToLowerInvariant()}", type);
                }
                catch (Exception e)
                {
                    ServiceLogger.Instance.Log(e);
                }
            }
        }

        public void Dispose()
        {
        }

        public object GetService(Type serviceType)
        {
            var name = serviceType.Name;

            switch (name)
            {
                case nameof(ContextController):
                    return new ContextController(_contexts, ServiceLogger.Instance);
                case nameof(RuntimeController):
                    return new RuntimeController(_contexts, _states, _expressionsCache, ServiceLogger.Instance, _loadedSchemas);
                case nameof(SelfController):
                    return new SelfController();
            }

            return null;
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            yield break;
        }

        public IDependencyScope BeginScope()
        {
            return this;
        }
    }
}