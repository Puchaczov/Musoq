using System;
using System.Collections.Generic;
using LibGit2Sharp;

namespace Musoq.Schema.Git
{
    public class TagsSource : RepositorySource<Tag>
    {
        public TagsSource(string repo, IDictionary<string, int> nameToIndexMap, IDictionary<int, Func<Tag, object>> indexToObjectAccessMap) : base(repo, nameToIndexMap, indexToObjectAccessMap)
        {
        }

        protected override int ChunkSize => 10;

        protected override IEnumerable<Tag> GetItems(Repository repo)
        {
            return repo.Tags;
        }
    }
}