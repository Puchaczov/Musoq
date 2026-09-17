using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Examples.DataSources.StructuredInputs;

public sealed class StructuredInputsSchema : SchemaBase
{
    public const string SchemaName = "inputs";
    public const string Match = "match";
    public const string Configure = "configure";
    public const string Numbers = "numbers";
    public const string Matrix = "matrix";
    public const string Weighted = "weighted";
    public const string Overloaded = "overloaded";
    public const string DefaultConflict = "defaultconflict";
    public const string ContextProbe = "contextprobe";
    public const string Strict = "strict";
    public const string Mutable = "mutable";
    public const string Throwing = "throwing";
    public const string Ambiguous = "ambiguous";
    public const string CollectionColumn = "collectioncolumn";
    public const string RelationAmbiguity = "relationambiguity";

    public StructuredInputsSchema()
        : base(SchemaName, CreateLibrary())
    {
        AddTable<StructuredInputTable<PatternMatchRow>>(Match);
        AddTypedSource<MatchSource>(Match);
        AddTable<StructuredInputTable<ConfigureRow>>(Configure);
        AddTypedSource<ConfigureSource>(Configure);
        AddTable<StructuredInputTable<NumberRow>>(Numbers);
        AddTypedSource<NumbersSource>(Numbers);
        AddTable<StructuredInputTable<MatrixValueRow>>(Matrix);
        AddTypedSource<MatrixSource>(Matrix);
        AddTable<StructuredInputTable<WeightedRow>>(Weighted);
        AddTypedSource<WeightedSource>(Weighted);
        AddTable<StructuredInputTable<OverloadRow>>(Overloaded);
        AddTypedSource<OverloadSource>(Overloaded);
        AddTable<StructuredInputTable<DefaultConflictRow>>(DefaultConflict);
        AddTypedSource<DefaultConflictSource>(DefaultConflict);
        AddTable<StructuredInputTable<ContextProbeRow>>(ContextProbe);
        AddTypedSource<ContextProbeSource>(ContextProbe);
        AddTable<StructuredInputTable<StrictLimitRow>>(Strict);
        AddTypedSource<StrictLimitSource>(
            Strict,
            new TypedSourceRegistrationOptions(new StructuralInputLimits(8, 3, 1024)));
        AddTable<StructuredInputTable<MutableProbeRow>>(Mutable);
        AddTypedSource<MutableProbeSource>(Mutable);
        AddTable<StructuredInputTable<StrictLimitRow>>(Throwing);
        AddTypedSource<ThrowingSource>(Throwing);
        AddTable<StructuredInputTable<OverloadRow>>(Ambiguous);
        AddTypedSource<AmbiguousSource>(Ambiguous);
        AddTable<StructuredInputTable<CollectionColumnRow>>(CollectionColumn);
        AddTypedSource<CollectionColumnSource>(CollectionColumn);
        AddTable<StructuredInputTable<OverloadRow>>(RelationAmbiguity);
        AddTypedSource<RelationAmbiguitySource>(RelationAmbiguity);
    }

    /// <summary>
    /// Returns the stable row schema for a source. Structural source
    /// arguments select construction of the row source; they do not change
    /// the columns exposed by this maintained example, so descriptions can
    /// safely request table metadata with concrete arguments.
    /// </summary>
    public override ISchemaTable GetTableByName(
        string name,
        SourceMetadataContext metadataContext,
        params object?[] parameters)
    {
        ArgumentNullException.ThrowIfNull(metadataContext);
        metadataContext.EndWorkToken.ThrowIfCancellationRequested();

        return name.ToLowerInvariant() switch
        {
            Match => new StructuredInputTable<PatternMatchRow>(),
            Configure => new StructuredInputTable<ConfigureRow>(),
            Numbers => new StructuredInputTable<NumberRow>(),
            Matrix => new StructuredInputTable<MatrixValueRow>(),
            Weighted => new StructuredInputTable<WeightedRow>(),
            Overloaded => new StructuredInputTable<OverloadRow>(),
            DefaultConflict => new StructuredInputTable<DefaultConflictRow>(),
            ContextProbe => new StructuredInputTable<ContextProbeRow>(),
            Strict => new StructuredInputTable<StrictLimitRow>(),
            Mutable => new StructuredInputTable<MutableProbeRow>(),
            Throwing => new StructuredInputTable<StrictLimitRow>(),
            Ambiguous => new StructuredInputTable<OverloadRow>(),
            CollectionColumn => new StructuredInputTable<CollectionColumnRow>(),
            RelationAmbiguity => new StructuredInputTable<OverloadRow>(),
            _ => base.GetTableByName(name, metadataContext, parameters)
        };
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();
        methodsManager.RegisterLibraries(new LibraryBase());
        return new MethodsAggregator(methodsManager);
    }
}
