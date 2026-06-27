using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Tests for edge cases in type conversion to increase coverage of:
///     - NumericOnlyTypeConverter (currently 45%)
///     - ComparisonTypeConverter (currently 65.3%)
/// </summary>
[TestClass]
public partial class TypeConversionEdgeCasesTests : PluginsTestBase;
