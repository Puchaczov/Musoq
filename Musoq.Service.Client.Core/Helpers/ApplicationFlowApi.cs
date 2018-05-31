using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Musoq.Service.Client.Core.Helpers
{
    public class ApplicationFlowApi
    {
        private readonly ContextApi _contextApi;
        private readonly RuntimeApi _runtimeApi;

        public ApplicationFlowApi(string address)
        {
            if (!address.EndsWith("/"))
                address = $"{address}/";

            address = $"http://{address}";
            _runtimeApi = new RuntimeApi(address);
            _contextApi = new ContextApi(address);
        }

        public async Task<ResultTable> WaitForQuery(QueryContext context)
        {
            var status = await _runtimeApi.Compile(context);

            if(status != System.Net.HttpStatusCode.OK)
                return new ResultTable(string.Empty, new string[0], new object[0][], TimeSpan.Zero);
            
            var currentState = await _runtimeApi.Status(context.QueryId);

            while (currentState.HasContext && (currentState.Status != ExecutionStatus.Compiled && currentState.Status != ExecutionStatus.Failure))
            {
                currentState = await _runtimeApi.Status(context.QueryId);
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }

            status = await _runtimeApi.Execute(context.QueryId);

            if (status != System.Net.HttpStatusCode.OK)
                return new ResultTable(string.Empty, new string[0], new object[0][], TimeSpan.Zero);

            currentState = await _runtimeApi.Status(context.QueryId);

            while (currentState.HasContext && (currentState.Status != ExecutionStatus.Success && currentState.Status != ExecutionStatus.Failure))
            {
                currentState = await _runtimeApi.Status(context.QueryId);
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }

            if (currentState.Status == ExecutionStatus.Success)
                return await _runtimeApi.Result(context.QueryId);

            return new ResultTable(string.Empty, new string[0], new object[0][], TimeSpan.Zero);
        }

        public async Task<ResultTable> RunQueryAsync(QueryContext context)
        {
            try
            {
                return await WaitForQuery(context);
            }
            catch (Exception e)
            {
                if (Debugger.IsAttached)
                    Debug.Write(e);
            }

            return new ResultTable(string.Empty, new string[0], new object[0][], TimeSpan.Zero);
        }

        public async Task<IReadOnlyList<T>> RunQueryAsync<T>(QueryContext context)
            where T : new()
        {
            return MapHelper.MapToType<T>(await RunQueryAsync(context));
        }
    }
}