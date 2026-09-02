using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Owns the MSTest framework-injected context for evaluator tests.
///     MSTest assigns this property after constructing each test instance.
/// </summary>
public abstract class MSTestContextTestBase
{
    public TestContext TestContext { get; set; } = null!;
}
