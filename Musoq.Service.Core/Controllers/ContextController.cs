using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Service.Client;
using Musoq.Service.Client.Core;
using Musoq.Service.Core.Logging;

namespace Musoq.Service.Core.Controllers
{
    public class ContextController : ControllerBase
    {
        private readonly QueryContextDictionary _contexts;
        private readonly IServiceLogger _logger;

        public ContextController(QueryContextDictionary contexts, IServiceLogger logger)
        {
            _contexts = contexts;
            _logger = logger;
        }

        [HttpPost]
        public Guid Create([FromBody] QueryContext context)
        {
            _logger.Log($"Creating context ({context.Query}).");
            var id = Guid.NewGuid();
            _contexts.TryAdd(id, context);
            return id;
        }

        [HttpGet]
        public Method[] Methods()
        {
            var methods = new List<Method>();
            foreach (var library in new LibraryBase[]
            {
                /*TO DO*/
            })
            {
                var bindableMethods = library.GetType().GetMethods()
                    .Where(f => f.GetCustomAttribute<BindableMethodAttribute>() != null);

                foreach (var method in bindableMethods)
                    methods.Add(new Method
                    {
                        Name = method.Name,
                        ReturnType = method.ReturnType.Name,
                        Args = method
                            .GetParameters()
                            .Where(f => f.GetCustomAttribute<InjectTypeAttribute>() == null)
                            .Select(f => f.ParameterType.Name)
                            .ToArray()
                    });
            }

            return methods.ToArray();
        }
    }
}