using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;

namespace EnhancedBeliefs
{
    public class MentalStateWorker_Iconoclast : MentalStateWorker
    {
        private static List<Thing> tmpThings = new List<Thing>();

        public override bool StateCanOccur(Pawn pawn)
        {
            if (!base.StateCanOccur(pawn))
            {
                return false;
            }
            tmpThings.Clear();
            TantrumMentalStateUtility.GetSmashableThingsNear(pawn, pawn.Position, tmpThings, (Thing t) => t is BookIdeo);
            bool result = tmpThings.Any();
            tmpThings.Clear();
            return result;
        }
    }

}
