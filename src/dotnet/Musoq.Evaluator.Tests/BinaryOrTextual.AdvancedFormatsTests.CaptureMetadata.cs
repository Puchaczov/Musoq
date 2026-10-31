using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualAdvancedFormatsTests
{
    [TestMethod]
    [DataRow("prefix42", "42")]
    [DataRow("prefix", "")]
    public void OptionalPatternCapture_ShouldRetainStringMetadata(string input, string expected)
    {
        const string query = "text OptionalCapture { Part: pattern 'prefix(?<Number>\\d+)?' capture (Number) }; select p.Part.Number as Number from #test.Entities() source cross apply Parse<OptionalCapture>(source.Text) p";
        var sources = new Dictionary<string, IEnumerable<TextEntity>>(StringComparer.OrdinalIgnoreCase)
        {
            ["#test"] = [new TextEntity { Name = "optional", Text = input }]
        };
        using var compiled = CompileGeneratedQuery(query, Guid.NewGuid().ToString(), new TextSchemaProvider(sources), LoggerResolver, TestCompilationOptions);
        using var result = compiled.Run();
        TableMaterializationTestHelper.AssertColumns(result, ("Number", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, [expected]);
    }

    [TestMethod]
    [TestProperty("CandidateId", "P2-031")]
    public void NamedPatternCaptures_ShouldAdvertiseStringMetadata()
    {
        const string query = "text Coordinates { Pair: pattern '(?<Lat>-?\\d+\\.\\d+),(?<Lon>-?\\d+\\.\\d+)' capture (Lat, Lon) }; select p.Pair.Lat as Lat, p.Pair.Lon as Lon from #test.Entities() source cross apply Parse<Coordinates>(source.Text) p";
        var entities = new[]
        {
            new TextEntity { Name = "coordinates", Text = "-52.2297,21.0122" }
        };
        var sources = new Dictionary<string, IEnumerable<TextEntity>>(StringComparer.OrdinalIgnoreCase)
        {
            ["test"] = entities,
            ["#test"] = entities
        };
        using var compiled = CompileGeneratedQuery(query, Guid.NewGuid().ToString(), new TextSchemaProvider(sources), LoggerResolver, TestCompilationOptions);
        using var result = compiled.Run();
        TableMaterializationTestHelper.AssertRowsUnordered(result,
            new object?[] { "-52.2297", "21.0122" });
        TableMaterializationTestHelper.AssertColumns(result,
            ("Lat", typeof(string)),
            ("Lon", typeof(string)));
    }
}
