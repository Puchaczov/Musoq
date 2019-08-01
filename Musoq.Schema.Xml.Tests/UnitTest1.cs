using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Plugins;
using System.Linq;
using System.Threading;

namespace Musoq.Schema.Xml.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var source = new XmlSource("./TestFiles/Test1.xml", new RuntimeContext(CancellationToken.None, null, null));

            var rows = source.Rows.ToArray();

            Assert.AreEqual(22, rows.Length);
        }

        [TestMethod]
        public void TestMethod2()
        {
            var query = @"select element, parent.element from #xml.file('./TestFiles/Test1.xml') where element = 'food' or element = 'price'";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            foreach(var row in table)
            {
                Assert.IsTrue(new string[] { "food", "price" }.Contains((string)row[0]));

                if (((string)row[0]) == "food")
                {
                    Assert.AreEqual("breakfast_menu", (string)row[1]);
                }
                else if(((string)row[0]) == "price")
                {
                    Assert.AreEqual("food", (string)row[1]);
                }
                else
                {
                    throw new System.Exception($"Can be only breakfast_menu or food but found: {(string)row[0]}");
                }
            }
        }

        [TestMethod]
        public void TestMethod3()
        {
            var query = @"select element, special from #xml.file('./TestFiles/Test1.xml') where special is not null";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(3, table.Count);


            Assert.AreEqual("name", table[0][0]);
            Assert.AreEqual("1", table[0][1]);

            Assert.AreEqual("calories", table[1][0]);
            Assert.AreEqual("2", table[1][1]);

            Assert.AreEqual("food", table[2][0]);
            Assert.AreEqual("4", table[2][1]);
        }

        [TestMethod]
        public void TestMethod4()
        {
            var query = @"select special, Count(special) from #xml.file('./TestFiles/Test1.xml') where special is not null group by special";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(3, table.Count);

            Assert.AreEqual("1", table[0][0]);
            Assert.AreEqual(1, table[0][1]);

            Assert.AreEqual("2", table[1][0]);
            Assert.AreEqual(1, table[1][1]);

            Assert.AreEqual("4", table[2][0]);
            Assert.AreEqual(1, table[2][1]);
        }

        [TestMethod]
        public void TestMethod5()
        {
            var query = @"select element, text from #xml.file('./TestFiles/Test1.xml') where special is not null";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(3, table.Count);


            Assert.AreEqual("name", table[0][0]);
            Assert.AreEqual("Strawberry Belgian Waffles", table[0][1]);

            Assert.AreEqual("calories", table[1][0]);
            Assert.AreEqual("900", table[1][1]);

            Assert.AreEqual("food", table[2][0]);
            Assert.AreEqual(null, table[2][1]);
        }


        [TestMethod]
        public void TestMethod6()
        {
            var query = @"select image, Count(image) from #xml.file('./TestFiles/Test2.xml') where element = 'color_swatch' group by image";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(4, table.Count);

            Assert.AreEqual("red_cardigan.jpg", table[0][0]);
            Assert.AreEqual(4, table[0][1]);

            Assert.AreEqual("burgundy_cardigan.jpg", table[1][0]);
            Assert.AreEqual(5, table[1][1]);

            Assert.AreEqual("navy_cardigan.jpg", table[2][0]);
            Assert.AreEqual(3, table[2][1]);

            Assert.AreEqual("black_cardigan.jpg", table[3][0]);
            Assert.AreEqual(3, table[3][1]);
        }


        [TestMethod]
        public void TestMethod7()
        {
            var query = @"select image, Count(image) from #xml.file('./TestFiles/Test2.xml') where element = 'color_swatch' group by image having Count(image) > 3";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            Assert.AreEqual(2, table.Count);

            Assert.AreEqual("red_cardigan.jpg", table[0][0]);
            Assert.AreEqual(4, table[0][1]);

            Assert.AreEqual("burgundy_cardigan.jpg", table[1][0]);
            Assert.AreEqual(5, table[1][1]);
        }


        [TestMethod]
        public void TestMethod8()
        {
            var query = @"select ToDecimal(text) from #xml.file('./TestFiles/Test2.xml') where price is not null";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();

            
            Assert.AreEqual(2, table.Count);
            Assert.AreEqual(39.95m, table[0][0]);
            Assert.AreEqual(42.50m, table[1][0]);
        }

        [TestMethod]
        public void TestMethod9()
        {
            var query = @"select ToDecimal(text) from #xml.file('./TestFiles/Test2.xml', 'child') where price is not null and ToDecimal(text) > 40";

            var vm = CreateAndRunVirtualMachine(query);
            var table = vm.Run();


            Assert.AreEqual(1, table.Count);
            Assert.AreEqual(42.50m, table[0][0]);
        }

        private CompiledQuery CreateAndRunVirtualMachine(string script)
        {
            return InstanceCreator.CompileForExecution(script, new XmlProvider());
        }

        static UnitTest1()
        {
            new Environment().SetValue(Constants.NetStandardDllEnvironmentName, EnvironmentUtils.GetOrCreateEnvironmentVariable());
        }
    }
}
