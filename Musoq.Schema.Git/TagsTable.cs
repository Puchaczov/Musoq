namespace Musoq.Schema.Git
{
    public class TagsTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } = SchemaGitHelper.TagColumns;
    }
}