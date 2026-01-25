using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Tests.Common;

namespace Musoq.Plugins.Tests;

[TestClass]
public class LibraryBaseBaseTests
{
    private Group _root = null!;
    protected Group Group = null!;

    protected LibraryBase Library = null!;

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

    private class EmptyLibrary : LibraryBase
    {
    }
}
