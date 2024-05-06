using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace EnhancedBeliefs
{
    public class WorkGiver_CompleteReligiousBook : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(EnhancedBeliefsDefOf.EB_UnfinishedIdeobook);
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            if (pawn.Map == null || pawn.Position == null || pawn.Ideo == null)
            {
                return true;
            }

            Precept_Role precept_Role = pawn.Ideo?.GetRole(pawn);

            if (precept_Role == null || precept_Role.def != PreceptDefOf.IdeoRole_Moralist)
            {
                return true;
            }

            return base.ShouldSkip(pawn, forced);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (pawn.Map == null || pawn.Position == null || pawn.Ideo == null)
            {
                return null;
            }

            Precept_Role precept_Role = pawn.Ideo?.GetRole(pawn);

            if (precept_Role == null || precept_Role.def != PreceptDefOf.IdeoRole_Moralist)
            {
                return null;
            }

            UnfinishedReligiousBook book = t as UnfinishedReligiousBook;

            if (book == null || (book.Creator != null && book.Creator != pawn) || (book.ideo != pawn.Ideo && book.ideo != null))
            {
                return null;
            }

            List<Thing> lecterns = Find.CurrentMap.listerThings.ThingsOfDef(ThingDefOf.Lectern);
            lecterns.SortBy((Thing thing) => thing.Position.DistanceToSquared(pawn.Position));
            Thing lectern = null;

            for (int i = 0; i < lecterns.Count; i++)
            {
                Thing thing = lecterns[i];
                if (pawn.CanReserveAndReach(thing, PathEndMode.Touch, pawn.NormalMaxDanger()))
                {
                    lectern = thing;
                    break;
                }
            }

            if (lectern == null)
            {
                return null;
            }

            Job job = JobMaker.MakeJob(EnhancedBeliefsDefOf.EB_CompleteReligiousBook, book, lectern);
            job.count = 1;
            return job;
        }
    }
}
