namespace EnhancedBeliefs;

public class Settings : ModSettings
{
    private bool _debugInteractionWorkers;
    public bool DebugInteractionWorkers
    {
        get => _debugInteractionWorkers;
        set => _debugInteractionWorkers = value;
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Values.Look(ref _debugInteractionWorkers, "debugInteractionWorkers", false);
    }

    public void DoSettingsWindowContents(Rect inRect)
    {
        Listing_Standard listingStandard = new();
        listingStandard.Begin(inRect);

        listingStandard.CheckboxLabeled(
            "EnhancedBeliefs.DebugInteractionWorkers".Translate(),
            ref _debugInteractionWorkers,
            "EnhancedBeliefs.DebugInteractionWorkers.Tip".Translate());

        listingStandard.End();
    }
}
