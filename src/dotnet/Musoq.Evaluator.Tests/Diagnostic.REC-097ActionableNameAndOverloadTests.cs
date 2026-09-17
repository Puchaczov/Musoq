using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Evaluator.Tests.Schema.Multi;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Exceptions;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;
using SchemaConstructorInfo = Musoq.Schema.Reflection.ConstructorInfo;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Recovery campaign REC-097: verify the complete diagnostic path for
///     symbol roles, callable names, overload facts, and safe repairs.
/// </summary>
[TestClass]
public sealed class DiagnosticREC097ActionableNameAndOverloadTests
{
    private static readonly CompilationOptions CompilationOptions =
        new(usePrimitiveTypeValidation: false);

    [TestMethod]
    [DynamicData(nameof(RecoveryCases))]
    public void ActionableDiagnostics_ShouldClassifyFactsAndValidateRepairs(object candidateData)
    {
        var candidate = (RecoveryCase)candidateData;
        var result = Analyze(candidate);
        Assert.IsFalse(result.IsSuccess, $"{candidate.CaseId}: expected a diagnostic.");

        var diagnostics = result.Errors.ToArray();
        Assert.HasCount(1, diagnostics,
            $"{candidate.CaseId}: expected one root diagnostic, got " +
            string.Join(" | ", diagnostics.Select(static diagnostic =>
                $"[{diagnostic.Code}] {diagnostic.Message}")));

        var diagnostic = diagnostics[0];
        Assert.AreEqual(candidate.ExpectedCode, diagnostic.Code, candidate.CaseId);
        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase, candidate.CaseId);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind, candidate.CaseId);
        Assert.IsTrue(diagnostic.Location.IsValid, candidate.CaseId);
        Assert.IsTrue(diagnostic.EndLocation.IsValid, candidate.CaseId);
        Assert.IsTrue(diagnostic.Message.Contains(candidate.MessageFragment, StringComparison.OrdinalIgnoreCase),
            $"{candidate.CaseId}: expected '{candidate.MessageFragment}' in '{diagnostic.Message}'.");

        foreach (var key in candidate.RequiredArgumentKeys)
            Assert.IsTrue(diagnostic.Arguments.ContainsKey(key),
                $"{candidate.CaseId}: missing structured fact '{key}'.");

        foreach (var fact in candidate.ExpectedArgumentValues)
        {
            Assert.IsTrue(diagnostic.Arguments.TryGetValue(fact.Key, out var actual),
                $"{candidate.CaseId}: missing structured fact '{fact.Key}'.");
            Assert.AreEqual(fact.Value, actual, candidate.CaseId);
        }

        var textEdits = diagnostic.SuggestedFixes
            .Where(static action => action.TextEdit != null)
            .Select(static action => action.TextEdit!)
            .ToArray();

        if (candidate.NativeReplacement != null)
        {
            Assert.HasCount(1, textEdits, candidate.CaseId);
            var edit = textEdits[0];
            Assert.AreEqual(candidate.NativeReplacement, edit.NewText, candidate.CaseId);
            Assert.IsTrue(edit.Span.Start >= 0 && edit.Span.End <= candidate.Query.Length,
                $"{candidate.CaseId}: native edit is outside the query span.");

            var repairedQuery = ApplyEdit(candidate.Query, edit);
            var repaired = Analyze(candidate with { Query = repairedQuery });
            Assert.IsTrue(repaired.IsSuccess,
                $"{candidate.CaseId}: native repair left diagnostics: {FormatDiagnostics(repaired)}");
        }
        else
        {
            Assert.IsEmpty(textEdits,
                $"{candidate.CaseId}: a non-unique or non-spelling failure received an automatic edit.");
        }

        if (candidate.ManualRepairFrom != null)
        {
            var repairedQuery = ReplaceOnce(candidate.Query, candidate.ManualRepairFrom, candidate.ManualRepairTo!);
            var repaired = Analyze(candidate with { Query = repairedQuery });
            Assert.IsTrue(repaired.IsSuccess,
                $"{candidate.CaseId}: qualified/manual repair left diagnostics: {FormatDiagnostics(repaired)}");
        }
    }

    [TestMethod]
    public void CandidateMatrix_ShouldContainEightCasesPerResolutionFamily()
    {
        Assert.HasCount(48, CandidateCases);
        Assert.HasCount(48, CandidateCases.Select(static candidate => candidate.CaseId).Distinct(StringComparer.Ordinal));

        var familyCounts = CandidateCases
            .GroupBy(static candidate => candidate.Family)
            .ToDictionary(static group => group.Key, static group => group.Count(), StringComparer.Ordinal);

        CollectionAssert.AreEquivalent(
            new[] { "source-and-schema", "alias", "column", "property", "function", "overload" },
            familyCounts.Keys.ToArray());
        Assert.IsTrue(familyCounts.Values.All(static count => count == 8));
        Assert.IsTrue(CandidateCases.All(static candidate => !string.IsNullOrWhiteSpace(candidate.Query)));
    }

    [TestMethod]
    public void DiagnosticRoleContract_ShouldUseOneStableBindCodePerRegisteredRole()
    {
        var expected = new Dictionary<string, DiagnosticCode>(StringComparer.Ordinal)
        {
            ["source-and-schema"] = DiagnosticCode.MQ3085_UnknownSource,
            ["alias"] = DiagnosticCode.MQ3015_UnknownAlias,
            ["column"] = DiagnosticCode.MQ3001_UnknownColumn,
            ["property"] = DiagnosticCode.MQ3028_UnknownProperty,
            ["function"] = DiagnosticCode.MQ3086_UnknownCallable,
            ["overload"] = DiagnosticCode.MQ3087_InvalidCallableArity
        };

        Assert.AreEqual(DiagnosticCode.MQ3010_UnknownSchema,
            CandidateCases.Single(static candidate => candidate.CaseId == "REC-097-S05").ExpectedCode);
        Assert.AreEqual(DiagnosticCode.MQ3079_UnknownSourceArgument,
            CandidateCases.Single(static candidate => candidate.CaseId == "REC-097-S07").ExpectedCode);
        Assert.AreEqual(DiagnosticCode.MQ3035_AmbiguousMethodOwner,
            CandidateCases.Single(static candidate => candidate.CaseId == "REC-097-F07").ExpectedCode);

        foreach (var pair in expected)
        {
            var cases = CandidateCases.Where(candidate => candidate.Family == pair.Key).ToArray();
            Assert.IsTrue(cases.Any(candidate => candidate.ExpectedCode == pair.Value), pair.Key);
        }
    }

    [TestMethod]
    public void OverloadDiagnostics_ShouldExposeBoundedTypedFactsAndNoTextEdit()
    {
        var overloadCases = CandidateCases.Where(static candidate => candidate.Family == "overload").ToArray();
        Assert.HasCount(8, overloadCases);

        foreach (var candidate in overloadCases)
        {
            var result = Analyze(candidate);
            var diagnostic = result.Errors.Single();
            Assert.IsTrue(diagnostic.Arguments.ContainsKey("actualTypes"), candidate.CaseId);
            Assert.IsTrue(diagnostic.Arguments.ContainsKey("candidateSignatures"), candidate.CaseId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Arguments["candidateSignatures"]), candidate.CaseId);
            Assert.IsLessThanOrEqualTo(5,
                diagnostic.Arguments["candidateSignatures"].Split(';', StringSplitOptions.RemoveEmptyEntries).Length,
                candidate.CaseId);
            Assert.IsEmpty(diagnostic.SuggestedFixes.Where(static action => action.TextEdit != null), candidate.CaseId);
        }
    }

    [TestMethod]
    public void SuccessfulNativeRepairs_ShouldLeaveOnlyTheIntendedOwnerAndNoSecondaryError()
    {
        var repairable = CandidateCases.Where(static candidate => candidate.NativeReplacement != null).ToArray();
        Assert.HasCount(32, repairable);

        foreach (var candidate in repairable)
        {
            var result = Analyze(candidate);
            var diagnostic = result.Errors.Single();
            var edit = diagnostic.SuggestedFixes.Single(static action => action.TextEdit != null).TextEdit!;
            var repaired = Analyze(candidate with { Query = ApplyEdit(candidate.Query, edit) });
            Assert.IsTrue(repaired.IsSuccess,
                $"{candidate.CaseId}: repair produced secondary errors: {FormatDiagnostics(repaired)}");
        }
    }

    public static IEnumerable<object[]> RecoveryCases()
    {
        foreach (var candidate in CandidateCases)
            yield return [candidate];
    }

    private static readonly IReadOnlyList<RecoveryCase> CandidateCases =
    [
        // Source/schema ownership and named source-argument spelling.
        new("REC-097-S01", "source-and-schema", ProviderKind.Basic,
            "select Name from #A.Entites()", DiagnosticCode.MQ3085_UnknownSource, "Entites", null, null, null,
            ["schema", "source"], [new("source", "Entites")]),
        new("REC-097-S02", "source-and-schema", ProviderKind.Basic,
            "select Name from #A.Enities()", DiagnosticCode.MQ3085_UnknownSource, "Enities", null, null, null,
            ["schema", "source"], [new("source", "Enities")]),
        new("REC-097-S03", "source-and-schema", ProviderKind.Basic,
            "select Name from #A.Entity()", DiagnosticCode.MQ3085_UnknownSource, "Entity", null, null, null,
            ["schema", "source"], [new("source", "Entity")]),
        new("REC-097-S04", "source-and-schema", ProviderKind.Basic,
            "select Name from #B.Entites()", DiagnosticCode.MQ3085_UnknownSource, "Entites", null, null, null,
            ["schema", "source"], [new("source", "Entites")]),
        new("REC-097-S05", "source-and-schema", ProviderKind.Basic,
            "select Name from #missing.Entities()", DiagnosticCode.MQ3010_UnknownSchema, "missing", null, null, null,
            [], []),
        new("REC-097-S06", "source-and-schema", ProviderKind.Basic,
            "select Name from #absent.People()", DiagnosticCode.MQ3010_UnknownSchema, "absent", null, null, null,
            [], []),
        new("REC-097-S07", "source-and-schema", ProviderKind.NamedSource,
            "select Value from #rec097.items(sourcePah: 'data')", DiagnosticCode.MQ3079_UnknownSourceArgument,
            "sourcePah", "sourcePath", null, null,
            ["argument", "candidateParameters", "suggestion"], [new("suggestion", "sourcePath")]),
        new("REC-097-S08", "source-and-schema", ProviderKind.NamedSource,
            "select Value from #rec097.items(sourcePath: 'data', pageSiz: 2)", DiagnosticCode.MQ3079_UnknownSourceArgument,
            "pageSiz", "pageSize", null, null,
            ["argument", "candidateParameters", "suggestion"], [new("suggestion", "pageSize")]),

        // Alias ownership and spelling.
        new("REC-097-A01", "alias", ProviderKind.Basic,
            "select peple.Name from #A.Entities() people", DiagnosticCode.MQ3015_UnknownAlias, "peple", "people", null, null,
            ["alias", "availableAliases", "suggestion"], [new("suggestion", "people")]),
        new("REC-097-A02", "alias", ProviderKind.Basic,
            "select Name from #A.Entities() people where peple.City = 'Warsaw'", DiagnosticCode.MQ3015_UnknownAlias, "peple", "people", null, null,
            ["alias", "availableAliases", "suggestion"], [new("suggestion", "people")]),
        new("REC-097-A03", "alias", ProviderKind.Basic,
            "select Name from #A.Entities() people order by peple.Country", DiagnosticCode.MQ3015_UnknownAlias, "peple", "people", null, null,
            ["alias", "availableAliases", "suggestion"], [new("suggestion", "people")]),
        new("REC-097-A04", "alias", ProviderKind.Basic,
            "select peopel.Name from #A.Entities() people", DiagnosticCode.MQ3015_UnknownAlias, "peopel", "people", null, null,
            ["alias", "availableAliases", "suggestion"], [new("suggestion", "people")]),
        new("REC-097-A05", "alias", ProviderKind.Basic,
            "select pepl.Name from #A.Entities() people", DiagnosticCode.MQ3015_UnknownAlias, "pepl", "people", null, null,
            ["alias", "availableAliases", "suggestion"], [new("suggestion", "people")]),
        new("REC-097-A06", "alias", ProviderKind.Basic,
            "select Name from #A.Entities() people where peple.Name = 'Warsaw'", DiagnosticCode.MQ3015_UnknownAlias, "peple", "people", null, null,
            ["alias", "availableAliases", "suggestion"], [new("suggestion", "people")]),
        new("REC-097-A07", "alias", ProviderKind.Basic,
            "select peple.Name from #A.Entities() people\r\nwhere people.City = 'Warsaw'", DiagnosticCode.MQ3015_UnknownAlias, "peple", "people", null, null,
            ["alias", "availableAliases", "suggestion"], [new("suggestion", "people")]),
        new("REC-097-A08", "alias", ProviderKind.Basic,
            "select peple.Self.Name from #A.Entities() people", DiagnosticCode.MQ3015_UnknownAlias, "peple", "people", null, null,
            ["alias", "availableAliases", "suggestion"], [new("suggestion", "people")]),

        // Known source, unknown column spelling.
        new("REC-097-C01", "column", ProviderKind.Basic,
            "select Naem from #A.Entities()", DiagnosticCode.MQ3001_UnknownColumn, "Naem", "Name", null, null,
            ["column", "candidateColumns", "suggestion"], [new("suggestion", "Name")]),
        new("REC-097-C02", "column", ProviderKind.Basic,
            "select Name from #A.Entities() where Ctiy = 'Warsaw'", DiagnosticCode.MQ3001_UnknownColumn, "Ctiy", "City", null, null,
            ["column", "candidateColumns", "suggestion"], [new("suggestion", "City")]),
        new("REC-097-C03", "column", ProviderKind.Basic,
            "select Name from #A.Entities() order by Countr", DiagnosticCode.MQ3001_UnknownColumn, "Countr", "Country", null, null,
            ["column", "candidateColumns", "suggestion"], [new("suggestion", "Country")]),
        new("REC-097-C04", "column", ProviderKind.Basic,
            "select Populaton from #A.Entities()", DiagnosticCode.MQ3001_UnknownColumn, "Populaton", "Population", null, null,
            ["column", "candidateColumns", "suggestion"], [new("suggestion", "Population")]),
        new("REC-097-C05", "column", ProviderKind.Basic,
            "select name from #A.Entities()", DiagnosticCode.MQ3001_UnknownColumn, "name", "Name", null, null,
            ["column", "candidateColumns", "suggestion"], [new("suggestion", "Name")]),
        new("REC-097-C06", "column", ProviderKind.Basic,
            "select Nmae from #A.Entities()", DiagnosticCode.MQ3001_UnknownColumn, "Nmae", "Name", null, null,
            ["column", "candidateColumns", "suggestion"], [new("suggestion", "Name")]),
        new("REC-097-C07", "column", ProviderKind.Basic,
            "select Name from #A.Entities() where Naem = 'Warsaw' and City = 'Warsaw'", DiagnosticCode.MQ3001_UnknownColumn, "Naem", "Name", null, null,
            ["column", "candidateColumns", "suggestion"], [new("suggestion", "Name")]),
        new("REC-097-C08", "column", ProviderKind.Basic,
            "select Name, Ctiy from #A.Entities()", DiagnosticCode.MQ3001_UnknownColumn, "Ctiy", "City", null, null,
            ["column", "candidateColumns", "suggestion"], [new("suggestion", "City")]),

        // Known object, unknown property spelling.
        new("REC-097-P01", "property", ProviderKind.Basic,
            "select Self.Naem from #A.Entities()", DiagnosticCode.MQ3028_UnknownProperty, "Naem", "Name", null, null,
            ["property", "candidateProperties", "suggestion"], [new("suggestion", "Name")]),
        new("REC-097-P02", "property", ProviderKind.Basic,
            "select Name from #A.Entities() where Self.Ctiy = 'Warsaw'", DiagnosticCode.MQ3028_UnknownProperty, "Ctiy", "City", null, null,
            ["property", "candidateProperties", "suggestion"], [new("suggestion", "City")]),
        new("REC-097-P03", "property", ProviderKind.Basic,
            "select Name from #A.Entities() order by Self.Countr", DiagnosticCode.MQ3028_UnknownProperty, "Countr", "Country", null, null,
            ["property", "candidateProperties", "suggestion"], [new("suggestion", "Country")]),
        new("REC-097-P04", "property", ProviderKind.Basic,
            "select Self.Populaton from #A.Entities()", DiagnosticCode.MQ3028_UnknownProperty, "Populaton", "Population", null, null,
            ["property", "candidateProperties", "suggestion"], [new("suggestion", "Population")]),
        new("REC-097-P05", "property", ProviderKind.Basic,
            "select Self.Nmae from #A.Entities()", DiagnosticCode.MQ3028_UnknownProperty, "Nmae", "Name", null, null,
            ["property", "candidateProperties", "suggestion"], [new("suggestion", "Name")]),
        new("REC-097-P06", "property", ProviderKind.Basic,
            "select Name from #A.Entities() where Self.Naem = 'Warsaw'", DiagnosticCode.MQ3028_UnknownProperty, "Naem", "Name", null, null,
            ["property", "candidateProperties", "suggestion"], [new("suggestion", "Name")]),
        new("REC-097-P07", "property", ProviderKind.Basic,
            "select Self.Naem from #A.Entities() order by Name", DiagnosticCode.MQ3028_UnknownProperty, "Naem", "Name", null, null,
            ["property", "candidateProperties", "suggestion"], [new("suggestion", "Name")]),
        new("REC-097-P08", "property", ProviderKind.Basic,
            "select Self.Dictinary from #A.Entities()", DiagnosticCode.MQ3028_UnknownProperty, "Dictinary", "Dictionary", null, null,
            ["property", "candidateProperties", "suggestion"], [new("suggestion", "Dictionary")]),

        // Callable name and owner confusion.
        new("REC-097-F01", "function", ProviderKind.Basic,
            "select Substrng(Name, 0, 2) from #A.Entities()", DiagnosticCode.MQ3086_UnknownCallable, "Substrng", "Substring", null, null,
            ["callable", "candidateCallables", "suggestion"], [new("suggestion", "Substring")]),
        new("REC-097-F02", "function", ProviderKind.Basic,
            "select Name from #A.Entities() where Substrng(Name, 0, 2) = 'Wa'", DiagnosticCode.MQ3086_UnknownCallable, "Substrng", "Substring", null, null,
            ["callable", "candidateCallables", "suggestion"], [new("suggestion", "Substring")]),
        new("REC-097-F03", "function", ProviderKind.Basic,
            "select ToStrng(Name) from #A.Entities()", DiagnosticCode.MQ3086_UnknownCallable, "ToStrng", "ToString", null, null,
            ["callable", "candidateCallables", "suggestion"], [new("suggestion", "ToString")]),
        new("REC-097-F04", "function", ProviderKind.Basic,
            "select GetCountr() from #A.Entities()", DiagnosticCode.MQ3086_UnknownCallable, "GetCountr", "GetCountry", null, null,
            ["callable", "candidateCallables", "suggestion"], [new("suggestion", "GetCountry")]),
        new("REC-097-F05", "function", ProviderKind.Basic,
            "select Conct(Name, City) from #A.Entities()", DiagnosticCode.MQ3086_UnknownCallable, "Conct", "Concat", null, null,
            ["callable", "candidateCallables", "suggestion"], [new("suggestion", "Concat")]),
        new("REC-097-F06", "function", ProviderKind.Basic,
            "select Name from #A.Entities() order by Substrng(Name, 0, 2)", DiagnosticCode.MQ3086_UnknownCallable, "Substrng", "Substring", null, null,
            ["callable", "candidateCallables", "suggestion"], [new("suggestion", "Substring")]),
        new("REC-097-F07", "function", ProviderKind.OwnerAmbiguous,
            "select AmbiguousMethod(a.Population) from #A.entities() a inner join #B.entities() b on a.City = b.City",
            DiagnosticCode.MQ3035_AmbiguousMethodOwner, "AmbiguousMethod", null, "AmbiguousMethod", "a.AmbiguousMethod",
            [], []),
        new("REC-097-F08", "function", ProviderKind.OwnerAmbiguous,
            "select AmbiguousMethod(b.Population) from #A.entities() a inner join #B.entities() b on a.City = b.City",
            DiagnosticCode.MQ3035_AmbiguousMethodOwner, "AmbiguousMethod", null, "AmbiguousMethod", "b.AmbiguousMethod",
            [], []),

        // Arity and type overload failures.
        new("REC-097-O01", "overload", ProviderKind.Basic,
            "select Substring(Name) from #A.Entities()", DiagnosticCode.MQ3087_InvalidCallableArity, "Substring", null, null, null,
            ["callable", "actualTypes", "expectedCounts", "candidateSignatures"], []),
        new("REC-097-O02", "overload", ProviderKind.Basic,
            "select Substring(Name, 0, 2, 3) from #A.Entities()", DiagnosticCode.MQ3087_InvalidCallableArity, "Substring", null, null, null,
            ["callable", "actualTypes", "expectedCounts", "candidateSignatures"], []),
        new("REC-097-O03", "overload", ProviderKind.Basic,
            "select Length(Name, 1, 2) from #A.Entities()", DiagnosticCode.MQ3087_InvalidCallableArity, "Length", null, null, null,
            ["callable", "actualTypes", "expectedCounts", "candidateSignatures"], []),
        new("REC-097-O04", "overload", ProviderKind.Basic,
            "select Round(Population, 2, 3) from #A.Entities()", DiagnosticCode.MQ3087_InvalidCallableArity, "Round", null, null, null,
            ["callable", "actualTypes", "expectedCounts", "candidateSignatures"], []),
        new("REC-097-O05", "overload", ProviderKind.Basic,
            "select Substring(Name, 'zero', 5) from #A.Entities()", DiagnosticCode.MQ3088_NoMatchingCallableOverload, "Substring", null, null, null,
            ["callable", "actualTypes", "candidateSignatures"], []),
        new("REC-097-O06", "overload", ProviderKind.Basic,
            "select Substring(Population, 1, 2) from #A.Entities()", DiagnosticCode.MQ3088_NoMatchingCallableOverload, "Substring", null, null, null,
            ["callable", "actualTypes", "candidateSignatures"], []),
        new("REC-097-O07", "overload", ProviderKind.Basic,
            "select Sum(Name) from #A.Entities()", DiagnosticCode.MQ3088_NoMatchingCallableOverload, "Sum", null, null, null,
            ["callable", "actualTypes", "candidateSignatures"], []),
        new("REC-097-O08", "overload", ProviderKind.Basic,
            "select Abs(Name) from #A.Entities()", DiagnosticCode.MQ3088_NoMatchingCallableOverload, "Abs", null, null, null,
            ["callable", "actualTypes", "candidateSignatures"], [])
    ];

    private static QueryAnalysisResult Analyze(RecoveryCase candidate)
    {
        return candidate.Provider switch
        {
            ProviderKind.Basic => new QueryAnalyzer(
                    new BasicSchemaProvider<BasicEntity>(CreateBasicSources()),
                    compilationOptions: CompilationOptions)
                .Analyze(candidate.Query),
            ProviderKind.NamedSource => new QueryAnalyzer(
                    new RecoverySourceProvider(),
                    compilationOptions: CompilationOptions)
                .Analyze(candidate.Query),
            ProviderKind.OwnerAmbiguous => new QueryAnalyzer(
                    CreateOwnerProvider(),
                    compilationOptions: CompilationOptions)
                .Analyze(candidate.Query),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateBasicSources()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity("Warsaw", "PL", 100)],
            ["#B"] = [new BasicEntity("Berlin", "DE", 200)]
        };
    }

    private static GenericSchemaProvider CreateOwnerProvider()
    {
        return new GenericSchemaProvider(new Dictionary<string, ISchema>
        {
            ["#A"] = CreateOwnerSchema<MethodOwnerAutoResolutionTests.AmbiguousMethodLibraryA>(),
            ["#B"] = CreateOwnerSchema<MethodOwnerAutoResolutionTests.AmbiguousMethodLibraryB>()
        });
    }

    private static GenericSchema<TLibrary> CreateOwnerSchema<TLibrary>()
        where TLibrary : Musoq.Plugins.LibraryBase, new()
    {
        return new GenericSchema<TLibrary>(new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
        {
            ["entities"] = (
                new BasicEntityTable(),
                new MultiRowSource<BasicEntity>([new BasicEntity("Warsaw", "Poland", 100)]))
        });
    }

    private static string ApplyEdit(string text, TextEdit edit)
    {
        return text[..edit.Span.Start] + edit.NewText + text[edit.Span.End..];
    }

    private static string ReplaceOnce(string text, string oldText, string newText)
    {
        var index = text.IndexOf(oldText, StringComparison.Ordinal);
        if (index < 0)
            throw new InvalidOperationException($"Expected '{oldText}' in query '{text}'.");

        return text[..index] + newText + text[(index + oldText.Length)..];
    }

    private static string FormatDiagnostics(QueryAnalysisResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(static diagnostic =>
            $"[{diagnostic.Code}] {diagnostic.Message}"));
    }

    private enum ProviderKind
    {
        Basic,
        NamedSource,
        OwnerAmbiguous
    }

    private sealed record RecoveryCase(
        string CaseId,
        string Family,
        ProviderKind Provider,
        string Query,
        DiagnosticCode ExpectedCode,
        string MessageFragment,
        string? NativeReplacement,
        string? ManualRepairFrom,
        string? ManualRepairTo,
        IReadOnlyList<string> RequiredArgumentKeys,
        IReadOnlyList<KeyValuePair<string, string>> ExpectedArgumentValues);

    private sealed class RecoverySourceProvider : ISchemaProvider
    {
        private readonly RecoverySourceSchema _schema = new();

        public ISchema GetSchema(string schema)
        {
            if (schema.Equals("#rec097", StringComparison.OrdinalIgnoreCase) ||
                schema.Equals("rec097", StringComparison.OrdinalIgnoreCase))
                return _schema;

            throw new SourceNotFoundException($"Schema '{schema}' was not found.");
        }
    }

    private sealed class RecoverySourceSchema : SchemaBase
    {
        private static readonly SchemaMethodInfo[] Constructors =
        [
            new(
                "items",
                new SchemaConstructorInfo(
                    typeof(RecoverySourceConstructor)
                        .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Single(),
                    false,
                    ("sourcePath", typeof(string)),
                    ("pageSize", typeof(int))))
        ];

        public RecoverySourceSchema()
            : base("rec097", new MethodsAggregator(new MethodsManager()))
        {
        }

        public override SchemaMethodInfo[] GetRawConstructors(SourceMetadataContext metadataContext) => Constructors;

        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            if (!name.Equals("items", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException($"Table '{name}' is not supported.");

            return new RecoverySourceTable();
        }

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters)
        {
            return EnsureSourceType<T, IReadOnlyDictionary<string, object?>>(
                name,
                new RecoveryRowsSource());
        }
    }

    private sealed class RecoverySourceConstructor(string sourcePath, int pageSize = 7)
    {
        public string SourcePath { get; } = sourcePath;

        public int PageSize { get; } = pageSize;
    }

    private sealed class RecoverySourceTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [new SchemaColumn("Value", 0, typeof(string))];

        public ISchemaColumn? GetColumnByName(string name) =>
            Columns.SingleOrDefault(column => column.ColumnName == name);

        public ISchemaColumn[] GetColumnsByName(string name) =>
            Columns.Where(column => column.ColumnName == name).ToArray();

        public SchemaTableMetadata Metadata { get; } =
            new(typeof(IReadOnlyDictionary<string, object?>));
    }

    private sealed class RecoveryRowsSource : RowSourceBase<IReadOnlyDictionary<string, object?>>
    {
        protected override void CollectChunks(IChunkWriter<IReadOnlyDictionary<string, object?>> writer)
        {
            writer.Write([new Dictionary<string, object?> { ["Value"] = "value" }]);
        }
    }
}
