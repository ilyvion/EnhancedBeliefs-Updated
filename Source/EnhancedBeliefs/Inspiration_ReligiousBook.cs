namespace EnhancedBeliefs;

internal class Inspiration_ReligiousBook : Inspiration
{
    public const float InitialWork = 45000f;

    public override void PostStart(bool sendLetter = true)
    {
        base.PostStart(sendLetter);

        if (pawn.Map == null || !pawn.Position.IsValid)
        {
            return;
        }

        var book = (UnfinishedThing)GenSpawn.Spawn(EnhancedBeliefsDefOf.EB_UnfinishedIdeobook, pawn.Position, pawn.Map);
        book.Creator = pawn;
        book.workLeft = InitialWork;
        End();
    }
}
