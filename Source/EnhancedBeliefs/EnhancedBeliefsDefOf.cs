namespace EnhancedBeliefs;

[DefOf]
internal static class EnhancedBeliefsDefOf
{
#pragma warning disable CA2211, CS0649 // Ensured by DefOfAttribute
    public static MemeDef Supremacist;
    public static MemeDef Loyalist;
    public static MemeDef Guilty;
    public static MentalStateDef IdeoChange;
    public static ThingDef EB_UnfinishedIdeobook;
    public static ThingDef EB_Ideobook;
    public static JobDef EB_CompleteReligiousBook;
    public static JobDef EB_PlaceAndBurnUntilDestroyed;
    public static ThoughtDef EB_ReligiousBookDestroyed;
    public static HistoryEventDef EB_DestroyedReligiousBook;
    public static HistoryEventDef EB_BookDestroyed;
    public static EffecterDef EB_CompleteBook;
#pragma warning restore CA2211, CS0649

#pragma warning disable CS8618 // Set by RimWorld
    static EnhancedBeliefsDefOf()
#pragma warning restore CS8618
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(EnhancedBeliefsDefOf));
    }
}
