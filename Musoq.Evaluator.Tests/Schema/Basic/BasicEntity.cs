using System;
using System.Collections.Generic;

namespace Musoq.Evaluator.Tests.Schema.Basic
{
    public class BasicEntity
    {
        public static readonly IDictionary<string, int> TestNameToIndexMap;
        public static readonly IDictionary<int, Func<BasicEntity, object>> TestIndexToObjectAccessMap;

        static BasicEntity()
        {
            TestNameToIndexMap = new Dictionary<string, int>
            {
                {nameof(Name), 10},
                {nameof(City), 11},
                {nameof(Country), 12},
                {nameof(Population), 13},
                {nameof(Self), 14},
                {nameof(Money), 15},
                {nameof(Month), 16},
                {nameof(Time), 17},
                {nameof(Id), 18},
                {nameof(NullableValue), 19},
                {nameof(Other), 20},
                {nameof(Array), 21},
                {nameof(Dictionary), 22}
            };

            TestIndexToObjectAccessMap = new Dictionary<int, Func<BasicEntity, object>>
            {
                {10, arg => arg.Name},
                {11, arg => arg.City},
                {12, arg => arg.Country},
                {13, arg => arg.Population},
                {14, arg => arg.Self},
                {15, arg => arg.Money},
                {16, arg => arg.Month},
                {17, arg => arg.Time},
                {18, arg => arg.Id},
                {19, arg => arg.NullableValue},
                {20, arg => arg.Other},
                {21, arg => arg.Array},
                {22, arg => arg.Dictionary}
            };
        }
        
        public BasicEntity()
        {
        }

        public BasicEntity(string name)
        {
            Name = name;
        }

        public BasicEntity(string country, string city)
        {
            Country = country;
            City = city;
        }

        public BasicEntity(string country, int population)
        {
            Country = country;
            Population = population;
        }

        public BasicEntity(string city, string country, int population)
        {
            City = city;
            Country = country;
            Population = population;
        }

        public BasicEntity(string month, decimal money)
        {
            Month = month;
            Money = money;
        }

        public BasicEntity(string city, string month, decimal money)
        {
            City = city;
            Month = month;
            Money = money;
        }

        public BasicEntity(DateTime time)
        {
            Time = time;
        }

        public string Month { get; set; }

        public string Name { get; set; }

        public string Country { get; set; }

        public string City { get; set; }

        public decimal Population { get; set; }

        public decimal Money { get; set; }

        public DateTime Time { get; set; }

        public BasicEntity Self => this;

        public BasicEntity Other => this;

        public int Id { get; set; }

        public int[] Array => new[] {0, 1, 2};
        
        public Dictionary<string, string> Dictionary => new()
        {
            {"A", "B"},
            {"C", "D"},
            {"AA", "BB"},
            {"CC", "DD"}
        };

        public int? NullableValue { get; set; }

        public override string ToString()
        {
            return "TEST STRING";
        }
    }
}