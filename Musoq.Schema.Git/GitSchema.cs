using System;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Schema.Git
{
    public class GitSchema : SchemaBase
    {
        private const string SchemaName = "git";

        public GitSchema()
            : base(SchemaName, CreateLibrary())
        {
        }

        public override ISchemaTable GetTableByName(string name, params object[] parameters)
        {
            switch (name.ToLowerInvariant())
            {
                case "commits":
                    return new CommitsTable();
                case "tags":
                    return new TagsTable();
                case "branches":
                    return new BranchesTable();
            }

            throw new NotSupportedException(name);
        }

        public override RowSource GetRowSource(string name, InterCommunicator interCommunicator, params object[] parameters)
        {
            switch (name)
            {
                case "commits":
                    return new CommitsSource((string)parameters[0], SchemaGitHelper.CommitsNameToIndexMap,
                        SchemaGitHelper.CommitsIndexToMethodAccessMap);
                case "tags":
                    return new TagsSource((string)parameters[0], SchemaGitHelper.TagsNameToIndexMap,
                        SchemaGitHelper.TagsIndexToMethodAccessMap);
                case "branches":
                    return new BranchesSource((string)parameters[0], SchemaGitHelper.BranchesNameToIndexMap,
                        SchemaGitHelper.BranchesIndexToMethodAccessMap);
            }

            throw new NotSupportedException(name);
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodsManager = new MethodsManager();
            var propertiesManager = new PropertiesManager();

            var library = new GitLibrary();

            methodsManager.RegisterLibraries(library);
            propertiesManager.RegisterProperties(library);

            return new MethodsAggregator(methodsManager, propertiesManager);
        }
    }
}