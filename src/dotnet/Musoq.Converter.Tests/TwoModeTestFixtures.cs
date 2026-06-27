using System;
using System.Collections;
using System.Collections.Generic;

namespace Musoq.Converter.Tests;

public static class TwoModeTestFixtures
{
    public static Person[] People()
    {
        return
        [
            new Person("Alice", 35, "NY"),
            new Person("Bob", 20, "LA"),
            new Person("Charlie", 28, "NY"),
            new Person("Dana", 41, "SF"),
            new Person("Eve", 32, "LA")
        ];
    }

    public static Person[] PeopleWithNullCity()
    {
        return
        [
            new Person("Alice", 35, "NY"),
            new Person("NoCity", 25, null),
            new Person("AlsoNoCity", 31, null)
        ];
    }

    public static IReadOnlyList<T>[] Chunks<T>(IReadOnlyList<T> rows)
    {
        return [rows];
    }

    public sealed record Person(string Name, int Age, string? City);

    public sealed record NameRow(string Name);

    public sealed record NameDto(string Name);

    public sealed record NumberDto(int Number);

    public sealed record CityDto(string City);

    public sealed record PersonCityDto(string Name, string? City);

    public sealed record PersonStarDto(string Name, int Age, string? City);

    public sealed record ValueDto(string Value);

    public sealed record CityCountDto(string? City, long Count);

    public sealed record NameNumberDto(string Name, int Number);

    public sealed record NameRankDto(string Name, long Rank);

    public sealed record NameAgeDto(string Name, int Age);

    public sealed record NameCityAgeDto(string Name, string? City, int Age);

    public sealed record ScoredPerson(string Name, int Age, string? City, int[] Scores);

    public sealed class FieldPerson(string name, int age)
    {
        public string Name = name;

        public int Age = age;
    }

    public sealed class AmbiguousPerson
    {
        public string Name { get; init; } = string.Empty;

        public string name = string.Empty;
    }

    public sealed class ThrowOnSecondMoveChunkEnumerable<T>(IReadOnlyList<T> first) : IEnumerable<IReadOnlyList<T>>
    {
        public IEnumerator<IReadOnlyList<T>> GetEnumerator()
        {
            yield return first;
            throw new InvalidOperationException("Second chunk should not be requested before the first typed result is consumed.");
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
