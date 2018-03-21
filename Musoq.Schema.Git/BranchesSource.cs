using System;
using System.Collections.Generic;
using LibGit2Sharp;

namespace Musoq.Schema.Git
{
    public class BranchesSource : RepositorySource<Branch>
    {
        public BranchesSource(string repo, IDictionary<string, int> nameToIndexMap, IDictionary<int, Func<Branch, object>> indexToObjectAccessMap) 
            : base(repo, nameToIndexMap, indexToObjectAccessMap)
        { }

        protected override int ChunkSize => 10;
        protected override IEnumerable<Branch> GetItems(Repository repo)
        {
            return repo.Branches;
        }
    }
}