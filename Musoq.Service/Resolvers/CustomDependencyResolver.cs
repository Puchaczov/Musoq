using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Web.Http.Dependencies;
using CacheManager.Core;
using Musoq.Evaluator;
using Musoq.Service.Client;
using Musoq.Service.Controllers;
using Musoq.Service.Logging;
using Musoq.Service.Models;

namespace Musoq.Service.Resolvers
{
    public class CustomDependencyResolver : IDependencyResolver
    {
        private readonly IDictionary<Guid, QueryContext> _contexts;
        private readonly IDictionary<Guid, ExecutionState> _states;

        private readonly ICacheManager<VirtualMachine> _expressionsCache = CacheFactory.Build<VirtualMachine>("evaluatedExpressions",
            settings => { settings.WithSystemRuntimeCacheHandle("exps"); });

        public CustomDependencyResolver()
        {
            _contexts = new ConcurrentDictionary<Guid, QueryContext>();
            _states = new ConcurrentDictionary<Guid, ExecutionState>();
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
                    return new ContextController(_contexts, new ServiceLogger());
                case nameof(RuntimeController):
                    return new RuntimeController(_contexts, _states, _expressionsCache, new ServiceLogger());
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