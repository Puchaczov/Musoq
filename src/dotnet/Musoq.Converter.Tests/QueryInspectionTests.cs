using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;

namespace Musoq.Converter.Tests;

[TestClass]
public partial class QueryInspectionTests
{
    private readonly TestsLoggerResolver _loggerResolver = new();
    private readonly SystemSchemaProvider _schemaProvider = new();
}
