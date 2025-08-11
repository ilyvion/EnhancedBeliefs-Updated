using Verse.AI;

namespace EnhancedBeliefs;

internal sealed class MentalStateWorker_Iconoclast : MentalStateWorker
{
    public override bool StateCanOccur(Pawn pawn)
    {
        return base.StateCanOccur(pawn) &&
#if !v1_5
            GenClosest.ClosestThing_Global_Reachable(
#else
            GenClosest.ClosestThing_Global_Reachable_NewTemp(
#endif
                pawn.Position,
                pawn.Map,
                pawn.Map.listerThings.AllThings,
                PathEndMode.Touch,
                TraverseParms.For(pawn),
                validator: t => t is BookIdeo,
                canLookInHaulableSources: true) != null;
    }
}
