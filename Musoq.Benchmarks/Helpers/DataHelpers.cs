using System.Globalization;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using Musoq.Benchmarks.Schema.Country;
using Musoq.Benchmarks.Schema.Profiles;

namespace Musoq.Benchmarks.Helpers;

public static class DataHelpers
{
    public static List<CountryEntity> ParseCountryData(string jsonFilePath)
    {
        var jsonContent = File.ReadAllText(jsonFilePath);
        var countriesDictionary = JsonSerializer.Deserialize<Dictionary<string, string[]>>(jsonContent);

        if (countriesDictionary == null) throw new Exception("Failed to parse the JSON content.");

        var countryEntities = new List<CountryEntity>();
        var random = new Random(345);

        foreach (var (country, cities) in countriesDictionary)
        foreach (var city in cities)
        {
            var entity = new CountryEntity(
                city,
                country,
                random.Next(100_000, 1_000_000)
            );

            countryEntities.Add(entity);
        }

        return countryEntities;
    }

    public static List<ProfileEntity> ReadProfiles(string csvFilePath)
    {
        using var reader = new StreamReader(csvFilePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<ProfileMap>();
        return csv.GetRecords<ProfileEntity>().ToList();
    }

    private sealed class ProfileMap : ClassMap<ProfileEntity>
    {
        public ProfileMap()
        {
            Map(m => m.FirstName).Name("first_name");
            Map(m => m.LastName).Name("last_name");
            Map(m => m.Email).Name("email");
            Map(m => m.Gender).Name("gender");
            Map(m => m.IpAddress).Name("ip_address");
            Map(m => m.Date).Name("date");
            Map(m => m.Image).Name("image");
            Map(m => m.Animal).Name("animal");
            Map(m => m.Avatar).Name("avatar");
        }
    }
}