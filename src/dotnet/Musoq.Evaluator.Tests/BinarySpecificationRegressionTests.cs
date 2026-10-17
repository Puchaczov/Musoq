using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Tests;

[TestClass]
public partial class BinarySpecificationRegressionTests : BinaryOrTextualEvaluatorTestBase
{
    private Table RunBinary(string schema, string projection, params byte[][] records) =>
        RunBinaryQuery($"{schema} select {projection} from #test.files() f cross apply Interpret<Rec>(f.Content) r", records);

    private Table RunBinaryQuery(string sql, params byte[][] records)
        => RunBinaryQuery(sql, new CompilationOptions(), records);

    private Table RunBinaryQuery(string sql, CompilationOptions options, params byte[][] records)
    {
        var entities = Array.ConvertAll(records, bytes => new BinaryEntity { Content = bytes });
        var provider = new BinarySchemaProvider(new Dictionary<string, IEnumerable<BinaryEntity>> { ["#test"] = entities });
        using var query = InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString(), provider, LoggerResolver, options);
        return TableMaterializationTestHelper.Materialize(query.Run());
    }
}
