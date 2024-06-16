using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace EnhancedBeliefs
{
    public class ThoughtWorker_IdeologyOpinion : ThoughtWorker
    {
        public override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn otherPawn)
        {
            if (p.Ideo == null || otherPawn.Ideo == null || Find.World == null)
            {
                return false;
            }

            return true;
        }
    }
}
