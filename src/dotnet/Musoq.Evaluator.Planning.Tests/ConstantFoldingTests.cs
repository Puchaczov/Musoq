using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Tests for constant folding optimization.
///     Verifies arithmetic, string, boolean, bitwise, and null folding,
///     division-by-zero detection, arithmetic overflow detection,
///     tautological/contradictory condition warnings, and passthrough for non-constant operands.
/// </summary>
[TestClass]
public partial class ConstantFoldingTests : GenericEntityTestBase
{
    private static readonly FoldEntity[] SingleEntitySource =
        [new() { Name = "a", Value = 1 }];











    #region Test entity

    public class FoldEntity
    {
        public string Name { get; set; } = string.Empty;

        public int Value { get; set; }
    }

    #endregion





    #region Diagnostic helper

    private BuildResult CompileWithDiagnostics<TEntity>(string script, TEntity[] source)
    {
        var schema = new GenericSchema<GenericLibrary>(
            new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
            {
                {
                    "first",
                    (new GenericEntityTable<TEntity>(),
                        new GenericChunkSource<TEntity>(
                            source,
                            GenericEntityTable<TEntity>.NameToIndexMap,
                            GenericEntityTable<TEntity>.IndexToObjectAccessMap))
                }
            });

        var schemaProvider = new GenericSchemaProvider(new Dictionary<string, ISchema>
        {
            { "#schema", schema }
        });

        return InstanceCreator.CompileWithDiagnostics(
            script,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);
    }

    #endregion
}
