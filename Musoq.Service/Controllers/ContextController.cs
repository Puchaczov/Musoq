using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using FQL.Plugins;
using FQL.Plugins.Attributes;
using Musoq.Service.Client;

namespace Musoq.Service.Controllers
{
    public class ContextController : ApiController
    {
        private readonly IDictionary<Guid, QueryContext> _contexts;

        public ContextController(IDictionary<Guid, QueryContext> contexts)
        {
            _contexts = contexts;
        }

        [HttpPost]
        public Guid Create([FromBody] QueryContext context)
        {
            Console.WriteLine("Creating context.");
            var id = Guid.NewGuid();
            _contexts.Add(id, context);
            return id;
        }

        [HttpGet]
        public Method[] Methods()
        {
            var methods = new List<Method>();
            foreach (var library in new LibraryBase[] { /*TO DO*/ })
            {
                var bindableMethods = library.GetType().GetMethods()
                    .Where(f => f.GetCustomAttribute<BindableMethodAttribute>() != null);

                foreach (var method in bindableMethods)
                    methods.Add(new Method
                    {
                        Name = method.Name,
                        ReturnType = method.ReturnType.Name,
                        Args = method.GetParameters().Where(f => f.GetCustomAttribute<InjectTypeAttribute>() == null)
                            .Select(f => f.ParameterType.Name).ToArray()
                    });
            }

            return methods.ToArray();
        }
    }
}