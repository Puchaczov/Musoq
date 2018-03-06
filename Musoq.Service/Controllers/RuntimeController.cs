using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web.Http;
using CacheManager.Core;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Plugins.Helpers;
using Musoq.Service.Client;
using Musoq.Service.Models;
using Musoq.Service.Visitors;

namespace Musoq.Service.Controllers
{
    public class RuntimeController : ApiController
    {
        private readonly IDictionary<Guid, QueryContext> _contexts;
        private readonly IDictionary<Guid, ExecutionState> _runetimeState;
        private readonly ICacheManager<VirtualMachine> _expressionsCache;

        public RuntimeController(IDictionary<Guid, QueryContext> contexts,
            IDictionary<Guid, ExecutionState> runetimeState,
            CacheManager.Core.ICacheManager<Evaluator.VirtualMachine> expressionsCache)
        {
            _contexts = contexts;
            _runetimeState = runetimeState;
            _expressionsCache = expressionsCache;
        }

        [HttpPost]
        public Guid Execute([FromUri] Guid id)
        {
            Console.WriteLine($"Executing task: {id}.");

            if (!_contexts.TryGetValue(id, out var context))
                return Guid.Empty;

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

                    if(!_expressionsCache.TryGetOrAdd(hash, (s) => InstanceCreator.Create(root, new DynamicSchemaProvider()), out var vm))
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
                }
            });

            return taskId;
        }

        [HttpGet]
        public ResultTable Result([FromUri] Guid id)
        {
            Console.WriteLine($"Returning result for: {id}.");
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
            Console.WriteLine($"Checking status for: {id}.");
            if (!_runetimeState.ContainsKey(id))
                return (false, ExecutionStatus.WaitingToStart);

            return (true, _runetimeState[id].Status);
        }
    }
}