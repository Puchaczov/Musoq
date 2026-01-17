namespace Musoq.Benchmarks.Schema.Country;

public class CountryEntity(string city, string country, int population)
{
    public static readonly IDictionary<string, int> KNameToIndexMap;
    public static readonly IDictionary<int, Func<CountryEntity, object>> KIndexToObjectAccessMap;

    static CountryEntity()
    {
        KNameToIndexMap = new Dictionary<string, int>
        {
            { nameof(City), 11 },
            { nameof(Country), 12 },
            { nameof(Population), 13 }
        };

        KIndexToObjectAccessMap = new Dictionary<int, Func<CountryEntity, object>>
        {
            { 11, arg => arg.City },
            { 12, arg => arg.Country },
            { 13, arg => arg.Population }
        };
    }

    public string Country { get; init; } = country;

    public string City { get; init; } = city;

    public decimal Population { get; init; } = population;
}