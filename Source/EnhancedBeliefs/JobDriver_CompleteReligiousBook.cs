using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace EnhancedBeliefs
{
    public class JobDriver_CompleteReligiousBook : JobDriver
    {
        public UnfinishedReligiousBook book => TargetThingA as UnfinishedReligiousBook;
        public Building lectern => TargetThingB as Building;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(book, job, 1, -1, null, errorOnFailed) && pawn.Reserve(lectern, job, 1, -1, null, errorOnFailed);
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell).FailOnDestroyedNullOrForbidden(TargetIndex.A).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(TargetIndex.A, putRemainderInQueue: false, subtractNumTakenFromJobCount: false, failIfStackCountLessThanJobCount: false, reserve: true, canTakeFromInventory: true);
            yield return GotoLectern();
            yield return StandAtLectern();
            yield return WriteBook();
        }

        public Toil GotoLectern()
        {
            Toil toil = ToilMaker.MakeToil("GotoLectern");
            toil.initAction = delegate
            {
                IntVec3 lecternSpot = lectern.InteractionCell;

                if (!lecternSpot.IsValid || !lecternSpot.Standable(pawn.Map) || !pawn.Map.pawnDestinationReservationManager.CanReserve(lecternSpot, pawn))
                {
                    for (int i = 0; i < 4; i++)
                    {
                        lecternSpot = lectern.Position - Rot4.FromAngleFlat(90f * i).FacingCell;

                        if (lecternSpot.IsValid && lecternSpot.Standable(pawn.Map) && pawn.Map.pawnDestinationReservationManager.CanReserve(lecternSpot, pawn))
                        {
                            break;
                        }
                    }
                }

                pawn.ReserveSittableOrSpot(lecternSpot, toil.actor.CurJob);
                pawn.Map.pawnDestinationReservationManager.Reserve(pawn, pawn.CurJob, lecternSpot);
                pawn.pather.StartPath(lecternSpot, PathEndMode.OnCell);
            };
            toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            return toil;
        }

        public Toil StandAtLectern()
        {
            Toil toil = ToilMaker.MakeToil("StandAtLectern");
            toil.initAction = delegate
            {
                pawn.rotationTracker.FaceCell(lectern.Position);
                rotateToFace = TargetIndex.B;
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

        public Toil WriteBook()
        {
            Toil toil = ToilMaker.MakeToil("WriteBook");

            toil.initAction = delegate
            {
                if (book.ideo == null)
                {
                    book.ideo = pawn.Ideo;
                }

                book.isOpen = true;
            };

            toil.tickAction = delegate
            {
#if v1_5
                pawn.GainComfortFromCellIfPossible();
#else
                pawn.GainComfortFromCellIfPossible(1);
#endif

                if (!pawn.Downed)
                {
                    pawn.Rotation = lectern.Rotation;
                }

                book.workLeft -= StatDefOf.GeneralLaborSpeed.Worker.IsDisabledFor(pawn) ? 1f : pawn.GetStatValue(StatDefOf.GeneralLaborSpeed);

                if (pawn.skills != null && !pawn.skills.GetSkill(SkillDefOf.Artistic).TotallyDisabled)
                {
                    pawn.skills.Learn(SkillDefOf.Artistic, 0.1f);
                }
            };

            toil.AddFinishAction(delegate
            {
                if (book != null)
                {
                    book.isOpen = false;
                }

                if (pawn.Downed)
                {
                    pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out var _);
                }
            });

            toil.AddEndCondition(delegate
            {
                if (book.workLeft <= 0f)
                {
                    Book newBook = ThingMaker.MakeThing(EnhancedBeliefsDefOf.EB_Ideobook) as Book;
                    GenPlace.TryPlaceThing(newBook, pawn.Position, pawn.Map, ThingPlaceMode.Near);
                    newBook.TryGetComp<CompQuality>().SetQuality(QualityUtility.GenerateQualityCreatedByPawn(pawn, SkillDefOf.Artistic), ArtGenerationContext.Colony);
                    newBook.StyleDef = Faction.OfPlayer.ideos.PrimaryIdeo.style.StyleForThingDef(newBook.def)?.styleDef;

                    foreach (BookOutcomeDoer doer in newBook.BookComp.Doers)
                    {
                        if (doer is ReadingOutcomeDoer_CertaintyChange changer)
                        {
                            changer.ideo = book.ideo;
                        }
                    }

                    newBook.GenerateBook(pawn, GenTicks.TicksAbs);

                    book.holdingOwner.TryDrop(book, ThingPlaceMode.Direct, out var _);
                    book.Destroy();

                    return JobCondition.Succeeded;
                }

                return JobCondition.Ongoing;
            });

            toil.AddFailCondition(() => !pawn.IsAdjacentToCardinalOrInside(lectern));
            toil.handlingFacing = true;
            toil.WithProgressBar(TargetIndex.B, () => 1f - book.workLeft / 45000f);
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.activeSkill = () => SkillDefOf.Artistic;
            return toil;
        }
    }
}
