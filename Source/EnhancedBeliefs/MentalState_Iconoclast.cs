using Verse.AI;

namespace EnhancedBeliefs
{
    public class MentalState_Iconoclast : MentalState_Tantrum
    {
        public int booksLeft = -1;
        private static List<Thing> tmpThings = new List<Thing>();

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

                if (!TryFindNewTarget() || booksLeft == 0)
                {
                    RecoverFromState();
                    return;
                }

            }

            if (!target.Spawned || !pawn.CanReach(target, PathEndMode.Touch, Danger.Deadly))
            {
                Thing thing = target;

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
            TantrumMentalStateUtility.GetSmashableThingsNear(pawn, pawn.Position, tmpThings, (Thing t) => t is BookIdeo);
            bool result = tmpThings.TryRandomElementByWeight((Thing x) => x.MarketValue * (float)x.stackCount, out target);
            tmpThings.Clear();
            return result;
        }
    }
}
