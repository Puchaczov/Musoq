namespace Musoq.Benchmarks.Scenarios;

public static partial class ScenarioCatalog
{
    private static void AddProfiles(List<Scenario> scenarios)
    {
        Add("B14_Group", "B14_Group_ManyGroups", "many-groups", "Group N distinct main-input keys with filtered distinct aggregation.");
        Add("B07_EqualityJoin", "B07_EqualityJoin_Shuffled", "shuffled", "Join unique keys over reproducibly shuffled main-input rows.");
        Add("B07_DuplicateJoin", "B07_DuplicateJoin_Fanout8", "fanout8", "Join against an eight-row-per-key dimension with bounded output.");

        void Add(string baseId, string id, string profile, string description) => scenarios.Add(
            scenarios.Single(scenario => scenario.Id == baseId) with
            {
                Id = id, DataProfileId = profile, Description = description
            });
    }
}
