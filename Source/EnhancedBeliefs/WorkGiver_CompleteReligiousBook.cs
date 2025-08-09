using Verse.AI;

namespace EnhancedBeliefs;

[HotSwappable]
internal sealed class WorkGiver_CompleteReligiousBook : WorkGiver_Scanner
{
    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(EnhancedBeliefsDefOf.EB_UnfinishedIdeobook);
    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        return pawn.Map == null || !pawn.Position.IsValid || pawn.Ideo == null || base.ShouldSkip(pawn, forced);
    }

    public override Job? JobOnThing(Pawn pawn, Thing thing, bool forced = false)
    {
        if (pawn.Map == null || !pawn.Position.IsValid || pawn.Ideo == null || thing is not UnfinishedReligiousBook book)
        {
            return null;
        }

        var ideo = pawn.Ideo;

        var requiredRole = ideo.RolesListForReading.FirstOrDefault(role => role.def == PreceptDefOf.IdeoRole_Moralist);
        if (requiredRole == null)
        {
            JobFailReason.Is("EnhancedBeliefs.IdeologyMissingRole".Translate(PreceptDefOf.IdeoRole_Moralist.Named("ROLE")));
            return null;
        }

        var pawnIdeoRole = ideo.GetRole(pawn);
        if (pawnIdeoRole == null || pawnIdeoRole.def != PreceptDefOf.IdeoRole_Moralist)
        {
            JobFailReason.Is("EnhancedBeliefs.PawnMissingRequiredRole".Translate(pawn.Named("PAWN"), requiredRole.Label.Named("ROLE")));
            return null;
        }

        if (book.Creator != null && book.Creator != pawn)
        {
            JobFailReason.Is("EnhancedBeliefs.PawnIsNotAuthor".Translate(pawn.Named("PAWN")));
            return null;
        }

        if (book.ideo != pawn.Ideo && book.ideo != null)
        {
            JobFailReason.Is("EnhancedBeliefs.BookIsNotForPawnIdeoligion".Translate(pawn.Named("PAWN")));
            return null;
        }

        var lecterns = Find.CurrentMap.listerThings.ThingsOfDef(ThingDefOf.Lectern);
        lecterns.SortBy(thing => thing.Position.DistanceToSquared(pawn.Position));
        Thing? foundLectern = null;

        foreach (var lectern in lecterns)
        {
            if (pawn.CanReserveAndReach(lectern, PathEndMode.Touch, pawn.NormalMaxDanger()))
            {
                foundLectern = lectern;
                break;
            }
        }

        if (foundLectern == null)
        {
            JobFailReason.Is("EnhancedBeliefs.NoFreeValidLecternFound".Translate());
            return null;
        }

        var job = JobMaker.MakeJob(EnhancedBeliefsDefOf.EB_CompleteReligiousBook, book, foundLectern);
        job.count = 1;
        return job;
    }
}
