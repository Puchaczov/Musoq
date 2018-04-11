using System;
using System.Collections.Generic;
using LibGit2Sharp;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Git
{
    public class SchemaGitHelper
    {
        public static readonly IDictionary<string, int> CommitsNameToIndexMap;
        public static readonly IDictionary<int, Func<Commit, object>> CommitsIndexToMethodAccessMap;
        public static readonly ISchemaColumn[] CommitColumns;

        public static readonly IDictionary<string, int> TagsNameToIndexMap;
        public static readonly IDictionary<int, Func<Tag, object>> TagsIndexToMethodAccessMap;
        public static readonly ISchemaColumn[] TagColumns;

        public static readonly IDictionary<string, int> BranchesNameToIndexMap;
        public static readonly IDictionary<int, Func<Branch, object>> BranchesIndexToMethodAccessMap;
        public static readonly ISchemaColumn[] BranchColumns;

        static SchemaGitHelper()
        {
            CommitsNameToIndexMap = new Dictionary<string, int>
            {
                {nameof(Commit.Id), 0},
                {nameof(Commit.Author), 1},
                {nameof(Commit.Committer), 2},
                {nameof(Commit.Encoding), 3},
                {nameof(Commit.Message), 4},
                {nameof(Commit.MessageShort), 5},
                {nameof(Commit.Sha), 6}
            };

            CommitsIndexToMethodAccessMap = new Dictionary<int, Func<Commit, object>>
            {
                {0, info => info.Id},
                {1, info => info.Author},
                {2, info => info.Committer},
                {3, info => info.Encoding},
                {4, info => info.Message},
                {5, info => info.MessageShort},
                {6, info => info.Sha}
            };

            CommitColumns = new ISchemaColumn[]
            {
                new SchemaColumn(nameof(Commit.Id), 0, typeof(ObjectId)),
                new SchemaColumn(nameof(Commit.Author), 1, typeof(Signature)),
                new SchemaColumn(nameof(Commit.Committer), 2, typeof(Signature)),
                new SchemaColumn(nameof(Commit.Encoding), 3, typeof(string)),
                new SchemaColumn(nameof(Commit.Message), 4, typeof(string)),
                new SchemaColumn(nameof(Commit.MessageShort), 5, typeof(string)),
                new SchemaColumn(nameof(Commit.Sha), 6, typeof(string))
            };


            TagsNameToIndexMap = new Dictionary<string, int>
            {
                {nameof(Tag.Annotation), 0},
                {nameof(Tag.CanonicalName), 1},
                {nameof(Tag.FriendlyName), 2},
                {nameof(Tag.IsAnnotated), 3},
                {nameof(Tag.PeeledTarget), 4},
                {nameof(Tag.Reference), 5},
                {nameof(Tag.Target), 6}
            };

            TagsIndexToMethodAccessMap = new Dictionary<int, Func<Tag, object>>
            {
                {0, info => info.Annotation},
                {1, info => info.CanonicalName},
                {2, info => info.FriendlyName},
                {3, info => info.IsAnnotated},
                {4, info => info.PeeledTarget},
                {5, info => info.Reference},
                {6, info => info.Target}
            };

            TagColumns = new ISchemaColumn[]
            {
                new SchemaColumn(nameof(Tag.Annotation), 0, typeof(TagAnnotation)),
                new SchemaColumn(nameof(Tag.CanonicalName), 1, typeof(string)),
                new SchemaColumn(nameof(Tag.FriendlyName), 2, typeof(string)),
                new SchemaColumn(nameof(Tag.IsAnnotated), 3, typeof(bool)),
                new SchemaColumn(nameof(Tag.PeeledTarget), 4, typeof(GitObject)),
                new SchemaColumn(nameof(Tag.Reference), 5, typeof(Reference)),
                new SchemaColumn(nameof(Tag.Target), 6, typeof(GitObject))
            };


            BranchesNameToIndexMap = new Dictionary<string, int>
            {
                {nameof(Branch.IsCurrentRepositoryHead), 0},
                {nameof(Branch.IsRemote), 1},
                {nameof(Branch.IsTracking), 2},
                {nameof(Branch.RemoteName), 3},
                {nameof(Branch.Tip), 4},
                {nameof(Branch.TrackingDetails), 5},
                {nameof(Branch.CanonicalName), 6},
                {nameof(Branch.FriendlyName), 7},
                {nameof(Branch.Reference), 8}
            };

            BranchesIndexToMethodAccessMap = new Dictionary<int, Func<Branch, object>>
            {
                {0, info => info.IsCurrentRepositoryHead},
                {1, info => info.IsRemote},
                {2, info => info.IsTracking},
                {3, info => info.RemoteName},
                {4, info => info.Tip},
                {5, info => info.TrackingDetails},
                {6, info => info.CanonicalName},
                {7, info => info.FriendlyName},
                {8, info => info.Reference}
            };

            BranchColumns = new ISchemaColumn[]
            {
                new SchemaColumn(nameof(Branch.IsCurrentRepositoryHead), 0, typeof(bool)),
                new SchemaColumn(nameof(Branch.IsRemote), 1, typeof(bool)),
                new SchemaColumn(nameof(Branch.IsTracking), 2, typeof(bool)),
                new SchemaColumn(nameof(Branch.RemoteName), 3, typeof(string)),
                new SchemaColumn(nameof(Branch.Tip), 4, typeof(Commit)),
                new SchemaColumn(nameof(Branch.TrackingDetails), 5, typeof(BranchTrackingDetails)),
                new SchemaColumn(nameof(Branch.CanonicalName), 6, typeof(string)),
                new SchemaColumn(nameof(Branch.FriendlyName), 7, typeof(string)),
                new SchemaColumn(nameof(Branch.Reference), 8, typeof(Reference))
            };
        }
    }
}