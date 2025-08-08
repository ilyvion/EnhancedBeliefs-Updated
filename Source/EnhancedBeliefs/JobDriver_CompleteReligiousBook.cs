using Verse.AI;

namespace EnhancedBeliefs;

internal sealed class JobDriver_CompleteReligiousBook : JobDriver
{
    public UnfinishedReligiousBook Book => (UnfinishedReligiousBook)TargetThingA;
    public Building Lectern => (Building)TargetThingB;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(Book, job, 1, -1, null, errorOnFailed) && pawn.Reserve(Lectern, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        _ = this.FailOnDestroyedOrNull(TargetIndex.A);
        _ = this.FailOnDestroyedOrNull(TargetIndex.B);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell).FailOnDestroyedNullOrForbidden(TargetIndex.A).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
        yield return Toils_Haul.StartCarryThing(TargetIndex.A, putRemainderInQueue: false, subtractNumTakenFromJobCount: false, failIfStackCountLessThanJobCount: false, reserve: true, canTakeFromInventory: true);
        yield return GotoLectern();
        yield return StandAtLectern();
        yield return WriteBook();
    }

    public Toil GotoLectern()
    {
        var toil = ToilMaker.MakeToil("GotoLectern");
        toil.initAction = delegate
        {
            var lecternSpot = Lectern.InteractionCell;

            if (!lecternSpot.IsValid || !lecternSpot.Standable(pawn.Map) || !pawn.Map.pawnDestinationReservationManager.CanReserve(lecternSpot, pawn))
            {
                foreach (var rotation in Rot4.AllRotations)
                {
                    lecternSpot = Lectern.Position - rotation.FacingCell;

                    if (lecternSpot.IsValid && lecternSpot.Standable(pawn.Map) && pawn.Map.pawnDestinationReservationManager.CanReserve(lecternSpot, pawn))
                    {
                        break;
                    }
                }
            }

            _ = pawn.ReserveSittableOrSpot(lecternSpot, toil.actor.CurJob);
            pawn.Map.pawnDestinationReservationManager.Reserve(pawn, pawn.CurJob, lecternSpot);
            pawn.pather.StartPath(lecternSpot, PathEndMode.OnCell);
        };
        toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
        return toil;
    }

    public Toil StandAtLectern()
    {
        var toil = ToilMaker.MakeToil("StandAtLectern");
        toil.initAction = delegate
        {
            pawn.rotationTracker.FaceCell(Lectern.Position);
            rotateToFace = TargetIndex.B;
        };
        toil.defaultCompleteMode = ToilCompleteMode.Instant;
        return toil;
    }

    public Toil WriteBook()
    {
        var toil = ToilMaker.MakeToil("WriteBook");

        toil.initAction = InitAction;
        toil.tickAction = TickAction;

        toil.AddFinishAction(FinishAction);
        toil.AddEndCondition(EndCondition);

        toil.AddFailCondition(() => !pawn.IsAdjacentToCardinalOrInside(Lectern));
        toil.handlingFacing = true;
        _ = toil.WithProgressBar(TargetIndex.B, () => 1f - (Book.workLeft / Inspiration_ReligiousBook.InitialWork));
        toil.defaultCompleteMode = ToilCompleteMode.Never;
        toil.activeSkill = () => SkillDefOf.Artistic;
        return toil;
    }

    private void InitAction()
    {
        if (Book.ideo == null)
        {
            Book.ideo = pawn.Ideo;
        }
        Book.isOpen = true;
    }

    private void TickAction()
    {
#if v1_5
            pawn.GainComfortFromCellIfPossible();
#else
        pawn.GainComfortFromCellIfPossible(1);
#endif

        if (!pawn.Downed)
        {
            pawn.Rotation = Lectern.Rotation;
        }

        Book.workLeft -= StatDefOf.GeneralLaborSpeed.Worker.IsDisabledFor(pawn) ? 1f : pawn.GetStatValue(StatDefOf.GeneralLaborSpeed);

        if (pawn.skills != null && !pawn.skills.GetSkill(SkillDefOf.Artistic).TotallyDisabled)
        {
            pawn.skills.Learn(SkillDefOf.Artistic, 0.1f);
        }
    }

    private void FinishAction()
    {
        if (Book != null)
        {
            Book.isOpen = false;
        }

        if (pawn.Downed)
        {
            _ = pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);
        }
    }

    private JobCondition EndCondition()
    {
        if (Book.workLeft <= 0f)
        {
            var newBook = (Book)ThingMaker.MakeThing(EnhancedBeliefsDefOf.EB_Ideobook);
            _ = GenPlace.TryPlaceThing(newBook, pawn.Position, pawn.Map, ThingPlaceMode.Near);
            newBook.TryGetComp<CompQuality>().SetQuality(QualityUtility.GenerateQualityCreatedByPawn(pawn, SkillDefOf.Artistic), ArtGenerationContext.Colony);
            newBook.StyleDef = Faction.OfPlayer.ideos.PrimaryIdeo.style.StyleForThingDef(newBook.def)?.styleDef;

            foreach (var doer in newBook.BookComp.Doers)
            {
                if (doer is ReadingOutcomeDoer_CertaintyChange changer)
                {
                    changer.ideo = Book.ideo;
                }
            }

            newBook.GenerateBook(pawn, GenTicks.TicksAbs);

            _ = Book.holdingOwner.TryDrop(Book, ThingPlaceMode.Direct, out _);
            Book.Destroy();

            return JobCondition.Succeeded;
        }

        return JobCondition.Ongoing;
    }
}
