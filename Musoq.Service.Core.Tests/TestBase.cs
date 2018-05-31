using Microsoft.Extensions.Caching.Memory;
using Moq;
using Musoq.Service.Client.Core;
using Musoq.Service.Core.Controllers;
using Musoq.Service.Core.Logging;
using Musoq.Service.Core.Tests.Schema;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Musoq.Service.Core.Tests
{
    public class TestBase
    {
        protected (ContextController ContextController, RuntimeController RuntimeController) CreateWorkflowControllers()
        {
            var queryContextDict = new QueryContextDictionary();
            var executeStateDict = new ExecutionStateDictionary();
            var loadedSchemasDict = new LoadedSchemasDictionary();

            loadedSchemasDict.TryAdd("#test", typeof(TestSchema));

            var serviceLoggerMoq = new Mock<IServiceLogger>();

            serviceLoggerMoq
                .Setup(s => s.Log(It.IsAny<string>()));

            serviceLoggerMoq
                .Setup(s => s.Log(It.IsAny<Exception>()));

            var contextController = new ContextController(queryContextDict, serviceLoggerMoq.Object);
            var runtimeController = new RuntimeController(queryContextDict, executeStateDict, new MemoryCache(new MemoryCacheOptions()), serviceLoggerMoq.Object, loadedSchemasDict);

            return (contextController, runtimeController);
        }

        protected QueryContext CreateQueryContext()
        {
            return QueryContext.FromQueryText(Guid.NewGuid(), "select * from #test.mem()");
        }

        protected void WaitUntilStatusCompiled(RuntimeController controller, Guid id)
        {
            var status = controller.Status(id);
            while (status.Status != ExecutionStatus.Compiled && status.Status != ExecutionStatus.Failure)
            {
                Thread.Sleep(100);
                status = controller.Status(id);
            };
        }
        protected void WaitUntilStatusSuccess(RuntimeController controller, Guid id)
        {
            var status = controller.Status(id);
            while (status.Status != ExecutionStatus.Success && status.Status != ExecutionStatus.Failure)
            {
                Thread.Sleep(1000);
                status = controller.Status(id);
            };

            if (status.Status == ExecutionStatus.Failure)
                throw new Exception();
        }
    }
}
