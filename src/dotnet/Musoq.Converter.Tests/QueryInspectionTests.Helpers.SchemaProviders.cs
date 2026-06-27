using System;
using System.Collections.Generic;
using Musoq.Converter.Tests.Schema;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    private static DynamicRowsSchemaProvider CreateDynamicRowsSchemaProvider()
    {
        var columns = new Dictionary<string, Type>
        {
            ["Team"] = typeof(string),
            ["Name"] = typeof(string),
            ["Score"] = typeof(int)
        };
        var rows = new List<IReadOnlyDictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                ["Team"] = "a",
                ["Name"] = "ada",
                ["Score"] = 2
            },
            new Dictionary<string, object>
            {
                ["Team"] = "a",
                ["Name"] = "bea",
                ["Score"] = 1
            },
            new Dictionary<string, object>
            {
                ["Team"] = "b",
                ["Name"] = "cid",
                ["Score"] = 3
            }
        };

        return new DynamicRowsSchemaProvider(columns, rows);
    }

    private static ApplyCandidateSchemaProvider CreateApplyCandidateSchemaProvider()
    {
        return new ApplyCandidateSchemaProvider(
        [
            new ApplyCandidateEntity
            {
                Name = "left",
                Line = "INFO ready",
                Numbers = [1, 2]
            },
            new ApplyCandidateEntity
            {
                Name = "right",
                Line = "WARN retry",
                Numbers = [3]
            }
        ]);
    }

    private static ApplyCandidateSchemaProvider CreateAliasDistinctAggregateSchemaProvider()
    {
        return new ApplyCandidateSchemaProvider(
        [
            new ApplyCandidateEntity
            {
                Name = "left",
                Line = "INFO ready",
                Numbers = [1, 2],
                Content = [10, 20]
            },
            new ApplyCandidateEntity
            {
                Name = "right",
                Line = "WARN retry",
                Numbers = [3],
                Content = [7]
            }
        ]);
    }

    private static ApplyCandidateSchemaProvider CreateAliasDistinctAggregateSortSchemaProvider()
    {
        return new ApplyCandidateSchemaProvider(
        [
            new ApplyCandidateEntity
            {
                Name = "left",
                Line = "INFO ready",
                Numbers = [10],
                Content = [1]
            },
            new ApplyCandidateEntity
            {
                Name = "right",
                Line = "WARN retry",
                Numbers = [1, 2],
                Content = [50]
            }
        ]);
    }

    private static ApplyCandidateSchemaProvider CreateMixedRegularAndDistinctAggregateSchemaProvider()
    {
        return new ApplyCandidateSchemaProvider(
        [
            new ApplyCandidateEntity
            {
                Name = "left",
                Line = "INFO ready",
                Numbers = [1, 1]
            },
            new ApplyCandidateEntity
            {
                Name = "right",
                Line = "WARN retry",
                Numbers = [2]
            }
        ]);
    }

    private static ApplyCandidateSchemaProvider CreateMixedDistinctAggregateFamilySchemaProvider()
    {
        return new ApplyCandidateSchemaProvider(
        [
            new ApplyCandidateEntity
            {
                Name = "left",
                Line = "INFO ready",
                Numbers = [1, 1, 4]
            },
            new ApplyCandidateEntity
            {
                Name = "right",
                Line = "WARN retry",
                Numbers = [3]
            }
        ]);
    }
}
