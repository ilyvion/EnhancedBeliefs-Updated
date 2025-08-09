namespace EnhancedBeliefs;

[StaticConstructorOnStartup]
[HotSwappable]
internal sealed class CompReligiousBook : ThingComp
{
    public CompProperties_ReligiousBook Props => (CompProperties_ReligiousBook)props;
    private ReadingOutcomeDoer_CertaintyChange? outcome;
    private Command_Action? gizmo;
    private int lastRecacheTick = -1;
    private static readonly Texture2D burnBookGizmoTexture = ContentFinder<Texture2D>.Get("UI/Gizmos/Gizmo_BookBurning");
    private int lastDamageTick = -1;
    private Pawn? lastDamageInsigator;

    internal static readonly SimpleCurve CertaintyLossFromQualityCurve =
    [
        new CurvePoint(1, 0.05f),
        new CurvePoint(2, 0.06f),
        new CurvePoint(3, 0.07f),
        new CurvePoint(4, 0.08f),
        new CurvePoint(5, 0.09f),
        new CurvePoint(6, 0.10f)
    ];

    public Ideo? Ideo
    {
        get
        {
            if (outcome == null)
            {
                var comp = parent.GetComp<CompBook>();

                foreach (var doer in comp.doers)
                {
                    if (doer is ReadingOutcomeDoer_CertaintyChange change)
                    {
                        outcome = change;
                        break;
                    }
                }
            }

            return outcome?.ideo;
        }
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
        base.PostDestroy(mode, previousMap);

        if (ShouldSkipPostDestroy(mode, previousMap))
        {
            return;
        }

        var currentTick = Find.TickManager.TicksGame;
        var instigator = lastDamageTick == currentTick ? lastDamageInsigator : null;

        // Record history event(s) and instigator thoughts when applicable
        RecordDestructionHistoryEvents(instigator);
        if (instigator != null)
        {
            GrantInstigatorThoughts(instigator);
        }

        // Notify player
        SendDestructionLetter();

        // Affect followers in the same map
        ApplyImpactToFollowers(previousMap);
    }

    private bool ShouldSkipPostDestroy(DestroyMode mode, Map previousMap)
    {
        // Could have some edge cases but it's the only way to prevent pawns from being upset when a trader leaves with a book
        return mode == DestroyMode.WillReplace || (previousMap == null && lastDamageTick != Find.TickManager.TicksGame);
    }

    private static void RecordEventWithOptionalDoer(Pawn? instigator)
    {
        if (instigator != null)
        {
            Find.HistoryEventsManager.RecordEvent(new HistoryEvent(EnhancedBeliefsDefOf.EB_BookDestroyed, instigator.Named(HistoryEventArgsNames.Doer)), canApplySelfTookThoughts: false);
        }
        else
        {
            Find.HistoryEventsManager.RecordEvent(new HistoryEvent(EnhancedBeliefsDefOf.EB_BookDestroyed));
        }
    }

    private static void RecordDestructionHistoryEvents(Pawn? instigator)
    {
        RecordEventWithOptionalDoer(instigator);
    }

    private static void GrantInstigatorThoughts(Pawn instigator)
    {
        if (instigator.Ideo == null)
        {
            return;
        }

        var preceptsListForReading = instigator.Ideo.PreceptsListForReading;
        foreach (var precept in preceptsListForReading)
        {
            var comps = precept.def.comps;
            foreach (var pComp in comps)
            {
                if (pComp is PreceptComp_SelfTookMemoryThought preceptComp &&
                    (preceptComp.eventDef == EnhancedBeliefsDefOf.EB_DestroyedReligiousBook || preceptComp.eventDef == EnhancedBeliefsDefOf.EB_BookDestroyed))
                {
                    instigator.needs.mood.thoughts.memories.TryGainMemory(preceptComp.thought, sourcePrecept: precept);
                }
            }
        }
    }

    private void SendDestructionLetter()
    {
        Find.LetterStack.ReceiveLetter(
            "EnhancedBeliefs.LetterReligiousBookDestroyedLabel".Translate(),
            "EnhancedBeliefs.LetterReligiousBookDestroyedText".Translate(parent.Named("BOOK"), Ideo.Named("IDEO")),
            Find.FactionManager.OfPlayer.ideos.IsPrimary(Ideo) ? LetterDefOf.NegativeEvent : LetterDefOf.NeutralEvent,
            new LookTargets(new TargetInfo[] { new(parent.PositionHeld, parent.MapHeld) }));
    }

    private void ApplyImpactToFollowers(Map previousMap)
    {
        var comp = Current.Game.GetComponent<GameComponent_EnhancedBeliefs>();
        if (Ideo is null || !comp.IdeoTracker.ContainsIdeo(Ideo))
        {
            return;
        }

        var pawns = comp.GetIdeoPawns(Ideo);
        var quality = (int)parent.GetComp<CompQuality>().Quality;

        foreach (var pawn in pawns)
        {
            if (pawn.Map != previousMap)
            {
                continue;
            }

            pawn.ideo.Certainty = Mathf.Clamp01(pawn.ideo.Certainty - CertaintyLossFromQualityCurve.Evaluate(quality));
            pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(EnhancedBeliefsDefOf.EB_ReligiousBookDestroyed);
            _ = comp.PawnTracker.EnsurePawnHasIdeoTracker(pawn).CheckConversion(excludeIdeos: [Ideo]);
        }
    }

    public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
    {
        base.PostPostApplyDamage(dinfo, totalDamageDealt);
        lastDamageTick = Find.TickManager.TicksGame;
        lastDamageInsigator = dinfo.Instigator != null
            ? dinfo.Instigator as Pawn
            : null;
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (var gizmo in base.CompGetGizmosExtra())
        {
            yield return gizmo;
        }

        // Try to reuse a cached gizmo and update its enabled state
        var cached = GetCachedGizmoIfValid();
        if (cached != null)
        {
            yield return cached;
            yield break;
        }

        // Create fresh gizmo and wire it up
        gizmo = CreateBurnGizmo();

        var burners = ValidBurnerPawns();
        UpdateGizmoDisabled(gizmo, burners, parent);

        var options = BuildBurnMenuOptions(burners);
        WireGizmoAction(gizmo, options);

        lastRecacheTick = Find.TickManager.TicksGame;
        yield return gizmo;
    }

    private Command_Action? GetCachedGizmoIfValid()
    {
        if (gizmo == null)
        {
            return null;
        }

        if (Find.TickManager.TicksGame - lastRecacheTick >= GenTicks.TickLongInterval)
        {
            return null;
        }

        UpdateGizmoDisabled(gizmo, ValidBurnerPawns(), parent);
        return gizmo;
    }

    private Command_Action CreateBurnGizmo()
    {
        return new Command_Action
        {
            defaultLabel = "Burn {0}".Formatted(parent),
            defaultDesc = Ideo != null
                ? "Destroy {0} by burning it. This will upset {1} of {2}".Formatted(parent, Ideo.MemberNamePlural, Ideo)
                : "Destroy {0} by burning it.".Formatted(parent),
            icon = burnBookGizmoTexture
        };
    }

    private static void UpdateGizmoDisabled(Command_Action gizmo, List<Pawn> burners, Thing thing)
    {
        if (burners.Count == 0)
        {
            gizmo.Disabled = true;
            gizmo.disabledReason = "No colonists can destroy {0}.".Formatted(thing);
        }
        else
        {
            gizmo.Disabled = false;
            gizmo.disabledReason = null;
        }
    }

    private List<FloatMenuOption> BuildBurnMenuOptions(List<Pawn> burners)
    {
        List<FloatMenuOption> options = [];
        foreach (var pawn in burners)
        {
            options.Add(new FloatMenuOption(pawn.LabelShort, delegate ()
            {
                var text = Ideo != null
                    ? "Are you sure you want to make {0} burn {1}? Doing so will greatly upset all {2} of {3}.".Formatted(pawn, parent, Ideo.MemberNamePlural, Ideo)
                    : "Are you sure you want to make {0} burn {1}?".Formatted(pawn, parent);

                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(text, delegate ()
                {
                    pawn.jobs.StartJob(JobMaker.MakeJob(EnhancedBeliefsDefOf.EB_BurnReligiousBook, parent));
                }));
            }, iconThing: pawn, iconColor: Color.white));
        }

        return options;
    }

    private static void WireGizmoAction(Command_Action gizmo, List<FloatMenuOption> options)
    {
        gizmo.action = delegate ()
        {
            Find.WindowStack.Add(new FloatMenu(options));
        };
    }

    public List<Pawn> ValidBurnerPawns()
    {
        List<Pawn> pawns = [];
        var allPawns = parent.MapHeld.mapPawns.FreeAdultColonistsSpawned;

        foreach (var pawn in allPawns)
        {
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

internal sealed class CompProperties_ReligiousBook : CompProperties
{
    public CompProperties_ReligiousBook()
    {
        compClass = typeof(CompReligiousBook);
    }

    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (var error in base.ConfigErrors(parentDef))
        {
            yield return error;
        }

        if (parentDef.comps.SingleOrDefault(c => c is CompProperties_Book) is not CompProperties_Book bookComp)
        {
            yield return "CompProperties_ReligiousBook must be also have a CompProperties_Book.";
            yield break;
        }

        if (bookComp.doers.SingleOrDefault(d => d is BookOutcomeProperties_CertaintyChange) is not BookOutcomeProperties_CertaintyChange bookOutcomeCertaintyChange)
        {
            yield return "CompProperties_ReligiousBook must have a BookOutcomeProperties_CertaintyChange in its doers.";
        }
    }
}
