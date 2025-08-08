using Verse.AI;

namespace EnhancedBeliefs;

internal sealed class MentalStateWorker_Iconoclast : MentalStateWorker
{
    private static readonly List<Thing> tmpThings = [];

    public override bool StateCanOccur(Pawn pawn)
    {
        if (!base.StateCanOccur(pawn))
        {
            return false;
        }
        tmpThings.Clear();
        TantrumMentalStateUtility.GetSmashableThingsNear(pawn, pawn.Position, tmpThings, t => t is BookIdeo);
        var result = tmpThings.Any();
        tmpThings.Clear();
        return result;
    }
}
