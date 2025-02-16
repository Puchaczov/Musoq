using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Tests.Common;

namespace Musoq.Plugins.Tests;

[TestClass]
public class LibraryBaseBaseTests
{
    private class EmptyLibrary : LibraryBase { }
    private Group _root;

    protected LibraryBase Library;
    protected Group Group;

    [TestInitialize]
    public void Initialize()
    {
        Library = CreateLibrary();
        _root = new Group(null, [], []);
        Group = new Group(_root, [], []);

        Culture.ApplyWithDefaultCulture();
    }
        
    protected virtual LibraryBase CreateLibrary()
    {
        return new EmptyLibrary();
    }
}