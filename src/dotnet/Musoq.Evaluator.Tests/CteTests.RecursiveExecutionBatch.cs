using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class CteTests
{
    private static readonly CompiledQueryBatchRepository<string> RecursiveUnionAllQueries =
        new(CreateRecursiveUnionAllQueries);

    private static readonly SemaphoreSlim RecursiveOptionBatchInitializationGate = new(1, 1);

    private static readonly Lazy<IReadOnlyDictionary<string, CompiledQueryBatchRepository<string>>>
        RecursiveOptionQueriesByProfile =
            new(CreateRecursiveOptionRepositories, LazyThreadSafetyMode.ExecutionAndPublication);

    private static CompiledQuery GetRecursiveUnionAllQuery(RecursiveCteSupportedCase testCase)
    {
        return RecursiveUnionAllQueries.Take(testCase.Name);
    }

    private static CompiledQuery GetRecursiveOptionQuery(
        RecursiveCteSupportedCase testCase,
        RecursiveOptionProfile profile)
    {
        return RecursiveOptionQueriesByProfile.Value[profile.Name].Take(testCase.Name);
    }

    [ClassCleanup]
    public static void DisposeRecursiveExecutionBatches()
    {
        RecursiveUnionAllQueries.Dispose();
        if (RecursiveOptionQueriesByProfile.IsValueCreated)
        {
            foreach (var repository in RecursiveOptionQueriesByProfile.Value.Values)
                repository.Dispose();
        }

        RecursiveOptionBatchInitializationGate.Dispose();
    }

    private static IReadOnlyDictionary<string, CompiledQuery> CreateRecursiveUnionAllQueries()
    {
        var requests = RecursiveCteSupportedCaseCatalog.Cases
            .Select((testCase, index) => new ExecutionBatchCompilationRequest(
                testCase.Name,
                testCase.Query,
                $"RecursiveUnionBatch_{index}",
                testCase.CreateSchemaProvider == null
                    ? new BasicSchemaProvider<BasicEntity>(CreateSingleSource())
                    : testCase.CreateSchemaProvider(),
                new TestsLoggerResolver(),
                testCase.CompilationOptions,
                ConsumerFamily: "recursive-union-all",
                ConsumerTestName: nameof(CreateRecursiveUnionAllQueries),
                BatchOrigin: "recursive-union-all"))
            .ToArray();

        return CompileBatch(requests, "recursive union");
    }

    private static IReadOnlyDictionary<string, CompiledQueryBatchRepository<string>>
        CreateRecursiveOptionRepositories()
    {
        return RecursiveOptionProfiles.ToDictionary(
            static profile => profile.Name,
            static profile => new CompiledQueryBatchRepository<string>(
                () => CreateRecursiveOptionQueries(profile)),
            StringComparer.Ordinal);
    }

    private static IReadOnlyDictionary<string, CompiledQueryBatchEntry> CreateRecursiveOptionQueries(
        RecursiveOptionProfile profile)
    {
        RecursiveOptionBatchInitializationGate.Wait();
        try
        {
            var requests = RecursiveCteSupportedCaseCatalog.Cases
                .Select((testCase, index) => new ExecutionBatchCompilationRequest(
                    testCase.Name,
                    testCase.Query,
                    $"RecursiveOptionBatch_{profile.Name}_{index}",
                    testCase.CreateSchemaProvider == null
                        ? new BasicSchemaProvider<BasicEntity>(CreateSingleSource())
                        : testCase.CreateSchemaProvider(),
                    new TestsLoggerResolver(),
                    ApplyProfile(testCase.CompilationOptions, profile),
                    ConsumerFamily: "recursive-optimizer-matrix",
                    ConsumerTestName: profile.Name,
                    BatchOrigin: "recursive-optimizer-matrix"))
                .ToArray();

            var results = InstanceCreator.CompileForExecutionBatch(requests);
            return results.ToDictionary(
                static result => result.Key,
                result => result.Result.Succeeded
                    ? CompiledQueryBatchEntry.Success(result.Result.CompiledQuery)
                    : CompiledQueryBatchEntry.Failure(
                        new InvalidOperationException(
                            $"Recursive option case '{result.Key}' in profile '{profile.Name}' failed to compile.",
                            result.Result.CaughtException)),
                StringComparer.Ordinal);
        }
        finally
        {
            RecursiveOptionBatchInitializationGate.Release();
        }
    }

    private static IReadOnlyDictionary<string, CompiledQuery> CompileBatch(
        IReadOnlyList<ExecutionBatchCompilationRequest> requests,
        string batchName)
    {
        var results = InstanceCreator.CompileForExecutionBatch(requests);
        var queries = new Dictionary<string, CompiledQuery>(StringComparer.Ordinal);
        try
        {
            foreach (var result in results)
            {
                if (!result.Result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Recursive {batchName} case '{result.Key}' failed to compile.",
                        result.Result.CaughtException);
                }

                queries.Add(result.Key, result.Result.CompiledQuery);
            }

            return queries;
        }
        catch
        {
            foreach (var query in queries.Values)
                query.Dispose();
            foreach (var result in results)
            {
                if (result.Result.Succeeded && !queries.ContainsKey(result.Key))
                    result.Result.CompiledQuery.Dispose();
            }

            throw;
        }
    }

}
