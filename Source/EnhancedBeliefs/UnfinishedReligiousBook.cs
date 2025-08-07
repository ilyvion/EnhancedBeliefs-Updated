
namespace EnhancedBeliefs;

internal class UnfinishedReligiousBook : UnfinishedThing
{
    public Ideo? ideo;
    public bool isOpen = false;
    public UnfinishedBookExtension? extension;

    public UnfinishedBookExtension Extension
    {
        get
        {
            extension ??= def.GetModExtension<UnfinishedBookExtension>();
            return extension;
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();

        if (Scribe.mode == LoadSaveMode.Saving)
        {
            if (!Find.IdeoManager.IdeosListForReading.Contains(ideo))
            {
                ideo = null;
            }
        }

        Scribe_References.Look(ref ideo, "ideo");
        Scribe_Values.Look(ref isOpen, "isOpen", false);
    }

    public override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        if (!isOpen)
        {
            base.DrawAt(drawLoc, flip);
            return;
        }

        var rot = (ParentHolder is not Pawn_CarryTracker pawn_CarryTracker)
            ? Rotation
            : pawn_CarryTracker.pawn.Rotation;
        Extension.openGraphic?.Graphic.Draw(drawLoc, flip ? rot.Opposite : rot, this);
    }
}

internal class UnfinishedBookExtension : DefModExtension
{
#pragma warning disable CS0649 // Set by RimWorld, ensured by ConfigErrors
    public GraphicData? openGraphic;

#pragma warning restore CS0649

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors())
        {
            yield return error;
        }

        if (openGraphic == null)
        {
            yield return "Missing openGraphic for UnfinishedBookExtension.";
        }
    }
}
