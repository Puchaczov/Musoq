using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using CacheManager.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Musoq.Evaluator;
using Musoq.Schema;
using Musoq.Service.Client.Core;
using Musoq.Service.Core.Logging;
using Musoq.Service.Core.Models;
using Musoq.Service.Core.Windows.Helpers;
using Musoq.Service.Core.Windows.Plugins;

namespace Musoq.Service.Core.Windows
{
    public class ApiStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            var loadedSchemas = new LoadedSchemasDictionary();

            var types = new List<Type>();

            types.AddRange(PluginsLoader.LoadDllBasedSchemas());
            types.Add(typeof(SchemaPlugins));

            foreach (var type in types)
            {
                try
                {
                    ServiceLogger.Instance.Log($"Attempting to load plugin {type.Name}");

                    var schema = (ISchema)Activator.CreateInstance(type);
                    loadedSchemas.TryAdd($"#{schema.Name.ToLowerInvariant()}", type);
                }
                catch (Exception e)
                {
                    ServiceLogger.Instance.Log(e);
                }
            }

            services.AddMemoryCache();
            services.AddSingleton(typeof(QueryContextDictionary), new QueryContextDictionary());
            services.AddSingleton(typeof(ExecutionStateDictionary), new ExecutionStateDictionary());
            services.AddSingleton(typeof(LoadedSchemasDictionary), loadedSchemas);
            services.AddSingleton(typeof(IServiceLogger), ServiceLogger.Instance);
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder appBuilder, IHostingEnvironment environment)
        {
            appBuilder.UseMvc(f => f.MapRoute(name: "default", template: "api/{controller}/{action}/{id?}/{key?}/{status?}"));
        }
    }
}