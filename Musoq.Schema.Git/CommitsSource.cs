using System;
using System.Collections.Generic;
using LibGit2Sharp;

namespace Musoq.Schema.Git
{
    public class CommitsSource : RepositorySource<Commit>
    {
        public CommitsSource(string repo, IDictionary<string, int> nameToIndexMap,
            IDictionary<int, Func<Commit, object>> indexToObjectAccessMap)
            : base(repo, nameToIndexMap, indexToObjectAccessMap)
        {
        }

        protected override int ChunkSize => 100;

        protected override IEnumerable<Commit> GetItems(Repository repo)
        {
            return repo.Commits;
        }
    }
}