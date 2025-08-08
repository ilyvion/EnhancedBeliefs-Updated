#if v1_5
using PlanetTile = int;
#else
using RimWorld.Planet;
#endif

namespace EnhancedBeliefs;

internal sealed class StockGenerator_IdeoBook : StockGenerator_SingleDef
{
    public override IEnumerable<Thing> GenerateThings(PlanetTile forTile, Faction? faction = null)
    {
        foreach (var book in base.GenerateThings(forTile, faction).Cast<Book>())
        {
            foreach (var doer in book.BookComp.Doers)
            {
                if (doer is ReadingOutcomeDoer_CertaintyChange changer)
                {
                    if (faction != null && faction.ideos?.PrimaryIdeo != null)
                    {
                        changer.ideo = faction.ideos?.PrimaryIdeo;
                    }
                }
            }

            yield return book;
        }
    }
}
