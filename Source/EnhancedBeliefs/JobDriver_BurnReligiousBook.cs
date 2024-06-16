using Mono.Unix.Native;
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
    public class JobDriver_BurnReligiousBook : JobDriver
    {
        public Book book => TargetThingA as Book;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(book, job, 1, -1, null, errorOnFailed, true);
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(delegate
            {
                if (book == null || book.Destroyed)
                {
                    return true;
                }

                return false;
            });
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            yield return BurnBook();
        }

        public Toil BurnBook()
        {
            Toil toil = ToilMaker.MakeToil("BurnBook");
            toil.initAction = delegate
            {
                pawn.rotationTracker.FaceCell(book.Position);
                rotateToFace = TargetIndex.A;
            };
            toil.tickAction = delegate
            {
                if (!book.Destroyed)
                {
                    book.TakeDamage(new DamageInfo(DamageDefOf.Burn, 0.2f, 100f, instigator: pawn));
                }
            };
            toil.AddFinishAction(delegate
            {
                CompBook comp = book.GetComp<CompBook>();
                Ideo ideo = null;

                for (int i = 0; i < comp.doers.Count; i++)
                {
                    if (comp.doers[i] is ReadingOutcomeDoer_CertaintyChange change)
                    {
                        ideo = change.ideo;
                        break;
                    }
                }

                Messages.Message("{0} has destroyed {1}. This has greatly upset {2} of {3}.".Formatted(pawn, book, ideo.MemberNamePlural, ideo), pawn, Find.FactionManager.OfPlayer.ideos.PrimaryIdeo == ideo ? MessageTypeDefOf.NegativeEvent : MessageTypeDefOf.NeutralEvent);
            });
            toil.AddEndCondition(delegate
            {
                if (book.Destroyed)
                {
                    return JobCondition.Succeeded;
                }

                return JobCondition.Ongoing;
            });
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.handlingFacing = true;
            toil.WithProgressBar(TargetIndex.A, () => 1f - ((float)book.HitPoints) / ((float)book.MaxHitPoints));
            //toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
            return toil;
        }
    }
}
