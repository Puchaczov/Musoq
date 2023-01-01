using System;
using System.Collections.Generic;
using Moq;
using Musoq.Converter;
using Musoq.Plugins;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests.Schema.EnvironmentVariable
{
    public class EnvironmentVariablesTestBase
    {
        protected CompiledQuery CreateAndRunVirtualMachine(
            string script,
            IDictionary<string, IEnumerable<EnvironmentVariableEntity>> sources,
            IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> positionalEnvironmentVariables = null)
        {
            return InstanceCreator.CompileForExecution(
                script, 
                Guid.NewGuid().ToString(), 
                new EnvironmentVariablesSchemaProvider(sources),
                positionalEnvironmentVariables ?? CreateMockedEnvironmentVariables());
        }

        private IReadOnlyDictionary<uint,IReadOnlyDictionary<string,string>> CreateMockedEnvironmentVariables()
        {
            var environmentVariablesMock = new Mock<IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>>>();
            environmentVariablesMock.Setup(f => f[It.IsAny<uint>()]).Returns(new Dictionary<string, string>());

            return environmentVariablesMock.Object;
        }

        static EnvironmentVariablesTestBase()
        {
            new Plugins.Environment()
                .SetValue(
                    Constants.NetStandardDllEnvironmentName, 
                    EnvironmentUtils.GetOrCreateEnvironmentVariable());

            Culture.ApplyWithDefaultCulture();
        }
    }
}