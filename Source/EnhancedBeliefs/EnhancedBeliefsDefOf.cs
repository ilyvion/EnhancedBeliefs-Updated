namespace EnhancedBeliefs;

[DefOf]
internal static class EnhancedBeliefsDefOf
{
#pragma warning disable CA2211, CS0649 // Ensured by DefOfAttribute
    internal static MemeDef Supremacist;
    internal static MemeDef Loyalist;
    internal static MemeDef Guilty;
    internal static MentalStateDef IdeoChange;
    internal static ThingDef EB_UnfinishedIdeobook;
    internal static ThingDef EB_Ideobook;
    internal static JobDef EB_CompleteReligiousBook;
    internal static JobDef EB_BurnReligiousBook;
    internal static ThoughtDef EB_ReligiousBookDestroyed;
    internal static HistoryEventDef EB_DestroyedReligiousBook;
    internal static HistoryEventDef EB_BookDestroyed;
#pragma warning restore CA2211, CS0649

#pragma warning disable CS8618 // Set by RimWorld
    static EnhancedBeliefsDefOf()
#pragma warning restore CS8618
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(EnhancedBeliefsDefOf));
    }
}
