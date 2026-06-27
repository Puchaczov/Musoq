using Musoq.Plugins.Lib.RuntimeOperators;
using Musoq.Plugins.Lib.TypeConversion;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    #region Dependency Injection - SOLID Principle: Dependency Inversion

    /// <summary>
    ///     Type converters and runtime operators following SOLID principles.
    ///     These are initialized as static singletons but can be replaced for testing.
    /// </summary>
    private static readonly StrictTypeConverter StrictConverter = new();

    private static readonly ComparisonTypeConverter ComparisonConverter = new();
    private static readonly NumericOnlyTypeConverter NumericOnlyConverter = new();

    private static readonly TypePreservingRuntimeOperators RuntimeOperators = new(
        NumericOnlyConverter,
        ComparisonConverter,
        StrictConverter);

    #endregion
}
