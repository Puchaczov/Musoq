using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
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
        public IActionResult Compile([FromBody] QueryContext context)
        {
            var query = context.Query;
            var id = context.QueryId;

            ExecutionState state;
            var hash = HashHelper.ComputeHash<MD5CryptoServiceProvider>(query);

            lock (_runetimeState)
            {
                if (_runetimeState.TryGetValue(id, out var runtimeState))
                {
                    if (hash == runtimeState.HashedQuery)
                        return Ok();

                    _expressionsCache.Remove(hash);
                    state = runtimeState;

                }
                else
                {
                    state = new ExecutionState();
                    _runetimeState.Add(id, state);
                }
            }

            if (!state.MakeActionedOrFalse())
                return StatusCode((int)HttpStatusCode.NotModified);

            state.Status = ExecutionStatus.WaitingToStart;

            state.Task = Task.Factory.StartNew(() =>
            {
                state.Status = ExecutionStatus.Compiling;

                try
                {
                    var watch = new Stopwatch();
                    watch.Start();

                    var root = InstanceCreator.CreateTree(query);

                    if (!_expressionsCache.TryGetValue(hash, out CompiledQuery vm))
                        vm = InstanceCreator.Create(root, new DynamicSchemaProvider(_schemas));

                    _expressionsCache.Set(hash, vm);

                    state.HashedQuery = hash;
                    state.CompilationTime = watch.Elapsed;
                    state.Status = ExecutionStatus.Compiled;
                }
                catch (Exception exc)
                {
                    state.Status = ExecutionStatus.Failure;
                    state.FailureMessage = exc.ToString();
                    _logger.Log(exc);
                }
                finally
                {
                    state.MakeUnactioned();
                }
            });

            return Ok();
        }

        [HttpPost]
        public IActionResult Execute(Guid id)
        {
            _logger.Log($"Executing task: {id}.");

            if (!_runetimeState.TryGetValue(id, out var state))
                return NotFound();

            if (!state.MakeActionedOrFalse())
                return StatusCode((int)HttpStatusCode.Conflict);

            if (state.Status != ExecutionStatus.Compiled)
                return StatusCode((int)HttpStatusCode.Conflict);

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

                    if (!_expressionsCache.TryGetValue(state.HashedQuery, out CompiledQuery vm))
                        throw new NotSupportedException();
                    
                    state.Result = vm.Run();
                    var executionTime = watch.Elapsed;

                    watch.Stop();
                    
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

                    state.MakeUnactioned();
                    _logger.Log("Query processed.");
                }
            });

            return Ok();
        }

        [HttpGet]
        public ResultTable Result(Guid id)
        {
            _logger.Log($"Returning result for: {id}.");

            if (!_runetimeState.TryGetValue(id, out var state))
                throw new NotSupportedException();

            if(!state.MakeActionedOrFalse())
                throw new NotSupportedException();

            lock (state)
            {

                var table = state.Result;
                var computationTime = state.ExecutionTime;
                var columns = table.Columns.Select(f => f.Name).ToArray();
                var rows = table.Select(f => f.Values).ToArray();

                try
                {
                    state.MakeActioned();

                    state.Result = null;
                    state.Status = ExecutionStatus.Compiled;
                }
                finally
                {
                    state.MakeUnactioned();
                }

                return new ResultTable(table.Name, columns, rows, computationTime);
            }
        }

        [HttpGet]
        public (bool HasContext, ExecutionStatus Status) Status(Guid id)
        {
            _logger.Log($"Checking status for: {id}.");

            if (!_runetimeState.TryGetValue(id, out var state))
                return (false, ExecutionStatus.WaitingToStart);

            return (true, state.Status);
        }
    }
}