using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Web.Http.Dependencies;
using CacheManager.Core;
using Musoq.Evaluator;
using Musoq.Schema;
using Musoq.Service.Client.Core;
using Musoq.Service.Core.Controllers;
using Musoq.Service.Core.Helpers;
using Musoq.Service.Core.Logging;
using Musoq.Service.Core.Models;

namespace Musoq.Service.Core.Resolvers
{
    public class CustomDependencyResolver : IDependencyResolver
    {
        private readonly IDictionary<Guid, QueryContext> _contexts;

        private readonly ICacheManager<CompiledQuery> _expressionsCache = CacheFactory.Build<CompiledQuery>(
            "evaluatedExpressions",
            settings => { settings.WithSystemRuntimeCacheHandle("exps"); });

        private readonly IDictionary<Guid, ExecutionState> _states;

        private IDictionary<string, Type> LoadedSchemas;

        public CustomDependencyResolver()
        {
            _contexts = new ConcurrentDictionary<Guid, QueryContext>();
            _states = new ConcurrentDictionary<Guid, ExecutionState>();
            LoadSchemas();
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
                    return new RuntimeController(_contexts, _states, _expressionsCache, ServiceLogger.Instance,
                        LoadedSchemas);
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

        private void LoadSchemas()
        {
            LoadedSchemas = new ConcurrentDictionary<string, Type>();

            var types = new List<Type>();

            types.AddRange(PluginsLoader.LoadDllBasedSchemas());

            foreach (var type in types)
                try
                {
                    ServiceLogger.Instance.Log($"Attempting to load plugin {type.Name}");
                    var schema = (ISchema) Activator.CreateInstance(type);
                    LoadedSchemas.Add($"#{schema.Name.ToLowerInvariant()}", type);
                }
                catch (Exception e)
                {
                    ServiceLogger.Instance.Log(e);
                }
        }
    }
}