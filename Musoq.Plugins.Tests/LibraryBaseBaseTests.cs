using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Tests.Common;

namespace Musoq.Plugins.Tests
{
    [TestClass]
    public class LibraryBaseBaseTests
    {
        private class EmptyLibrary : LibraryBase { }

        protected LibraryBase Library;
        protected Group Group;
        protected Group Root;

        [TestInitialize]
        public void Initialize()
        {
            Library = new EmptyLibrary();
            Root = new Group(null, new string[0], new object[0]);
            Group = new Group(Root, new string[0], new object[0]);

            Culture.ApplyWithDefaultCulture();
        }
    }
}
