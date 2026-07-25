using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class KeywordCollisionSchemaTests : SchemaParserTestsBase
{
    [TestMethod]
    public void SchemaKeywordCatalog_ShouldRemainUsableAsSchemaFieldNames()
    {
        foreach (var (text, _) in KeywordCollisionCatalog.SchemaKeywords)
        {
            try
            {
                var schema = ParseBinarySchema($"binary T {{ {text}: byte }}");
                Assert.HasCount(1, schema.Fields);
                var field = (FieldDefinitionNode)schema.Fields[0];
                Assert.AreEqual(text, field.Name, $"Schema field name '{text}' was not preserved.");
            }
            catch (SyntaxException exception)
            {
                Assert.Fail($"Schema keyword field '{text}' failed: {exception.Message}");
            }
        }
    }
}
