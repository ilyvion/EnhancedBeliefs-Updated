using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace EnhancedBeliefs
{
    public class InspirationWorker_ReligiousBook : InspirationWorker
    {
        public override bool InspirationCanOccur(Pawn pawn)
        {
            if (!base.InspirationCanOccur(pawn))
            {
                return false;
            }

            if (pawn == null || pawn.Map == null || pawn.Position == null || pawn.Ideo == null)
            {
                return false;
            }

            Precept_Role precept_Role = pawn.Ideo?.GetRole(pawn);

            if (precept_Role == null || precept_Role.def == PreceptDefOf.IdeoRole_Moralist)
            {
                return true;
            }

            return false;
        }

        public override float CommonalityFor(Pawn pawn)
        {
            if (pawn.Map == null || pawn.Position == null || pawn.Ideo == null)
            {
                return 0f;
            }

            Precept_Role precept_Role = pawn.Ideo?.GetRole(pawn);

            if (precept_Role == null || precept_Role.def != PreceptDefOf.IdeoRole_Moralist)
            {
                return 0f;
            }

            return 10f * Mathf.Sqrt(pawn.GetStatValue(StatDefOf.SocialIdeoSpreadFrequencyFactor));
        }
    }
}
