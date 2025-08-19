namespace EnhancedBeliefs;

[HotSwappable]
internal sealed class InteractionWorker_IdeologicalDebatePrecept : InteractionWorker
{
    public IssueDef? topic;

    internal static readonly SimpleCurve CompatibilityFactorCurve =
    [
        new CurvePoint(-1.5f, 0.1f),
        new CurvePoint(-0.5f, 0.5f),
        new CurvePoint(0f, 1f),
        new CurvePoint(0.5f, 1.3f),
        new CurvePoint(1f, 1.8f),
        new CurvePoint(2f, 3f)
    ];

    public override float RandomSelectionWeight(Pawn initiator, Pawn recipient)
    {
        EnhancedBeliefsMod.DebugIf(EnhancedBeliefsMod.Settings.DebugInteractionWorkers, $"RandomSelectionWeight called: initiator={initiator}, recipient={recipient}");
        if (initiator.Inhumanized())
        {
            EnhancedBeliefsMod.DebugIf(EnhancedBeliefsMod.Settings.DebugInteractionWorkers, "Initiator is inhumanized. Returning 0.");
            return 0f;
        }
        if (!ModsConfig.IdeologyActive)
        {
            EnhancedBeliefsMod.DebugIf(EnhancedBeliefsMod.Settings.DebugInteractionWorkers, "Ideology not active. Returning 0.");
            return 0f;
        }
        if (Find.IdeoManager.classicMode)
        {
            EnhancedBeliefsMod.DebugIf(EnhancedBeliefsMod.Settings.DebugInteractionWorkers, "Classic mode enabled. Returning 0.");
            return 0f;
        }
        if (initiator.Ideo == null)
        {
            EnhancedBeliefsMod.DebugIf(EnhancedBeliefsMod.Settings.DebugInteractionWorkers, "Initiator has no ideo. Returning 0.");
            return 0f;
        }
        if (!recipient.RaceProps.Humanlike)
        {
            EnhancedBeliefsMod.DebugIf(EnhancedBeliefsMod.Settings.DebugInteractionWorkers, "Recipient not humanlike. Returning 0.");
            return 0f;
        }
        if (initiator.Ideo == recipient.Ideo)
        {
            EnhancedBeliefsMod.DebugIf(EnhancedBeliefsMod.Settings.DebugInteractionWorkers, "Initiator and recipient have same ideo. Returning 0.");
            return 0f;
        }
        if (recipient.DevelopmentalStage.Baby())
        {
            EnhancedBeliefsMod.DebugIf(EnhancedBeliefsMod.Settings.DebugInteractionWorkers, "Recipient is a baby. Returning 0.");
            return 0f;
        }
        if (initiator.skills.GetSkill(SkillDefOf.Social).TotallyDisabled)
        {
            EnhancedBeliefsMod.DebugIf(EnhancedBeliefsMod.Settings.DebugInteractionWorkers, "Initiator's social skill is totally disabled. Returning 0.");
            return 0f;
        }
        var spreadFactor = initiator.GetStatValue(StatDefOf.SocialIdeoSpreadFrequencyFactor);
        var compatibility = initiator.relations.CompatibilityWith(recipient);
        var curveEval = CompatibilityFactorCurve.Evaluate(compatibility);
        var result = 0.03f * spreadFactor * curveEval;
        EnhancedBeliefsMod.DebugIf(EnhancedBeliefsMod.Settings.DebugInteractionWorkers, $"Returning weight: {result} (spreadFactor={spreadFactor}, compatibility={compatibility}, curveEval={curveEval})");
        return result;
    }

    public override void Interacted(
        Pawn initiator,
        Pawn recipient,
        List<RulePackDef> extraSentencePacks,
        out string? letterText,
        out string? letterLabel,
        out LetterDef? letterDef,
        out LookTargets? lookTargets)
    {
        letterText = null;
        letterLabel = null;
        letterDef = null;
        lookTargets = null;

        EnhancedBeliefsMod.DebugIf(EnhancedBeliefsMod.Settings.DebugInteractionWorkers, $"Interacted called: initiator={initiator}, recipient={recipient}");

        var comp = Current.Game.GetComponent<GameComponent_EnhancedBeliefs>();
        var initiatorTracker = comp.PawnTracker.EnsurePawnHasIdeoTracker(initiator);
        var recipientTracker = comp.PawnTracker.EnsurePawnHasIdeoTracker(recipient);
        var initiatorIdeo = initiator.Ideo;
        var recipientIdeo = recipient.Ideo;

        topic = GetDebateTopic(initiatorIdeo, recipientIdeo, initiator, recipient, out var initiatorPrecept, out var recipientPrecept);
        EnhancedBeliefsMod.DebugIf(EnhancedBeliefsMod.Settings.DebugInteractionWorkers, $"Debate topic selected: {topic}");
        if (initiatorPrecept == null)
        {
            EnhancedBeliefsMod.DebugIf(EnhancedBeliefsMod.Settings.DebugInteractionWorkers, "No initiator precept found. Exiting.");
            return;
        }
        if (recipientPrecept == null)
        {
            EnhancedBeliefsMod.DebugIf(EnhancedBeliefsMod.Settings.DebugInteractionWorkers, "No recipient precept found. Exiting.");
            return;
        }
        if (initiatorPrecept == recipientPrecept)
        {
            EnhancedBeliefsMod.DebugIf(EnhancedBeliefsMod.Settings.DebugInteractionWorkers, "Initiator and recipient have the same precept. Exiting.");
            return;
        }
        EnhancedBeliefsMod.DebugIf(EnhancedBeliefsMod.Settings.DebugInteractionWorkers, $"Initiator's precept: {initiatorPrecept}, recipient's precept: {recipientPrecept}");

        var initiatorRoll = GetDebateRoll(initiator);
        var recipientRoll = GetDebateRoll(recipient);
        EnhancedBeliefsMod.DebugIf(EnhancedBeliefsMod.Settings.DebugInteractionWorkers, $"Debate rolls: initiator={initiatorRoll}, recipient={recipientRoll}");

        if (Math.Abs(initiatorRoll - recipientRoll) <= 0.1f)
        {
            EnhancedBeliefsMod.DebugIf(EnhancedBeliefsMod.Settings.DebugInteractionWorkers, "Debate is a draw. Calling HandleDraw.");
            if (HandleDraw(initiator, recipient, initiatorTracker, recipientTracker, initiatorPrecept, recipientPrecept, initiatorIdeo, recipientIdeo, extraSentencePacks, ref letterText, ref letterLabel, ref letterDef, ref lookTargets))
            {
                EnhancedBeliefsMod.DebugIf(EnhancedBeliefsMod.Settings.DebugInteractionWorkers, "HandleDraw returned true (social fight or conversion occurred). Exiting.");
                return;
            }
        }
        else
        {
            EnhancedBeliefsMod.DebugIf(EnhancedBeliefsMod.Settings.DebugInteractionWorkers, "Debate is not a draw. Adjusting opinions.");
            AdjustOpinions(initiator, recipient, comp, initiatorPrecept, recipientPrecept, initiatorRoll, recipientRoll);
        }
    }

    private static IssueDef? GetDebateTopic(Ideo initiatorIdeo, Ideo recipientIdeo, Pawn initiator, Pawn recipient, out PreceptDef? initiatorPrecept, out PreceptDef? recipientPrecept)
    {
        var retryAttempts = DefDatabase<IssueDef>.DefCount * 5;

        var initiatorIssues = initiatorIdeo.precepts.Select(p => p.def.issue).Distinct();
        var recipientIssues = recipientIdeo.precepts.Select(p => p.def.issue).Distinct();
        var conflictingIssues = initiatorIssues.Intersect(recipientIssues)
            .Select(issue => (
                issue,
                initiatorPrecept: GetPreceptForTopic(initiatorIdeo, issue, initiator),
                recipientPrecept: GetPreceptForTopic(recipientIdeo, issue, recipient)
            ))
            .Where(ip =>
                ip.initiatorPrecept != null &&
                ip.initiatorPrecept != ip.recipientPrecept);

        if (!conflictingIssues.Any())
        {
            EnhancedBeliefsMod.Warning("GetDebateTopic: No conflicting topics found. Exiting.");
            initiatorPrecept = null;
            recipientPrecept = null;
            return null;
        }

        (var selectedIssue, initiatorPrecept, recipientPrecept) = conflictingIssues.RandomElement();
        return selectedIssue;
    }

    private static PreceptDef? GetPreceptForTopic(Ideo ideo, IssueDef? topic, Pawn pawn)
    {
        var precept = ideo.precepts.Select(p => p.def).FirstOrDefault(d => d.issue == topic);
        if (precept == null)
        {
            EnhancedBeliefsMod.Error($"Could not find precept for {pawn} on topic {topic}. This should not happen.");
        }
        return precept;
    }

    private static float GetDebateRoll(Pawn pawn)
    {
        var rand = Rand.Value;
        var convPower = pawn.GetStatValue(StatDefOf.ConversionPower);
        var certaintyLoss = pawn.GetStatValue(StatDefOf.CertaintyLossFactor);
        var socialImpact = pawn.GetStatValue(StatDefOf.SocialImpact);
        var certainty = pawn.ideo.Certainty;
        var result = rand * convPower / certaintyLoss * socialImpact * (1f + ((certainty - 0.6f) * 0.5f));
        EnhancedBeliefsMod.DebugIf(EnhancedBeliefsMod.Settings.DebugInteractionWorkers, $"GetDebateRoll: pawn={pawn}, rand={rand}, convPower={convPower}, certaintyLoss={certaintyLoss}, socialImpact={socialImpact}, certainty={certainty}, result={result}");
        return result;
    }

    private bool HandleDraw(
        Pawn initiator,
        Pawn recipient,
        IdeoTrackerData initiatorTracker,
        IdeoTrackerData recipientTracker,
        PreceptDef initiatorPrecept,
        PreceptDef recipientPrecept,
        Ideo initiatorIdeo,
        Ideo recipientIdeo,
        List<RulePackDef> extraSentencePacks,
        ref string? letterText,
        ref string? letterLabel,
        ref LetterDef? letterDef,
        ref LookTargets? lookTargets)
    {
        // Fetch social fight multiplier
        interaction.socialFightBaseChance = 1f;
        var fightChanceModifier = initiator.interactions.SocialFightChance(interaction, recipient) + recipient.interactions.SocialFightChance(interaction, initiator);
        interaction.socialFightBaseChance = 0f;
        EnhancedBeliefsMod.DebugIf(EnhancedBeliefsMod.Settings.DebugInteractionWorkers, $"HandleDraw: fightChanceModifier={fightChanceModifier}");

        // Socially adept pawns are much less likely to start a brawl over an ideological debate
        var socialFightChance = 0.05f * fightChanceModifier /
            (0.5f + (initiator.skills.GetSkill(SkillDefOf.Social).Level * 0.1f)) /
            (0.5f + (recipient.skills.GetSkill(SkillDefOf.Social).Level * 0.1f));
        EnhancedBeliefsMod.DebugIf(EnhancedBeliefsMod.Settings.DebugInteractionWorkers, $"HandleDraw: socialFightChance={socialFightChance}");

        var randFight = Rand.Value;
        EnhancedBeliefsMod.DebugIf(EnhancedBeliefsMod.Settings.DebugInteractionWorkers, $"HandleDraw: randFight={randFight}");
        if (randFight < socialFightChance)
        {
            EnhancedBeliefsMod.DebugIf(EnhancedBeliefsMod.Settings.DebugInteractionWorkers, "Social fight triggered!");
            recipient.interactions.StartSocialFight(initiator, "EnhancedBeliefs.IdeologicalDebateOutcomeSocialFight");
            return true;
        }

        // Smarter pawns have a higher chance of arriving to a mutual conclusion that both of their ideoligions suck
        var randomOpinion = 0.2f * (0.75f + (initiator.skills.GetSkill(SkillDefOf.Intellectual).Level * 0.05f)) *
            (0.75f + (recipient.skills.GetSkill(SkillDefOf.Intellectual).Level * 0.05f)) /
            (0.2f + ((initiator.ideo.Certainty + recipient.ideo.Certainty) / 2f * 0.8f));
        EnhancedBeliefsMod.DebugIf(EnhancedBeliefsMod.Settings.DebugInteractionWorkers, $"HandleDraw: randomOpinion={randomOpinion}");

        var randOpinion = Rand.Value;
        EnhancedBeliefsMod.DebugIf(EnhancedBeliefsMod.Settings.DebugInteractionWorkers, $"HandleDraw: randOpinion={randOpinion}");
        if (randOpinion < randomOpinion)
        {
            EnhancedBeliefsMod.DebugIf(EnhancedBeliefsMod.Settings.DebugInteractionWorkers, "Mutual conclusion reached. Adjusting precept opinions and certainty.");
            var adjInit = -0.03f * initiator.GetStatValue(StatDefOf.CertaintyLossFactor) * (0.8f + (Rand.Value * 0.4f));
            var adjRecip = -0.03f * recipient.GetStatValue(StatDefOf.CertaintyLossFactor) * (0.8f + (Rand.Value * 0.4f));
            EnhancedBeliefsMod.DebugIf(EnhancedBeliefsMod.Settings.DebugInteractionWorkers, $"HandleDraw: Adjusting precept opinions: initiator={adjInit}, recipient={adjRecip}");
            initiatorTracker.AdjustPreceptOpinion(
                initiatorPrecept,
                adjInit);
            recipientTracker.AdjustPreceptOpinion(
                recipientPrecept,
                adjRecip);

            var newCertInit = Mathf.Clamp01(0.01f * initiator.GetStatValue(StatDefOf.CertaintyLossFactor) * (0.8f + (Rand.Value * 0.4f)));
            var newCertRecip = Mathf.Clamp01(0.01f * recipient.GetStatValue(StatDefOf.CertaintyLossFactor) * (0.8f + (Rand.Value * 0.4f)));
            EnhancedBeliefsMod.DebugIf(EnhancedBeliefsMod.Settings.DebugInteractionWorkers, $"HandleDraw: Setting new certainty: initiator={newCertInit}, recipient={newCertRecip}");
            initiator.ideo.Certainty = newCertInit;
            recipient.ideo.Certainty = newCertRecip;

            // Would be pretty funny if they both decide to change their beliefs at the same time
            EnhancedBeliefsMod.DebugIf(EnhancedBeliefsMod.Settings.DebugInteractionWorkers, "HandleDraw: Calling HandleConversion.");
            HandleConversion(initiator, recipient, initiatorTracker, recipientTracker, initiatorIdeo, recipientIdeo, extraSentencePacks, ref letterText, ref letterLabel, ref letterDef, ref lookTargets);
            return true;
        }
        EnhancedBeliefsMod.DebugIf(EnhancedBeliefsMod.Settings.DebugInteractionWorkers, "HandleDraw: No social fight or mutual conclusion. Returning false.");
        return false;
    }

    private static void HandleConversion(
        Pawn initiator,
        Pawn recipient,
        IdeoTrackerData initiatorTracker,
        IdeoTrackerData recipientTracker,
        Ideo initiatorIdeo,
        Ideo recipientIdeo,
        List<RulePackDef> extraSentencePacks,
        ref string? letterText,
        ref string? letterLabel,
        ref LetterDef? letterDef,
        ref LookTargets? lookTargets)
    {
        if (initiatorTracker.CheckConversion() == ConversionOutcome.Success)
        {
            EnhancedBeliefsMod.DebugIf(EnhancedBeliefsMod.Settings.DebugInteractionWorkers, "HandleConversion: Initiator conversion success.");
            if (PawnUtility.ShouldSendNotificationAbout(initiator) || PawnUtility.ShouldSendNotificationAbout(recipient))
            {
                letterLabel = "LetterLabelConvertIdeoAttempt_Success".Translate();
                letterText = "EnhancedBeliefs.LetterIdeologicalDebateConversionText".Translate(initiator.Named("CONVINCED"), recipient.Named("CONVINCER"), initiatorIdeo.Named("OLDIDEO"), initiator.Ideo.Named("NEWIDEO"));
                letterDef = LetterDefOf.NeutralEvent;
                lookTargets = new LookTargets(recipient, initiator);
                var role = initiatorIdeo.GetRole(initiator);
                if (role != null)
                {
                    letterText = letterText + "\n\n" + "LetterRoleLostLetterIdeoChangedPostfix".Translate(initiator.Named("PAWN"), role.Named("ROLE"), initiatorIdeo.Named("OLDIDEO")).Resolve();
                }
            }
            extraSentencePacks.Add(RulePackDefOf.Sentence_ConvertIdeoAttemptSuccess);
        }
        if (recipientTracker.CheckConversion() == ConversionOutcome.Success)
        {
            EnhancedBeliefsMod.DebugIf(EnhancedBeliefsMod.Settings.DebugInteractionWorkers, "HandleConversion: Recipient conversion success.");
            if (PawnUtility.ShouldSendNotificationAbout(initiator) || PawnUtility.ShouldSendNotificationAbout(recipient))
            {
                letterLabel = "LetterLabelConvertIdeoAttempt_Success".Translate();
                letterText = "EnhancedBeliefs.LetterIdeologicalDebateConversionText".Translate(recipient.Named("CONVINCED"), initiator.Named("CONVINCER"), recipientIdeo.Named("OLDIDEO"), recipient.Ideo.Named("NEWIDEO"));
                letterDef = LetterDefOf.NeutralEvent;
                lookTargets = new LookTargets(initiator, recipient);
                var role = recipientIdeo.GetRole(recipient);
                if (role != null)
                {
                    letterText = letterText + "\n\n" + "LetterRoleLostLetterIdeoChangedPostfix".Translate(recipient.Named("PAWN"), role.Named("ROLE"), recipientIdeo.Named("OLDIDEO")).Resolve();
                }
            }
            extraSentencePacks.Add(RulePackDefOf.Sentence_ConvertIdeoAttemptSuccess);
        }
    }

    private static void AdjustOpinions(Pawn initiator, Pawn recipient, GameComponent_EnhancedBeliefs comp, PreceptDef initiatorPrecept, PreceptDef recipientPrecept, float initiatorRoll, float recipientRoll)
    {
        Pawn winner, loser;
        PreceptDef? winnerPrecept, loserPrecept;
        if (initiatorRoll > recipientRoll)
        {
            winner = initiator;
            loser = recipient;
            winnerPrecept = initiatorPrecept;
            loserPrecept = recipientPrecept;
        }
        else
        {
            winner = recipient;
            loser = initiator;
            winnerPrecept = recipientPrecept;
            loserPrecept = initiatorPrecept;
        }
        EnhancedBeliefsMod.DebugIf(EnhancedBeliefsMod.Settings.DebugInteractionWorkers, $"AdjustOpinions: winner={winner}, loser={loser}, winnerPrecept={winnerPrecept}, loserPrecept={loserPrecept}");
        var loserTracker = comp.PawnTracker.EnsurePawnHasIdeoTracker(loser);
        var adjWin = 0.03f * winner.GetStatValue(StatDefOf.ConversionPower) * loser.GetStatValue(StatDefOf.CertaintyLossFactor);
        var adjLose = -0.03f * winner.GetStatValue(StatDefOf.ConversionPower) * loser.GetStatValue(StatDefOf.CertaintyLossFactor);

        EnhancedBeliefsMod.DebugIf(EnhancedBeliefsMod.Settings.DebugInteractionWorkers, $"AdjustOpinions: Adjusting precept opinions for loser: winnerPrecept={adjWin}, loserPrecept={adjLose}");
        loserTracker.AdjustPreceptOpinion(winnerPrecept, adjWin);
        loserTracker.AdjustPreceptOpinion(loserPrecept, adjLose);
    }
}
