using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Web.Http.Dependencies;
using FQL.Service.Client;
using FQL.Service.Controllers;
using FQL.Service.Models;

namespace FQL.Service.Resolvers
{
    public class CustomDependencyResolver : IDependencyResolver
    {
        private readonly IDictionary<Guid, QueryContext> _contexts;
        private readonly IDictionary<Guid, ExecutionState> _states;

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
                    return new ContextController(_contexts);
                case nameof(RuntimeController):
                    return new RuntimeController(_contexts, _states);
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