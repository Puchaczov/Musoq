namespace Musoq.Schema.Disk.Console
{
    internal class TextBasedTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } = SchemaConsoleHelper.Columns;
    }
}