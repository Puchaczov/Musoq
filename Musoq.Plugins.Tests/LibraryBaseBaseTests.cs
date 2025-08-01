using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Tests.Common;

namespace Musoq.Plugins.Tests;

[TestClass]
public class LibraryBaseBaseTests
{
    private class EmptyLibrary : LibraryBase { }
    private Group _root = null!;

    protected LibraryBase Library = null!;
    protected Group Group = null!;

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