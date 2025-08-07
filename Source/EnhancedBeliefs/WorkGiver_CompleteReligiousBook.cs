using Verse.AI;

namespace EnhancedBeliefs;

internal class WorkGiver_CompleteReligiousBook : WorkGiver_Scanner
{
    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(EnhancedBeliefsDefOf.EB_UnfinishedIdeobook);
    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        if (pawn.Map == null || pawn.Position.IsValid || pawn.Ideo == null)
        {
            return true;
        }

        var precept_Role = pawn.Ideo.GetRole(pawn);

        return precept_Role == null || precept_Role.def != PreceptDefOf.IdeoRole_Moralist || base.ShouldSkip(pawn, forced);
    }

    public override Job? JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (pawn.Map == null || pawn.Position.IsValid || pawn.Ideo == null)
        {
            return null;
        }

        var precept_Role = pawn.Ideo.GetRole(pawn);

        if (precept_Role == null || precept_Role.def != PreceptDefOf.IdeoRole_Moralist)
        {
            return null;
        }


        if (t is not UnfinishedReligiousBook book
            || (book.Creator != null && book.Creator != pawn)
            || (book.ideo != pawn.Ideo && book.ideo != null))
        {
            return null;
        }

        var lecterns = Find.CurrentMap.listerThings.ThingsOfDef(ThingDefOf.Lectern);
        lecterns.SortBy(thing => thing.Position.DistanceToSquared(pawn.Position));
        Thing? lectern = null;

        foreach (var thing in lecterns)
        {
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

        var job = JobMaker.MakeJob(EnhancedBeliefsDefOf.EB_CompleteReligiousBook, book, lectern);
        job.count = 1;
        return job;
    }
}
