using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Solution
{
    public class SolutionSource : RowSource
    {
        private readonly string _solution;

        public SolutionSource(string solution)
        {
            _solution = solution;
        }

        public override IEnumerable<IObjectResolver> Rows
        {
            get
            {
                var workspace = MSBuildWorkspace.Create();
                var solution = workspace.OpenSolutionAsync(_solution).Result;

                foreach (var project in solution.Projects)
                foreach (var document in project.Documents)
                    yield return new EntityResolver<Document>(document, SchemaSolutionHelper.NameToIndexMap,
                        SchemaSolutionHelper.IndexToMethodAccessMap);
            }
        }
    }
}