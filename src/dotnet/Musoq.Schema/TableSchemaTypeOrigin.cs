namespace Musoq.Schema;

/// <summary>Identifies who establishes a source table's column types.</summary>
public enum TableSchemaTypeOrigin
{
    /// <summary>The provider owns exact column types, including value-type nullability.</summary>
    ProviderDefined,

    /// <summary>The provider accepts types established by a query's TABLE declaration.</summary>
    TableDefined
}
