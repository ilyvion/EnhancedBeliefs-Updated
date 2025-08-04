﻿using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

#if v1_5
using PlanetTile = int;
#endif

namespace EnhancedBeliefs
{
    public class StockGenerator_IdeoBook : StockGenerator_SingleDef
    {
        public override IEnumerable<Thing> GenerateThings(PlanetTile forTile, Faction faction = null)
        {
            foreach (Book book in StockGeneratorUtility.TryMakeForStock(thingDef, RandomCountOf(thingDef), faction))
            {
                foreach (BookOutcomeDoer doer in book.BookComp.Doers)
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
}
