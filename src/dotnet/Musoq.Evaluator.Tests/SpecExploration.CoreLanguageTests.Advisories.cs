using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class SpecExplorationCoreLanguageTests
{
    [TestMethod]
    public void Spec_AdvisoryExamples_ShouldCompileWithDocumentedCodes()
    {
        var cases = new (string Query, DiagnosticCode Code)[]
        {
            ("select case when false then 'dead' else 'live' end from #A.Entities()", DiagnosticCode.MQ5008_UnreachableCode),
            ("select Name from #A.Entities() where 1 = 1", DiagnosticCode.MQ5010_TautologicalCondition),
            ("select Name from #A.Entities() where 1 = 2", DiagnosticCode.MQ5011_ContradictoryCondition),
            (@"select 'C:\new\test' from #A.Entities()", DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape),
            (@"select Name from #A.Entities() where Name rlike '\bword\b'", DiagnosticCode.MQ5015_SuspiciousRegexEscape),
            ("select Name from #A.Entities() where Name like '*.log'", DiagnosticCode.MQ5016_GlobWildcardInLike),
            ("select Name from #A.Entities() where Name = NULL", DiagnosticCode.MQ5017_NullComparison),
            ("select a.Name, b.Name from #A.Entities() a left join #B.Entities() b on a.Id = b.Id where b.Name is null", DiagnosticCode.MQ5018_AmbiguousOuterJoinNullCheck),
            ("select a.Name, b.Name from #A.Entities() a left join #B.Entities() b on a.Id = b.Id where b.Name = 'match'", DiagnosticCode.MQ5019_NullRejectingOuterJoinFilter),
            ("select Name from #A.Entities() skip 10", DiagnosticCode.MQ5021_UnorderedSkip),
            ("with dead as (select Name from #A.Entities()) select Name from #A.Entities()", DiagnosticCode.MQ5022_UnusedCte),
            ("let dead: int = 1; select Name from #A.Entities()", DiagnosticCode.MQ5023_UnusedScriptVariable),
            ("select Name from #A.Entities() where Name not in ('Alice', NULL)", DiagnosticCode.MQ5024_NullSensitiveNotIn),
            ("select 1 from #A.Entities() where Time = '02/31/2026'", DiagnosticCode.MQ5025_ImpossibleImplicitConversion),
            ("select 1 from #A.Entities() where Time = '01/02/2026'", DiagnosticCode.MQ5003_ImplicitTypeConversion)
        };

        foreach (var (query, code) in cases)
        {
            var build = InstanceCreator.CompileWithDiagnostics(
                query,
                $"SpecAdvisory_{code}_{Guid.NewGuid():N}",
                CreateProvider(),
                LoggerResolver,
                TestCompilationOptions);

            Assert.IsTrue(build.Succeeded, $"{code}: {Format(build.Diagnostics)}");
            Assert.IsEmpty(build.Errors, $"{code}: {Format(build.Diagnostics)}");
            var warnings = build.Warnings.Where(item => item.Code == code).ToArray();
            Assert.HasCount(1, warnings, $"{code}: {Format(build.Diagnostics)}");
            var warning = warnings[0];
            Assert.AreEqual(
                code == DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape
                    ? DiagnosticPhase.Parse
                    : DiagnosticPhase.Bind,
                warning.Phase,
                code.ToString());
            var compiledQuery = build.CompiledQuery ??
                throw new AssertFailedException("Successful advisory compilation produced no compiled query.");
            compiledQuery.Dispose();
        }
    }

    [TestMethod]
    public void Spec_AdvisoryStringExamples_ShouldExecuteWithoutChangingExistingValues()
    {
        var cases = new (string Query, string Expected, bool HasWarning)[]
        {
            (@"select 'C:\new\test' from #A.Entities()", "C:\n" + "ew\t" + "est", true),
            (@"select r'C:\new\test' from #A.Entities()", @"C:\new\test", false),
            (@"select 'C:\\new\\test' from #A.Entities()", @"C:\new\test", false),
            (@"select '\n' from #A.Entities()", "\n", false),
            (@"select r'\n' from #A.Entities()", @"\n", false)
        };

        foreach (var (query, expected, hasWarning) in cases)
        {
            var build = InstanceCreator.CompileWithDiagnostics(
                query,
                $"SpecAdvisoryRuntime_{Guid.NewGuid():N}",
                CreateProvider(),
                LoggerResolver,
                TestCompilationOptions);

            Assert.IsTrue(build.Succeeded, Format(build.Diagnostics));
            Assert.AreEqual(hasWarning, build.Warnings.Count > 0, query);
            var compiledQuery = build.CompiledQuery ??
                throw new AssertFailedException("Successful advisory compilation produced no compiled query.");
            using var table = compiledQuery.Run(TokenSource.Token);
            Assert.AreEqual(expected, table[0][0], query);
        }
    }

    private static BasicSchemaProvider<BasicEntity> CreateProvider()
    {
        return new BasicSchemaProvider<BasicEntity>(
            new Dictionary<string, IEnumerable<BasicEntity>>
            {
                ["#A"] = [new BasicEntity("word")],
                ["#B"] = []
            });
    }

    private static string Format(IEnumerable<Diagnostic> diagnostics)
    {
        return string.Join(" | ", diagnostics);
    }
}
