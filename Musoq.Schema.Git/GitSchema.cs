using System;
using System.Linq;
using System.Threading.Tasks;
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

        public override ISchemaTable GetTableByName(string name, string[] parameters)
        {
            switch (name.ToLowerInvariant())
            {
                case "commits":
                    return new CommitsTable();
                case "tags":
                    return new TagsTable();
            }

            throw new NotSupportedException(name);
        }

        public override RowSource GetRowSource(string name, string[] parameters)
        {
            switch (name)
            {
                case "commits":
                    return new CommitsSource(parameters[0], SchemaGitHelper.CommitsNameToIndexMap, SchemaGitHelper.CommitsIndexToMethodAccessMap);
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
