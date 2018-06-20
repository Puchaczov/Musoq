using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net;
using System.Threading;

namespace Musoq.Service.Core.Tests
{
    [TestClass]
    public class WorkflowTests : TestBase
    {
        [TestMethod]
        public void RegisterCompileExecuteWorkflowTest()
        {
            var controllers = CreateWorkflowControllers();
            var query = CreateQueryContext();

            var result = controllers.RuntimeController.Compile(query);

            Assert.IsInstanceOfType(result, typeof(OkResult));

            WaitUntilStatusCompiled(controllers.RuntimeController, query.QueryId);

            result = controllers.RuntimeController.Execute(query.QueryId);

            Assert.IsInstanceOfType(result, typeof(OkResult));

            WaitUntilStatusSuccess(controllers.RuntimeController, query.QueryId);

            Assert.IsNotNull(controllers.RuntimeController.Result(query.QueryId));

            //another execution of the same query.
            result = controllers.RuntimeController.Execute(query.QueryId);

            Assert.IsInstanceOfType(result, typeof(OkResult));

            WaitUntilStatusSuccess(controllers.RuntimeController, query.QueryId);

            Assert.IsNotNull(controllers.RuntimeController.Result(query.QueryId));
        }

        [TestMethod]
        public void RegisterCompileEditCompileExecuteWorkflowTest()
        {
            var controllers = CreateWorkflowControllers();
            var query = CreateQueryContext();
            var result = controllers.RuntimeController.Compile(query);

            Assert.IsInstanceOfType(result, typeof(OkResult));

            WaitUntilStatusCompiled(controllers.RuntimeController, query.QueryId);

            query.Query = $"select * from #test.mem() where 1 = 1";

            result = controllers.RuntimeController.Compile(query);

            Assert.IsInstanceOfType(result, typeof(OkResult));

            WaitUntilStatusCompiled(controllers.RuntimeController, query.QueryId);

            result = controllers.RuntimeController.Execute(query.QueryId);

            WaitUntilStatusSuccess(controllers.RuntimeController, query.QueryId);

            Assert.IsNotNull(controllers.RuntimeController.Result(query.QueryId));
        }

        [TestMethod]
        public void RegisterCompileSimultaneously()
        {
            var controllers = CreateWorkflowControllers();
            var query = CreateQueryContext();

            var result = controllers.RuntimeController.Compile(query);

            Assert.IsInstanceOfType(result, typeof(OkResult));

            result = controllers.RuntimeController.Compile(query);

            Assert.IsInstanceOfType(result, typeof(StatusCodeResult));
            Assert.AreEqual((int)HttpStatusCode.NotModified, ((StatusCodeResult)result).StatusCode);
        }

        [TestMethod]
        public void RegisterCompileExecuteSimultaneously()
        {
            var controllers = CreateWorkflowControllers();
            var query = CreateQueryContext();

            var result = controllers.RuntimeController.Compile(query);

            Assert.IsInstanceOfType(result, typeof(OkResult));

            WaitUntilStatusCompiled(controllers.RuntimeController, query.QueryId);

            result = controllers.RuntimeController.Execute(query.QueryId);

            Assert.IsInstanceOfType(result, typeof(OkResult));

            result = controllers.RuntimeController.Execute(query.QueryId);

            Assert.IsInstanceOfType(result, typeof(StatusCodeResult));
            Assert.AreEqual((int)HttpStatusCode.Conflict, ((StatusCodeResult)result).StatusCode);

            WaitUntilStatusSuccess(controllers.RuntimeController, query.QueryId);

            Assert.IsNotNull(controllers.RuntimeController.Result(query.QueryId));
        }
    }
}
