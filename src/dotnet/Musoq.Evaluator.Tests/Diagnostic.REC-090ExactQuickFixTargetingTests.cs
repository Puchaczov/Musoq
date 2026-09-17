using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.NegativeTests;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticREC090ExactQuickFixTargetingTests : NegativeTestsBase
{
    [TestMethod]
    [DynamicData(nameof(QuickFixCases))]
    public void QuickFixTargetsOnlyTheFaultyOccurrence(
        string caseId,
        string alias,
        string misspelling,
        string query,
        string expectedQuery,
        string faultText)
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(caseId));
        AssertAliasEditAndExecute(query, expectedQuery, faultText, alias, misspelling);
    }

    public static IEnumerable<object[]> QuickFixCases()
    {
        const string alias = "people";
        const string misspelling = "peple";
        var cases = new[]
        {
            (
                CaseId: "REC-090-A01",
                Query: "select 'peple' as Literal, peple.Name from #test.people() people",
                ExpectedQuery: "select 'peple' as Literal, people.Name from #test.people() people",
                FaultText: "peple.Name"
            ),
            (
                CaseId: "REC-090-A02",
                Query: "select peple.Name, 'peple' as Literal from #test.people() people",
                ExpectedQuery: "select people.Name, 'peple' as Literal from #test.people() people",
                FaultText: "peple.Name"
            ),
            (
                CaseId: "REC-090-A03",
                Query: "select case when 1 = 1 then peple.Name else 'peple' end as Value from #test.people() people",
                ExpectedQuery: "select case when 1 = 1 then people.Name else 'peple' end as Value from #test.people() people",
                FaultText: "peple.Name"
            ),
            (
                CaseId: "REC-090-A04",
                Query: "select peple.Name from #test.people() people where 'peple' = 'peple'",
                ExpectedQuery: "select people.Name from #test.people() people where 'peple' = 'peple'",
                FaultText: "peple.Name"
            ),
            (
                CaseId: "REC-090-A05",
                Query: "select peple.Name from #test.people() people where 'prefix peple suffix' = 'prefix peple suffix'",
                ExpectedQuery: "select people.Name from #test.people() people where 'prefix peple suffix' = 'prefix peple suffix'",
                FaultText: "peple.Name"
            ),
            (
                CaseId: "REC-090-A06",
                Query: "select peple.Name from #test.people() people where 'peple' in ('peple', 'other')",
                ExpectedQuery: "select people.Name from #test.people() people where 'peple' in ('peple', 'other')",
                FaultText: "peple.Name"
            ),
            (
                CaseId: "REC-090-A07",
                Query: "select peple.Name from #test.people() people where 'peple' like 'pe%'",
                ExpectedQuery: "select people.Name from #test.people() people where 'peple' like 'pe%'",
                FaultText: "peple.Name"
            ),
            (
                CaseId: "REC-090-A08",
                Query: "select peple.Name from #test.people() people where 'peple' not like 'zz%'",
                ExpectedQuery: "select people.Name from #test.people() people where 'peple' not like 'zz%'",
                FaultText: "peple.Name"
            ),
            (
                CaseId: "REC-090-A09",
                Query: "select /* peple */ peple.Name from #test.people() people",
                ExpectedQuery: "select /* peple */ people.Name from #test.people() people",
                FaultText: "peple.Name"
            ),
            (
                CaseId: "REC-090-A10",
                Query: "select peple.Name from /* peple */ #test.people() people",
                ExpectedQuery: "select people.Name from /* peple */ #test.people() people",
                FaultText: "peple.Name"
            ),
            (
                CaseId: "REC-090-A11",
                Query: "-- peple\nselect peple.Name from #test.people() people",
                ExpectedQuery: "-- peple\nselect people.Name from #test.people() people",
                FaultText: "peple.Name"
            ),
            (
                CaseId: "REC-090-A12",
                Query: "select peple.Name from #test.people() people -- peple\n",
                ExpectedQuery: "select people.Name from #test.people() people -- peple\n",
                FaultText: "peple.Name"
            ),
            (
                CaseId: "REC-090-B01",
                Query: "select peple.Name as DisplayName from #test.people() people",
                ExpectedQuery: "select people.Name as DisplayName from #test.people() people",
                FaultText: "peple.Name"
            ),
            (
                CaseId: "REC-090-B02",
                Query: "select peple.Age + 1 as NextAge from #test.people() people",
                ExpectedQuery: "select people.Age + 1 as NextAge from #test.people() people",
                FaultText: "peple.Age"
            ),
            (
                CaseId: "REC-090-B03",
                Query: "select case when peple.Age > 30 then 1 else 0 end as IsAdult from #test.people() people",
                ExpectedQuery: "select case when people.Age > 30 then 1 else 0 end as IsAdult from #test.people() people",
                FaultText: "peple.Age"
            ),
            (
                CaseId: "REC-090-B04",
                Query: "select Length(peple.Name) as NameLength from #test.people() people",
                ExpectedQuery: "select Length(people.Name) as NameLength from #test.people() people",
                FaultText: "peple.Name"
            ),
            (
                CaseId: "REC-090-B05",
                Query: "select Count(peple.Id) as IdCount from #test.people() people",
                ExpectedQuery: "select Count(people.Id) as IdCount from #test.people() people",
                FaultText: "peple.Id"
            ),
            (
                CaseId: "REC-090-B06",
                Query: "select distinct peple.City from #test.people() people",
                ExpectedQuery: "select distinct people.City from #test.people() people",
                FaultText: "peple.City"
            ),
            (
                CaseId: "REC-090-B07",
                Query: "select peple.Name, Age from #test.people() people",
                ExpectedQuery: "select people.Name, Age from #test.people() people",
                FaultText: "peple.Name"
            ),
            (
                CaseId: "REC-090-B08",
                Query: "select peple.[Name] from #test.people() people",
                ExpectedQuery: "select people.[Name] from #test.people() people",
                FaultText: "peple.[Name]"
            ),
            (
                CaseId: "REC-090-B09",
                Query: "select (peple.Age) as AgeValue from #test.people() people",
                ExpectedQuery: "select (people.Age) as AgeValue from #test.people() people",
                FaultText: "peple.Age"
            ),
            (
                CaseId: "REC-090-B10",
                Query: "select peple.Salary * 2 as DoubleSalary from #test.people() people",
                ExpectedQuery: "select people.Salary * 2 as DoubleSalary from #test.people() people",
                FaultText: "peple.Salary"
            ),
            (
                CaseId: "REC-090-B11",
                Query: "select peple.Id % 2 as Parity from #test.people() people",
                ExpectedQuery: "select people.Id % 2 as Parity from #test.people() people",
                FaultText: "peple.Id"
            ),
            (
                CaseId: "REC-090-B12",
                Query: "select peple.Name as [peple] from #test.people() people",
                ExpectedQuery: "select people.Name as [peple] from #test.people() people",
                FaultText: "peple.Name"
            ),
            (
                CaseId: "REC-090-C01",
                Query: "select Name from #test.people() people where peple.Age > 20",
                ExpectedQuery: "select Name from #test.people() people where people.Age > 20",
                FaultText: "peple.Age"
            ),
            (
                CaseId: "REC-090-C02",
                Query: "select Name from #test.people() people where peple.City = 'London'",
                ExpectedQuery: "select Name from #test.people() people where people.City = 'London'",
                FaultText: "peple.City"
            ),
            (
                CaseId: "REC-090-C03",
                Query: "select Name from #test.people() people where peple.ManagerId is null",
                ExpectedQuery: "select Name from #test.people() people where people.ManagerId is null",
                FaultText: "peple.ManagerId"
            ),
            (
                CaseId: "REC-090-C04",
                Query: "select Count(1) as Total from #test.people() people group by peple.City",
                ExpectedQuery: "select Count(1) as Total from #test.people() people group by people.City",
                FaultText: "peple.City"
            ),
            (
                CaseId: "REC-090-C05",
                Query: "select Name from #test.people() people order by peple.Age",
                ExpectedQuery: "select Name from #test.people() people order by people.Age",
                FaultText: "peple.Age"
            ),
            (
                CaseId: "REC-090-C06",
                Query: "select Name from #test.people() people order by peple.Age desc",
                ExpectedQuery: "select Name from #test.people() people order by people.Age desc",
                FaultText: "peple.Age"
            ),
            (
                CaseId: "REC-090-C07",
                Query: "select Name from #test.people() people where peple.Id in (1, 2)",
                ExpectedQuery: "select Name from #test.people() people where people.Id in (1, 2)",
                FaultText: "peple.Id"
            ),
            (
                CaseId: "REC-090-C08",
                Query: "select Name from #test.people() people where peple.Age between 20 and 40",
                ExpectedQuery: "select Name from #test.people() people where people.Age between 20 and 40",
                FaultText: "peple.Age"
            ),
            (
                CaseId: "REC-090-C09",
                Query: "select Name from #test.people() people where not (peple.Age < 20)",
                ExpectedQuery: "select Name from #test.people() people where not (people.Age < 20)",
                FaultText: "peple.Age"
            ),
            (
                CaseId: "REC-090-C10",
                Query: "select City, Count(1) as Total from #test.people() people group by City having Count(peple.Id) > 0",
                ExpectedQuery: "select City, Count(1) as Total from #test.people() people group by City having Count(people.Id) > 0",
                FaultText: "peple.Id"
            ),
            (
                CaseId: "REC-090-C11",
                Query: "select distinct City from #test.people() people order by peple.City",
                ExpectedQuery: "select distinct City from #test.people() people order by people.City",
                FaultText: "peple.City"
            ),
            (
                CaseId: "REC-090-C12",
                Query: "select Name from #test.people() people where (peple.Age > 20)",
                ExpectedQuery: "select Name from #test.people() people where (people.Age > 20)",
                FaultText: "peple.Age"
            ),
            (
                CaseId: "REC-090-D01",
                Query: "with names as (select peple.Name from #test.people() people) select Name from names",
                ExpectedQuery: "with names as (select people.Name from #test.people() people) select Name from names",
                FaultText: "peple.Name"
            ),
            (
                CaseId: "REC-090-D02",
                Query: "with names as (select /* peple */ peple.Name from #test.people() people) select Name from names",
                ExpectedQuery: "with names as (select /* peple */ people.Name from #test.people() people) select Name from names",
                FaultText: "peple.Name"
            ),
            (
                CaseId: "REC-090-D03",
                Query: "with names as (select Name from #test.people() people where peple.Age > 20) select Name from names",
                ExpectedQuery: "with names as (select Name from #test.people() people where people.Age > 20) select Name from names",
                FaultText: "peple.Age"
            ),
            (
                CaseId: "REC-090-D04",
                Query: "with names as (select Count(1) as Total from #test.people() people group by peple.City) select Total from names",
                ExpectedQuery: "with names as (select Count(1) as Total from #test.people() people group by people.City) select Total from names",
                FaultText: "peple.City"
            ),
            (
                CaseId: "REC-090-D05",
                Query: "with base as (select Name, Id from #test.people() people), names as (select people.Name from base people where peple.Id > 0) select Name from names",
                ExpectedQuery: "with base as (select Name, Id from #test.people() people), names as (select people.Name from base people where people.Id > 0) select Name from names",
                FaultText: "peple.Id"
            ),
            (
                CaseId: "REC-090-D06",
                Query: "with names as (select Name, Id from #test.people() people), filtered as (select people.Name from names people where peple.Id > 0) select Name from filtered",
                ExpectedQuery: "with names as (select Name, Id from #test.people() people), filtered as (select people.Name from names people where people.Id > 0) select Name from filtered",
                FaultText: "peple.Id"
            ),
            (
                CaseId: "REC-090-D07",
                Query: "select (select peple.Name from #test.people() people where people.Id = 1) as FirstName from #test.people() outerPeople",
                ExpectedQuery: "select (select people.Name from #test.people() people where people.Id = 1) as FirstName from #test.people() outerPeople",
                FaultText: "peple.Name"
            ),
            (
                CaseId: "REC-090-D08",
                Query: "select Name from #test.people() outerPeople where Id in (select peple.Id from #test.people() people where people.Age > 30)",
                ExpectedQuery: "select Name from #test.people() outerPeople where Id in (select people.Id from #test.people() people where people.Age > 30)",
                FaultText: "peple.Id"
            ),
            (
                CaseId: "REC-090-D09",
                Query: "select Name from #test.people() outerPeople where exists (select people.Id from #test.people() people where people.Id = outerPeople.Id and peple.Age > 0)",
                ExpectedQuery: "select Name from #test.people() outerPeople where exists (select people.Id from #test.people() people where people.Id = outerPeople.Id and people.Age > 0)",
                FaultText: "peple.Age"
            ),
            (
                CaseId: "REC-090-D10",
                Query: "select names.Name from (select peple.Name from #test.people() people) names",
                ExpectedQuery: "select names.Name from (select people.Name from #test.people() people) names",
                FaultText: "peple.Name"
            ),
            (
                CaseId: "REC-090-D11",
                Query: "with names as (select Name from #test.people() where Id in (select peple.Id from #test.people() people where people.Age > 0)) select Name from names",
                ExpectedQuery: "with names as (select Name from #test.people() where Id in (select people.Id from #test.people() people where people.Age > 0)) select Name from names",
                FaultText: "peple.Id"
            ),
            (
                CaseId: "REC-090-D12",
                Query: "with raw as (select Name, Id from #test.people()), names as (select Name from raw where (select peple.Id from #test.people() people where people.Id = raw.Id) = raw.Id) select Name from names",
                ExpectedQuery: "with raw as (select Name, Id from #test.people()), names as (select Name from raw where (select people.Id from #test.people() people where people.Id = raw.Id) = raw.Id) select Name from names",
                FaultText: "peple.Id"
            )
        };

        return cases.Select(static item => new object[] { item.CaseId, alias, misspelling, item.Query, item.ExpectedQuery, item.FaultText });
    }

    private void AssertAliasEditAndExecute(
        string query,
        string expectedQuery,
        string faultText,
        string expectedAlias,
        string misspelling)
    {
        var exception = Assert.Throws<MusoqQueryException>(() => CompileQuery(query));
        AssertSingleError(exception, DiagnosticCode.MQ3015_UnknownAlias, DiagnosticPhase.Bind, $"Did you mean '{expectedAlias}'?");

        var envelope = exception.PrimaryEnvelope;
        var faultStart = query.IndexOf(faultText, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, faultStart, $"Fault text '{faultText}' was not found.");
        var faultSpan = new TextSpan(faultStart, misspelling.Length);

        Assert.AreEqual(faultSpan.Start, envelope.Offset);
        Assert.AreEqual(faultSpan.Length, envelope.Length);
        Assert.AreEqual(misspelling, query.Substring(envelope.Offset!.Value, envelope.Length!.Value));
        Assert.AreEqual(expectedAlias, envelope.Arguments["suggestion"]);
        Assert.HasCount(1, envelope.Actions);

        var action = envelope.Actions.Single();
        Assert.AreEqual(DiagnosticActionKind.QuickFix, action.Kind);
        Assert.IsNotNull(action.TextEdit);
        Assert.AreEqual(faultSpan, action.TextEdit!.Span);
        Assert.AreEqual(expectedAlias, action.TextEdit.NewText);

        var repairedQuery = query.Remove(action.TextEdit.Span.Start, action.TextEdit.Span.Length)
            .Insert(action.TextEdit.Span.Start, action.TextEdit.NewText);
        Assert.AreEqual(expectedQuery, repairedQuery);

        var compiled = CompileQuery(repairedQuery);
        var table = compiled.Run(TokenSource.Token);
        Assert.IsGreaterThan(0, table.Count);
    }
}
