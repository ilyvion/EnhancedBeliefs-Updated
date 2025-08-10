using Verse.AI;

namespace EnhancedBeliefs;

internal sealed class JobGiver_Iconoclast : ThinkNode_JobGiver
{
    private IntRange waitTicks = new(80, 140);

    private const float FireStartChance = 0.75f;

    public override ThinkNode DeepCopy(bool resolve = true)
    {
        var obj = (JobGiver_Iconoclast)base.DeepCopy(resolve);
        obj.waitTicks = waitTicks;
        return obj;
    }

    protected override Job? TryGiveJob(Pawn pawn)
    {
        if (pawn.MentalState is not MentalState_Iconoclast { target: not null } mentalState)
        {
            return null;
        }

        if (!pawn.CanReach(mentalState.target, PathEndMode.Touch, Danger.Deadly))
        {
            var job = JobMaker.MakeJob(JobDefOf.Wait_Wander);
            job.expiryInterval = waitTicks.RandomInRange;
            pawn.mindState.nextMoveOrderIsWait = false;
            return job;
        }

        if (Rand.Value < FireStartChance)
        {
            if (mentalState.target != null)
            {
                var job = JobMaker.MakeJob(EnhancedBeliefsDefOf.EB_PlaceAndBurnUntilDestroyed, mentalState.target);
                job.count = 1;
                return job;
            }
        }
        var intVec = RCellFinder.RandomWanderDestFor(pawn, pawn.Position, 10f, null, Danger.Deadly);
        if (intVec.IsValid)
        {
            pawn.mindState.nextMoveOrderIsWait = true;
            return JobMaker.MakeJob(JobDefOf.GotoWander, intVec);
        }

        return null;
    }
}
