namespace Musoq.Schema.Git
{
    public class CommitsTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } = SchemaGitHelper.CommitColumns;
    }
}