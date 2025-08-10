using Verse.AI;

namespace EnhancedBeliefs;

internal sealed class JobDriver_PlaceAndBurnUntilDestroyed : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        var reserved = pawn.Reserve(TargetThingA, job, 1, -1, null, errorOnFailed, true);
        return reserved;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        _ = this.FailOn(delegate
        {
            return TargetThingA == null || TargetThingA.Destroyed;
        });
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch, true).FailOnSomeonePhysicallyInteracting(TargetIndex.A);

        yield return Toils_Haul.StartCarryThing(TargetIndex.A, canTakeFromInventory: true);
        yield return Toils_Goto.GotoCell(pawn.Position.RandomAdjacentCell8Way().RandomAdjacentCell8Way(), PathEndMode.OnCell);
        yield return Toils_General.Wait(90);
        yield return Toils_Haul.DropCarriedThing();
        yield return Toils_Reserve.ReserveDestinationOrThing(TargetIndex.A);
        yield return Toils_General.Wait(90);

        var tryIgniteAgain = TryIgniteAgain();
        yield return tryIgniteAgain;

        yield return Toils_Goto.GotoCell(pawn.Position.RandomAdjacentCell8Way().RandomAdjacentCell8Way(), PathEndMode.OnCell);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        var tryStartIgnite = ToilMaker.MakeToil("TryStartIgnite");
        tryStartIgnite.initAction = delegate
        {
            _ = pawn.natives.TryStartIgnite(TargetThingA);
        };
        yield return tryStartIgnite;

        yield return Toils_General.Wait(90);
        yield return Toils_Jump.JumpIf(tryIgniteAgain, () => !TargetThingA.IsBurning());
        yield return Toil_EnhancedBeliefs.BurnBook().FailOn(() => !TargetThingA.IsBurning());
        yield return Toils_Jump.JumpIf(tryIgniteAgain, () => !TargetThingA.IsBurning());
    }

    private static Toil TryIgniteAgain()
    {
        return Toils_General.Label();
    }
}

internal sealed class Toil_EnhancedBeliefs
{
    public static Toil BurnBook()
    {
        var toil = ToilMaker.MakeToil("BurnBook");
        toil.initAction = delegate
        {
            var actor = toil.actor;
            var target = actor.jobs.curJob.GetTarget(TargetIndex.A);

            actor.rotationTracker.FaceCell(target.Cell);
            actor.jobs.curDriver.rotateToFace = TargetIndex.A;
        };
        toil.tickAction = delegate
        {
            var actor = toil.actor;
            var book = (Thing)actor.jobs.curJob.GetTarget(TargetIndex.A);

            if (!book.Destroyed)
            {
                _ = book.TakeDamage(new DamageInfo(DamageDefOf.Burn, 0.2f, 100f, instigator: actor));
            }
        };
        toil.AddFinishAction(delegate
        {
            var actor = toil.actor;
            var book = (ThingWithComps)actor.jobs.curJob.GetTarget(TargetIndex.A);

            if (book == null || !book.Destroyed)
            {
                return;
            }

            var comp = book.GetComp<CompBook>();
            Ideo? ideo = null;

            foreach (var doer in comp.doers)
            {
                if (doer is ReadingOutcomeDoer_CertaintyChange change)
                {
                    ideo = change.ideo;
                    break;
                }
            }

            if (ideo != null)
            {
                Messages.Message(
                    "EnhancedBeliefs.BookBurningSuccess".Translate(
                        actor.Named("PAWN"), book.Named("BOOK"), ideo.Named("IDEO")),
                    actor,
                    Find.FactionManager.OfPlayer.ideos.PrimaryIdeo == ideo
                        ? MessageTypeDefOf.NegativeEvent
                        : MessageTypeDefOf.NeutralEvent);
            }
        });
        toil.AddEndCondition(delegate
        {
            var actor = toil.actor;
            var book = (Thing)actor.jobs.curJob.GetTarget(TargetIndex.A);
            return (book?.Destroyed ?? true) ? JobCondition.Succeeded : JobCondition.Ongoing;
        });
        toil.defaultCompleteMode = ToilCompleteMode.Never;
        toil.handlingFacing = true;
        _ = toil.WithProgressBar(TargetIndex.A, () =>
        {
            var actor = toil.actor;
            var book = (Thing)actor.jobs.curJob.GetTarget(TargetIndex.A);
            return 1f - (book.HitPoints / ((float)book.MaxHitPoints));
        });
        //toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
        return toil;
    }
}
