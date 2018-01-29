using System;
using System.Threading.Tasks;

namespace FQL.Service.Client.Helpers
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

        public async Task<ResultTable> RunQueryAsync(QueryContext context)
        {
            try
            {
                var registeredId = await _contextApi.Create(context);

                if (registeredId == Guid.Empty)
                    return new ResultTable(string.Empty, new string[0], new object[0][], TimeSpan.Zero);

                var resultId = await _runtimeApi.Execute(registeredId);

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
            catch (Exception exc)
            {

            }

            return new ResultTable(string.Empty, new String[0], new object[0][], TimeSpan.Zero);
        }
    }
}