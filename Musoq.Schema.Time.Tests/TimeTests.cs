using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator;

namespace Musoq.Schema.Time.Tests
{
    [TestClass]
    public class TimeTests
    {
        [TestMethod]
        public void EnumerateAllDaysInMonthTest()
        {
            var query = "select Day from #time.interval('01.04.2018 00:00:00', '30.04.2018 00:00:00', 'days')";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            for (var i = 1; i <= 30; i++) Assert.AreEqual(i, table[i - 1][0]);
        }

        private IRunnable CreateAndRunVirtualMachine(string script)
        {
            return InstanceCreator.Create(script, new TimeSchemaProvider());
        }
    }
}