using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using CacheManager.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Plugins.Helpers;
using Musoq.Service.Client.Core;
using Musoq.Service.Core.Logging;
using Musoq.Service.Core.Models;
using Musoq.Service.Core.Visitors;
using Musoq.Service.Environment.Core;

namespace Musoq.Service.Core.Controllers
{
    public class RuntimeController : ControllerBase
    {
        private readonly IDictionary<Guid, QueryContext> _contexts;
        private readonly IMemoryCache _expressionsCache;
        private readonly IServiceLogger _logger;
        private readonly IDictionary<Guid, ExecutionState> _runetimeState;
        private readonly IDictionary<string, Type> _schemas;

        public RuntimeController(
            QueryContextDictionary contexts,
            ExecutionStateDictionary runetimeState,
            IMemoryCache expressionsCache, IServiceLogger logger,
            LoadedSchemasDictionary schemas)
        {
            _contexts = contexts;
            _runetimeState = runetimeState;
            _expressionsCache = expressionsCache;
            _logger = logger;
            _schemas = schemas;
        }

        [HttpPost]
        public Guid Execute(Guid id)
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
                    var hash = HashHelper.ComputeHash<MD5CryptoServiceProvider>(query);

                    if (!_expressionsCache.TryGetValue(hash, out CompiledQuery vm))
                        vm = InstanceCreator.Create(root, new DynamicSchemaProvider(_schemas));

                    _expressionsCache.Set(hash, vm);

                    var compiledTime = watch.Elapsed;
                    state.Result = vm.Run();
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

                    _logger.Log("Query processed.");
                }
            });

            return taskId;
        }

        [HttpGet]
        public ResultTable Result(Guid id)
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
        public (bool HasContext, ExecutionStatus Status) Status(Guid id)
        {
            _logger.Log($"Checking status for: {id}.");

            if (!_runetimeState.ContainsKey(id))
                return (false, ExecutionStatus.WaitingToStart);

            return (true, _runetimeState[id].Status);
        }
    }
}