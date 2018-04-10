using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web.Http;
using CacheManager.Core;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Plugins.Helpers;
using Musoq.Service.Client;
using Musoq.Service.Environment;
using Musoq.Service.Logging;
using Musoq.Service.Models;
using Musoq.Service.Visitors;

namespace Musoq.Service.Controllers
{
    public class RuntimeController : ApiController
    {
        private readonly IDictionary<Guid, QueryContext> _contexts;
        private readonly IDictionary<Guid, ExecutionState> _runetimeState;
        private readonly ICacheManager<VirtualMachine> _expressionsCache;
        private readonly IServiceLogger _logger;

        public RuntimeController(IDictionary<Guid, QueryContext> contexts,
            IDictionary<Guid, ExecutionState> runetimeState,
            ICacheManager<VirtualMachine> expressionsCache, IServiceLogger logger)
        {
            _contexts = contexts;
            _runetimeState = runetimeState;
            _expressionsCache = expressionsCache;
            _logger = logger;
        }

        [HttpPost]
        public Guid Execute([FromUri] Guid id)
        {
            if (!_contexts.TryGetValue(id, out var context))
                return Guid.Empty;

            _logger.Log($"Executing task: {id}.");

            var query = context.Query;
            var state = new ExecutionState
            {
                Status = ExecutionStatus.WaitingToStart
            };

            var taskId = Guid.NewGuid();
            _runetimeState.Add(taskId, state);

            state.Task = Task.Factory.StartNew(() =>
            {
                state.Status = ExecutionStatus.Running;

                var env = new Plugins.Environment();

                env.SetValue(EnvironmentServiceHelper.PluginsFolderKey, ApplicationConfiguration.PluginsFolder);
                env.SetValue(EnvironmentServiceHelper.HttpServerAddressKey, ApplicationConfiguration.HttpServerAdress);
                env.SetValue(EnvironmentServiceHelper.ServerAddressKey, ApplicationConfiguration.ServerAddress);
                env.SetValue(EnvironmentServiceHelper.TempFolderKey, Path.Combine(Path.GetTempPath(), id.ToString()));

                try
                {
                    var watch = new Stopwatch();
                    watch.Start();

                    var root = InstanceCreator.CreateTree(query);

                    var cacheKeyCreator = new QueryCacheStringifier();
                    var traverser = new QueryCacheStringifierTraverser(cacheKeyCreator);

                    root.Accept(traverser);

                    var key = cacheKeyCreator.CacheKey;
                    var hash = HashHelper.ComputeHash<MD5CryptoServiceProvider>(key);

                    if (!_expressionsCache.TryGetOrAdd(hash,
                        (s) => InstanceCreator.Create(root, new DynamicSchemaProvider()), out var vm))
                        vm = InstanceCreator.Create(root, new DynamicSchemaProvider());

                    var compiledTime = watch.Elapsed;
                    state.Result = vm.Execute();
                    var executionTime = watch.Elapsed;

                    watch.Stop();

                    state.CompilationTime = compiledTime;
                    state.ExecutionTime = executionTime;
                    state.Status = ExecutionStatus.Success;
                }
                catch (Exception exc)
                {
                    state.Status = ExecutionStatus.Failure;
                    state.FailureMessage = exc.ToString();
                    _logger.Log(exc);
                }
                finally
                {
                    var dirInfo = new DirectoryInfo(env.Value<string>(EnvironmentServiceHelper.TempFolderKey));

                    if (dirInfo.Exists)
                        dirInfo.Delete();
                }
            });

            return taskId;
        }

        [HttpGet]
        public ResultTable Result([FromUri] Guid id)
        {
            _logger.Log($"Returning result for: {id}.");

            var state = _runetimeState[id];
            var table = state.Result;
            var computationTime = state.ExecutionTime;
            var columns = table.Columns.Select(f => f.Name).ToArray();
            var rows = table.Select(f => f.Values).ToArray();

            _runetimeState.Remove(id);

            return new ResultTable(table.Name, columns, rows, computationTime);
        }

        [HttpGet]
        public (bool HasContext, ExecutionStatus Status) Status([FromUri] Guid id)
        {
            _logger.Log($"Checking status for: {id}.");

            if (!_runetimeState.ContainsKey(id))
                return (false, ExecutionStatus.WaitingToStart);

            return (true, _runetimeState[id].Status);
        }
    }
}