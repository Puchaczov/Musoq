using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using Musoq.Converter;
using Musoq.Service.Client;
using Musoq.Service.Models;

namespace Musoq.Service.Controllers
{
    public class RuntimeController : ApiController
    {
        private readonly IDictionary<Guid, QueryContext> _contexts;
        private readonly IDictionary<Guid, ExecutionState> _runetimeState;

        public RuntimeController(IDictionary<Guid, QueryContext> contexts,
            IDictionary<Guid, ExecutionState> runetimeState)
        {
            _contexts = contexts;
            _runetimeState = runetimeState;
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
                    var vm = InstanceCreator.Create(query, new DynamicSchemaProvider());
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