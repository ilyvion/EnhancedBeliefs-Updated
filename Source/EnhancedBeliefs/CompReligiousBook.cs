using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static Unity.Burst.Intrinsics.X86.Avx;

namespace EnhancedBeliefs
{
    [StaticConstructorOnStartup]
    public class CompReligiousBook : ThingComp
    {
        public CompProperties_ReligiousBook Props => props as CompProperties_ReligiousBook;
        public ReadingOutcomeDoer_CertaintyChange outcome;
        public Command_Action gizmo;
        public int lastRecacheTick = -1;
        public static readonly Texture2D burnBookGizmoTexture = ContentFinder<Texture2D>.Get("UI/Gizmos/Gizmo_BookBurning");
        public int lastDamageTick = -1;
        public Pawn lastDamageInsigator;

        public static readonly SimpleCurve CertaintyLossFromQualityCurve = new SimpleCurve
        {
            new CurvePoint(1, 0.05f),
            new CurvePoint(2, 0.06f),
            new CurvePoint(3, 0.07f),
            new CurvePoint(4, 0.08f),
            new CurvePoint(5, 0.09f),
            new CurvePoint(6, 0.10f)
        };

        public Ideo Ideo
        {
            get
            {
                if (outcome == null)
                {
                    CompBook comp = parent.GetComp<CompBook>();

                    for (int i = 0; i < comp.doers.Count; i++)
                    {
                        if (comp.doers[i] is ReadingOutcomeDoer_CertaintyChange change)
                        {
                            outcome = change;
                            break;
                        }
                    }
                }

                return outcome.ideo;
            }
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);

            // Could have some edge cases but its the only way to prevent pawns from being pissy when a trader leaves with a book
            if (mode == DestroyMode.WillReplace || (previousMap == null && lastDamageTick != Find.TickManager.TicksGame))
            {
                return;
            }

            if (lastDamageTick == Find.TickManager.TicksGame && lastDamageInsigator != null)
            {
                Find.HistoryEventsManager.RecordEvent(new HistoryEvent(EnhancedBeliefsDefOf.EB_BookDestroyed, lastDamageInsigator.Named(HistoryEventArgsNames.Doer)), canApplySelfTookThoughts: false);

                if (lastDamageInsigator.Ideo != null)
                {
                    List<Precept> preceptsListForReading = lastDamageInsigator.Ideo.PreceptsListForReading;
                    for (int i = 0; i < preceptsListForReading.Count; i++)
                    {
                        List<PreceptComp> comps = preceptsListForReading[i].def.comps;
                        for (int j = 0; j < comps.Count; j++)
                        {
                            if (comps[j] is PreceptComp_SelfTookMemoryThought preceptComp && (preceptComp.eventDef == EnhancedBeliefsDefOf.EB_DestroyedReligiousBook || preceptComp.eventDef == EnhancedBeliefsDefOf.EB_BookDestroyed))
                            {
                                lastDamageInsigator.needs.mood.thoughts.memories.TryGainMemory(preceptComp.thought, sourcePrecept: preceptsListForReading[i]);
                            }
                        }
                    }
                }
            }
            else
            {
                Find.HistoryEventsManager.RecordEvent(new HistoryEvent(EnhancedBeliefsDefOf.EB_BookDestroyed));
            }

            Find.LetterStack.ReceiveLetter("Religious book destroyed", "{0}, an important religious book for {1}, has been destroyed. Followers of {1} won't be happy about it.".Formatted(parent, Ideo), Find.FactionManager.OfPlayer.ideos.IsPrimary(Ideo) ? LetterDefOf.NegativeEvent : LetterDefOf.NeutralEvent, new LookTargets(new TargetInfo[] { new TargetInfo(parent.PositionHeld, parent.MapHeld) }));
            GameComponent_EnhancedBeliefs comp = Current.Game.GetComponent<GameComponent_EnhancedBeliefs>();
            if (!comp.ideoPawnsList.ContainsKey(Ideo))
            {
                return;
            }

            List<Pawn> pawns = comp.GetIdeoPawns(Ideo);

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];

                if (pawn.Map != previousMap)
                {
                    continue;
                }

                pawn.ideo.Certainty = Mathf.Clamp01(pawn.ideo.Certainty - CertaintyLossFromQualityCurve.Evaluate((int)parent.GetComp<CompQuality>().Quality));
                pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(EnhancedBeliefsDefOf.EB_ReligiousBookDestroyed);
                comp.pawnTrackerData[pawn].CheckConversion(excludeIdeos: new List<Ideo> { Ideo });
            }
        }

        public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostPostApplyDamage(dinfo, totalDamageDealt);
            lastDamageTick = Find.TickManager.TicksGame;
            lastDamageInsigator = dinfo.Instigator as Pawn;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }

            if (gizmo != null && Find.TickManager.TicksGame - lastRecacheTick < GenTicks.TickLongInterval)
            {
                if (ValidBurnerPawns().Count == 0)
                {
                    gizmo.disabled = true;
                    gizmo.disabledReason = "No colonists can destroy {0}.".Formatted(parent);
                }
                else
                {
                    gizmo.disabled = false;
                    gizmo.disabledReason = null;
                }

                yield return gizmo;
                yield break;
            }

            gizmo = new Command_Action();
            gizmo.defaultLabel = "Burn {0}".Formatted(parent);
            gizmo.defaultDesc = "Destroy {0} by burning it. This will upset {1} of {2}".Formatted(parent, Ideo.MemberNamePlural, Ideo);

            List<FloatMenuOption> options = new List<FloatMenuOption>();
            List<Pawn> burners = ValidBurnerPawns();

            if (burners.Count == 0)
            {
                gizmo.disabled = true;
                gizmo.disabledReason = "No colonists can destroy {0}.".Formatted(parent);
            }
            else
            {
                gizmo.disabled = false;
                gizmo.disabledReason = null;
            }

            for (int i = 0; i <  burners.Count; i++)
            {
                Pawn pawn = burners[i];
                options.Add(new FloatMenuOption(pawn.LabelShort, delegate ()
                {
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("Are you sure you want to make {0} burn {1}? Doing so will greatly upset all {2} of {3}.".Formatted(pawn, parent, Ideo.MemberNamePlural, Ideo), delegate ()
                    {
                        pawn.jobs.StartJob(JobMaker.MakeJob(EnhancedBeliefsDefOf.EB_BurnReligiousBook, parent));
                    }));
                }, iconThing: pawn, iconColor: Color.white));
            }

            gizmo.icon = burnBookGizmoTexture;
            gizmo.action = delegate ()
            {
                Find.WindowStack.Add(new FloatMenu(options));
            };

            yield return gizmo;
        }

        public List<Pawn> ValidBurnerPawns()
        {
            List<Pawn> pawns = new List<Pawn>();
            List<Pawn> allPawns = parent.Map.mapPawns.FreeAdultColonistsSpawned;

            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn pawn = allPawns[i];

                if (!pawn.IsColonistPlayerControlled)
                {
                    continue;
                }

                if (pawn.Ideo == Ideo)
                {
                    continue;
                }

                pawns.Add(pawn);
            }

            return pawns;
        }
    }

    public class CompProperties_ReligiousBook : CompProperties
    {
        public CompProperties_ReligiousBook()
        {
            compClass = typeof(CompReligiousBook);
        }
    }
}
