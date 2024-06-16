using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace EnhancedBeliefs
{
    public class Inspiration_ReligiousBook : Inspiration
    {
        public override void PostStart(bool sendLetter = true)
        {
            base.PostStart(sendLetter);

            if (pawn.Map == null || pawn.Position == null)
            {
                return;
            }

            UnfinishedThing book = GenSpawn.Spawn(EnhancedBeliefsDefOf.EB_UnfinishedIdeobook, pawn.Position, pawn.Map) as UnfinishedThing;
            book.Creator = pawn;
            book.workLeft = 45000f;
            End();
        }
    }
}
