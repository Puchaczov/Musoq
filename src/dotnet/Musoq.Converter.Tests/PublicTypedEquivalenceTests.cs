using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator;
using Musoq.Tests.Common;
using MusoqApi = Musoq.Converter.Musoq;
using static Musoq.Converter.Tests.TwoModeTestFixtures;
using City = Musoq.Converter.Tests.TwoModeTestFixtures.NameRow;
using CityCountDto = Musoq.Converter.Tests.TwoModeTestFixtures.CityCountDto;
using CityDto = Musoq.Converter.Tests.TwoModeTestFixtures.CityDto;
using NameAgeDto = Musoq.Converter.Tests.TwoModeTestFixtures.NameAgeDto;
using NameDto = Musoq.Converter.Tests.TwoModeTestFixtures.NameDto;
using NameCityAgeDto = Musoq.Converter.Tests.TwoModeTestFixtures.NameCityAgeDto;
using NameNumberDto = Musoq.Converter.Tests.TwoModeTestFixtures.NameNumberDto;
using NameRankDto = Musoq.Converter.Tests.TwoModeTestFixtures.NameRankDto;
using Person = Musoq.Converter.Tests.TwoModeTestFixtures.Person;
using PersonCityDto = Musoq.Converter.Tests.TwoModeTestFixtures.PersonCityDto;
using PersonStarDto = Musoq.Converter.Tests.TwoModeTestFixtures.PersonStarDto;
using ScoredPerson = Musoq.Converter.Tests.TwoModeTestFixtures.ScoredPerson;
using ValueDto = Musoq.Converter.Tests.TwoModeTestFixtures.ValueDto;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class PublicTypedEquivalenceTests
{
    static PublicTypedEquivalenceTests()
    {
        Culture.ApplyWithDefaultCulture();
    }

    [TestMethod]
    public void TypedExecution_WhenQueryUsesJoin_ShouldMatchEquivalentProjection()
    {
        var people = People();
        var cities = new[] { new City("NY"), new City("SF") };

        var actual = MusoqApi
            .Query("select p.Name as Name from #A.entities() p inner join #B.entities() c on p.City = c.Name order by p.Name")
            .Source("#A", "entities", Chunks(people))
            .Source("#B", "entities", Chunks(cities))
            .WithCompilationOptions(new CompilationOptions(ParallelizationMode.None))
            .CompileAndRun<NameDto>(CancellationToken.None)
            .Select(static row => row.Name)
            .ToArray();
        var expected = people
            .Join(cities, static person => person.City, static city => city.Name, static (person, _) => person.Name)
            .OrderBy(static name => name)
            .ToArray();

        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void TypedExecution_WhenQueryUsesOrderSkipTake_ShouldMatchEquivalentProjection()
    {
        var people = People();

        var actual = MusoqApi
            .Query("select p.Name as Name from #A.entities() p order by p.Age desc skip 1 take 2")
            .Source("#A", "entities", Chunks(people))
            .WithCompilationOptions(new CompilationOptions(ParallelizationMode.None))
            .CompileAndRun<NameDto>(CancellationToken.None)
            .Select(static row => row.Name)
            .ToArray();
        var expected = people
            .OrderByDescending(static person => person.Age)
            .Skip(1)
            .Take(2)
            .Select(static person => person.Name)
            .ToArray();

        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void TypedExecution_WhenQueryUsesDistinctOrderSkipTake_ShouldMatchEquivalentProjection()
    {
        var people = People();

        var actual = MusoqApi
            .Query("select distinct p.City as City from #A.entities() p order by p.City desc skip 1 take 2")
            .Source("#A", "entities", Chunks(people))
            .WithCompilationOptions(new CompilationOptions(ParallelizationMode.None))
            .CompileAndRun<CityDto>(CancellationToken.None)
            .Select(static row => row.City)
            .ToArray();
        var expected = people
            .Select(static person => person.City)
            .Distinct()
            .OrderByDescending(static city => city)
            .Skip(1)
            .Take(2)
            .ToArray();

        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void TypedExecution_WhenQueryUsesGroupedAggregate_ShouldMatchEquivalentProjection()
    {
        var people = People();

        var actual = MusoqApi
            .Query("select p.City as City, Count(p.Name) as Count from #A.entities() p group by p.City order by p.City")
            .Source("#A", "entities", Chunks(people))
            .WithCompilationOptions(new CompilationOptions(ParallelizationMode.None))
            .CompileAndRun<CityCountDto>(CancellationToken.None)
            .Select(static row => (row.City, row.Count))
            .ToArray();
        var expected = people
            .GroupBy(static person => person.City)
            .OrderBy(static group => group.Key)
            .Select(static group => (group.Key, Count: (long)group.Count()))
            .ToArray();

        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void TypedExecution_WhenQueryUsesWindowFunction_ShouldMatchEquivalentProjection()
    {
        var people = People();

        var actual = MusoqApi
            .Query("select p.Name as Name, RowNumber() over (order by p.Age, p.Name) as Rank from #A.entities() p order by Rank")
            .Source("#A", "entities", Chunks(people))
            .WithCompilationOptions(new CompilationOptions(ParallelizationMode.None))
            .CompileAndRun<NameRankDto>(CancellationToken.None)
            .Select(static row => (row.Name, row.Rank))
            .ToArray();
        var expected = people
            .OrderBy(static person => person.Age)
            .ThenBy(static person => person.Name)
            .Select(static (person, index) => (person.Name, Rank: index + 1L))
            .ToArray();

        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void TypedExecution_WhenQueryUsesCte_ShouldMatchEquivalentProjection()
    {
        var people = People();

        var actual = MusoqApi
            .Query("""
                   with adults as (
                       select p.Name as Name, p.Age as Age from #A.entities() p where p.Age >= 30
                   )
                   select a.Name as Name, a.Age as Age from adults a order by a.Name
                   """)
            .Source("#A", "entities", Chunks(people))
            .WithCompilationOptions(new CompilationOptions(ParallelizationMode.None))
            .CompileAndRun<NameAgeDto>(CancellationToken.None)
            .Select(static row => (row.Name, row.Age))
            .ToArray();
        var expected = people
            .Where(static person => person.Age >= 30)
            .OrderBy(static person => person.Name)
            .Select(static person => (person.Name, person.Age))
            .ToArray();

        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void TypedExecution_WhenQueryUsesSetOperation_ShouldMatchEquivalentProjection()
    {
        var people = People();

        var actual = MusoqApi
            .Query("""
                   select p.Name as Name from #A.entities() p where p.City = 'NY'
                   union (Name)
                   select q.Name as Name from #A.entities() q where q.Age >= 30
                   """)
            .Source("#A", "entities", Chunks(people))
            .WithCompilationOptions(new CompilationOptions(ParallelizationMode.None))
            .CompileAndRun<NameDto>(CancellationToken.None)
            .Select(static row => row.Name)
            .ToArray();
        var expected = people
            .Where(static person => person.City == "NY")
            .Select(static person => person.Name)
            .Union(people
                .Where(static person => person.Age >= 30)
                .Select(static person => person.Name))
            .ToArray();

        CollectionAssert.AreEquivalent(expected, actual);
    }

    [TestMethod]
    public void TypedExecution_WhenQueryUsesApplyChain_ShouldMatchEquivalentProjection()
    {
        var people =
            new[]
            {
                new ScoredPerson("Alice", 35, "NY", [2, 1]),
                new ScoredPerson("Bob", 20, "LA", [3])
            };

        var actual = MusoqApi
            .Query("select p.Name as Name, s.Value as Number from #A.entities() p cross apply p.Scores as s order by p.Name, s.Value")
            .Source("#A", "entities", Chunks(people))
            .WithCompilationOptions(new CompilationOptions(ParallelizationMode.None))
            .CompileAndRun<NameNumberDto>(CancellationToken.None)
            .Select(static row => (row.Name, row.Number))
            .ToArray();
        var expected = people
            .SelectMany(static person => person.Scores.Select(score => (person.Name, Number: score)))
            .OrderBy(static row => row.Name)
            .ThenBy(static row => row.Number)
            .ToArray();

        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void TypedExecution_WhenQueryUsesAdvancedJoinShape_ShouldMatchEquivalentProjection()
    {
        var people = People();
        var cities = new[] { new City("NY"), new City("LA") };

        var actual = MusoqApi
            .Query("""
                   select p.Name as Name, p.City as City, p.Age as Age
                   from #A.entities() p
                   inner join #B.entities() c on p.City = c.Name
                   where p.Age >= 25
                   order by p.City, p.Age desc
                   """)
            .Source("#A", "entities", Chunks(people))
            .Source("#B", "entities", Chunks(cities))
            .WithCompilationOptions(new CompilationOptions(ParallelizationMode.None))
            .CompileAndRun<NameCityAgeDto>(CancellationToken.None)
            .Select(static row => (row.Name, row.City, row.Age))
            .ToArray();
        var expected = people
            .Join(cities, static person => person.City, static city => city.Name, static (person, _) => person)
            .Where(static person => person.Age >= 25)
            .OrderBy(static person => person.City)
            .ThenByDescending(static person => person.Age)
            .Select(static person => (person.Name, person.City, person.Age))
            .ToArray();

        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void TypedExecution_WhenQueryFiltersNulls_ShouldMatchEquivalentProjection()
    {
        var people = PeopleWithNullCity();

        var actual = MusoqApi
            .Query("select p.Name as Name, p.City as City from #A.entities() p where p.City is null order by p.Name")
            .Source("#A", "entities", Chunks(people))
            .WithCompilationOptions(new CompilationOptions(ParallelizationMode.None))
            .CompileAndRun<PersonCityDto>(CancellationToken.None)
            .Select(static row => (row.Name, row.City))
            .ToArray();
        var expected = people
            .Where(static person => person.City == null)
            .OrderBy(static person => person.Name)
            .Select(static person => (person.Name, person.City))
            .ToArray();

        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void TypedExecution_WhenQueryUsesSelectStar_ShouldBindAllProjectedMembers()
    {
        var people = People();

        var actual = MusoqApi
            .Query("select * from #A.entities() p where p.Name = 'Alice'")
            .Source("#A", "entities", Chunks(people))
            .WithCompilationOptions(new CompilationOptions(ParallelizationMode.None))
            .CompileAndRun<PersonStarDto>(CancellationToken.None)
            .Single();

        Assert.AreEqual("Alice", actual.Name);
        Assert.AreEqual(35, actual.Age);
        Assert.AreEqual("NY", actual.City);
    }

    [TestMethod]
    public void TypedExecution_WhenQueryUsesParameter_ShouldMatchEquivalentProjection()
    {
        var people = People();
        var minAge = 30;

        var actual = MusoqApi
            .Query("param(minAge: int) select p.Name as Name from #A.entities() p where p.Age >= $minAge order by p.Name")
            .Source<Person>("#A", "entities")
            .WithCompilationOptions(new CompilationOptions(ParallelizationMode.None))
            .Compile<NameDto>()
            .Run(
                new TypedQueryRunOptions(
                    CancellationToken.None,
                    new Dictionary<string, object?> { ["minAge"] = minAge }),
                MusoqApi.Source("#A", "entities", Chunks(people)))
            .Select(static row => row.Name)
            .ToArray();
        var expected = people
            .Where(person => person.Age >= minAge)
            .OrderBy(static person => person.Name)
            .Select(static person => person.Name)
            .ToArray();

        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void TypedExecution_WhenQueryHasDuplicateAliases_ShouldRejectLikeOutputBindingRules()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => MusoqApi
            .Query("select p.Name as Value, p.City as Value from #A.entities() p")
            .Source<Person>("#A", "entities")
            .Compile<ValueDto>());

        StringAssert.Contains(exception.Message, "duplicate output alias 'Value'");
    }

}
