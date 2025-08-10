using Verse.AI;

namespace EnhancedBeliefs;

internal sealed class MentalState_Iconoclast : MentalState
{
    public BookIdeo? target;
    public int booksLeft = -1;

#if v1_5
    public override void MentalStateTick()
#else
    public override void MentalStateTick(int delta)
#endif
    {
        if (booksLeft <= 0)
        {
            RecoverFromState();
            return;
        }

        if (target == null || target.Destroyed)
        {
            booksLeft -= 1;

            if (booksLeft == 0 || !TryFindNewTarget())
            {
                RecoverFromState();
                return;
            }

        }

        // target is not null here because we checked it above; if target is null or destroyed and
        // TryFindNewTarget returns false, we already returned.
        if (!pawn.CanReach(target, PathEndMode.Touch, Danger.Deadly))
        {
            var thing = target!;

            if (!TryFindNewTarget())
            {
                RecoverFromState();
                return;
            }

            Messages.Message("MessageTargetedTantrumChangedTarget".Translate(pawn.LabelShort, thing.Label, target!.Label, pawn.Named("PAWN"), thing.Named("OLDTARGET"), target.Named("TARGET")).AdjustedFor(pawn), pawn, MessageTypeDefOf.NegativeEvent);
        }

#if v1_5
        base.MentalStateTick();
#else
        base.MentalStateTick(delta);
#endif
    }

    public override void PostStart(string reason)
    {
        base.PostStart(reason);
        booksLeft = Rand.RangeInclusive(2, 4);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            // Attempt to load old value label first.
            Scribe_Values.Look(ref booksLeft, "booksBurned");
            if (booksLeft == 0)
            {
                Scribe_Values.Look(ref booksLeft, "booksLeft");
            }
        }
        else
        {
            Scribe_Values.Look(ref booksLeft, "booksLeft");
        }
    }

    public override RandomSocialMode SocialModeMax()
    {
        return RandomSocialMode.Off;
    }

    private bool TryFindNewTarget()
    {
#if !v1_5
        target = GenClosest.ClosestThing_Global_Reachable(
#else
        target = GenClosest.ClosestThing_Global_Reachable_NewTemp(
#endif
                pawn.Position,
                pawn.Map,
                pawn.Map.listerThings.AllThings,
                PathEndMode.Touch,
                TraverseParms.For(pawn),
                validator: t => t is BookIdeo,
                canLookInHaulableSources: true) as BookIdeo;

        return target != null;
    }
}
