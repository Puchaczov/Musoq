using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Tests.Common;

namespace Musoq.Plugins.Tests;

[TestClass]
public class PluginsTestBase
{
    protected LibraryBase Library = null!;

    [TestInitialize]
    public void Initialize()
    {
        Library = CreateLibrary();

        Culture.ApplyWithDefaultCulture();
    }

    protected virtual LibraryBase CreateLibrary()
    {
        return new EmptyLibrary();
    }

    private sealed class EmptyLibrary : LibraryBase;
}
