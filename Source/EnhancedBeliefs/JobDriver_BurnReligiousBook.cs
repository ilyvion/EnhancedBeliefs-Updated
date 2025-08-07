using Verse.AI;

namespace EnhancedBeliefs;

internal class JobDriver_BurnReligiousBook : JobDriver
{
    public Book Book => (Book)TargetThingA;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(Book, job, 1, -1, null, errorOnFailed, true);
    }

    public override IEnumerable<Toil> MakeNewToils()
    {
        _ = this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        _ = this.FailOn(delegate
        {
            return Book == null || Book.Destroyed;
        });
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
        yield return BurnBook();
    }

    public Toil BurnBook()
    {
        var toil = ToilMaker.MakeToil("BurnBook");
        toil.initAction = delegate
        {
            pawn.rotationTracker.FaceCell(Book.Position);
            rotateToFace = TargetIndex.A;
        };
        toil.tickAction = delegate
        {
            if (!Book.Destroyed)
            {
                _ = Book.TakeDamage(new DamageInfo(DamageDefOf.Burn, 0.2f, 100f, instigator: pawn));
            }
        };
        toil.AddFinishAction(delegate
        {
            var comp = Book.GetComp<CompBook>();
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
                    "{0} has destroyed {1}. This has greatly upset {2} of {3}."
                        .Formatted(pawn, Book, ideo.MemberNamePlural, ideo),
                    pawn,
                    Find.FactionManager.OfPlayer.ideos.PrimaryIdeo == ideo
                        ? MessageTypeDefOf.NegativeEvent
                        : MessageTypeDefOf.NeutralEvent);
            }
        });
        toil.AddEndCondition(delegate
        {
            return Book.Destroyed ? JobCondition.Succeeded : JobCondition.Ongoing;
        });
        toil.defaultCompleteMode = ToilCompleteMode.Never;
        toil.handlingFacing = true;
        _ = toil.WithProgressBar(TargetIndex.A, () => 1f - (Book.HitPoints / ((float)Book.MaxHitPoints)));
        //toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
        return toil;
    }
}
