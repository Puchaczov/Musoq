namespace Musoq.Schema.Git
{
    public class BranchesTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } = SchemaGitHelper.BranchColumns;
    }
}