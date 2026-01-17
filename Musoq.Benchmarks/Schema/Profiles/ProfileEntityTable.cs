using Musoq.Schema;

namespace Musoq.Benchmarks.Schema.Profiles;

public class ProfileEntityTable : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } =
    [
        new SchemaColumn(nameof(ProfileEntity.FirstName), 11,
            typeof(ProfileEntity).GetProperty(nameof(ProfileEntity.FirstName))!.PropertyType),
        new SchemaColumn(nameof(ProfileEntity.LastName), 12,
            typeof(ProfileEntity).GetProperty(nameof(ProfileEntity.LastName))!.PropertyType),
        new SchemaColumn(nameof(ProfileEntity.Email), 13,
            typeof(ProfileEntity).GetProperty(nameof(ProfileEntity.Email))!.PropertyType),
        new SchemaColumn(nameof(ProfileEntity.Gender), 14,
            typeof(ProfileEntity).GetProperty(nameof(ProfileEntity.Gender))!.PropertyType),
        new SchemaColumn(nameof(ProfileEntity.IpAddress), 15,
            typeof(ProfileEntity).GetProperty(nameof(ProfileEntity.IpAddress))!.PropertyType),
        new SchemaColumn(nameof(ProfileEntity.Date), 16,
            typeof(ProfileEntity).GetProperty(nameof(ProfileEntity.Date))!.PropertyType),
        new SchemaColumn(nameof(ProfileEntity.Image), 17,
            typeof(ProfileEntity).GetProperty(nameof(ProfileEntity.Image))!.PropertyType),
        new SchemaColumn(nameof(ProfileEntity.Animal), 18,
            typeof(ProfileEntity).GetProperty(nameof(ProfileEntity.Animal))!.PropertyType),
        new SchemaColumn(nameof(ProfileEntity.Avatar), 19,
            typeof(ProfileEntity).GetProperty(nameof(ProfileEntity.Avatar))!.PropertyType)
    ];

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(col => col.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(col => col.ColumnName == name).ToArray();
    }

    public SchemaTableMetadata Metadata { get; } = new(typeof(ProfileEntity));
}