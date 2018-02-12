using System;
using System.Threading.Tasks;

namespace Musoq.Service.Client.Helpers
{
    public class ApplicationFlowApi
    {
        private readonly ContextApi _contextApi;
        private readonly RuntimeApi _runtimeApi;
        private readonly SelfApi _selfApi;

        public ApplicationFlowApi(string address)
        {
            if (!address.EndsWith("/"))
                address = $"{address}/";

            address = $"http://{address}";
            _runtimeApi = new RuntimeApi(address);
            _contextApi = new ContextApi(address);
            _selfApi = new SelfApi(address);
        }

        public async Task<ResultTable> WaitForQuery(Guid taskId)
        {
            var resultId = await _runtimeApi.Execute(taskId);

            if (resultId == Guid.Empty)
                return new ResultTable(string.Empty, new string[0], new object[0][], TimeSpan.Zero);

            var currentState = await _runtimeApi.Status(resultId);

            while (currentState.HasContext && (currentState.Status == ExecutionStatus.Running ||
                                               currentState.Status == ExecutionStatus.WaitingToStart))
            {
                currentState = await _runtimeApi.Status(resultId);
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }

            if (currentState.Status == ExecutionStatus.Success)
                return await _runtimeApi.Result(resultId);

            return new ResultTable(string.Empty, new string[0], new object[0][], TimeSpan.Zero);
        }

        public async Task<ResultTable> RunQueryAsync(QueryContext context)
        {
            try
            {
                var registeredId = await _contextApi.Create(context);

                if (registeredId == Guid.Empty)
                    return new ResultTable(string.Empty, new string[0], new object[0][], TimeSpan.Zero);

                return WaitForQuery(registeredId).Result;
            }
            catch (Exception exc)
            {

            }

            return new ResultTable(string.Empty, new String[0], new object[0][], TimeSpan.Zero);
        }

        public async Task<ResultTable> GetSelfFiles()
        {
            return await WaitForQuery(await _selfApi.UsedFiles());
        }
    }
}