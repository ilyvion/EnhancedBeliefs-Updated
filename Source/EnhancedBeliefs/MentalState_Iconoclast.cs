using Verse.AI;

namespace EnhancedBeliefs;

// TODO: Replace with custom mental state and related job that finds books and lights them on fire.
internal sealed class MentalState_Iconoclast : MentalState_Tantrum
{
    public int booksLeft = -1;
    private static readonly List<Thing> tmpThings = [];

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
        if (!target!.Spawned || !pawn.CanReach(target, PathEndMode.Touch, Danger.Deadly))
        {
            var thing = target;

            if (!TryFindNewTarget())
            {
                RecoverFromState();
                return;
            }

            Messages.Message("MessageTargetedTantrumChangedTarget".Translate(pawn.LabelShort, thing.Label, target.Label, pawn.Named("PAWN"), thing.Named("OLDTARGET"), target.Named("TARGET")).AdjustedFor(pawn), pawn, MessageTypeDefOf.NegativeEvent);
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
        Scribe_Values.Look(ref booksLeft, "booksBurned");
    }

    private bool TryFindNewTarget()
    {
        TantrumMentalStateUtility.GetSmashableThingsNear(pawn, pawn.Position, tmpThings, t => t is BookIdeo);
        var result = tmpThings.TryRandomElementByWeight(x => x.MarketValue * x.stackCount, out target);
        tmpThings.Clear();
        return result;
    }
}
