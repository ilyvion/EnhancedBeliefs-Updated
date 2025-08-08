namespace EnhancedBeliefs;

internal class ThoughtWorker_IdeologyOpinion : ThoughtWorker
{
    protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn otherPawn)
    {
        return p.Ideo != null
            && otherPawn.Ideo != null
            && Find.World != null;
    }
}
