namespace EnhancedBeliefs;

internal sealed class InteractionWorker_IdeologicalDebateMeme : InteractionWorker
{
    public MemeDef? topic;

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
        return initiator.Inhumanized()
            || !ModsConfig.IdeologyActive
            || Find.IdeoManager.classicMode
            || initiator.Ideo == null
            || !recipient.RaceProps.Humanlike
            || initiator.Ideo == recipient.Ideo
            || recipient.DevelopmentalStage.Baby()
            || initiator.skills.GetSkill(SkillDefOf.Social).TotallyDisabled
            ? 0f
            : 0.03f * initiator.GetStatValue(StatDefOf.SocialIdeoSpreadFrequencyFactor) * CompatibilityFactorCurve.Evaluate(initiator.relations.CompatibilityWith(recipient));
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

        var comp = Current.Game.GetComponent<GameComponent_EnhancedBeliefs>();
        var initiatorTracker = comp.PawnTracker.EnsurePawnHasIdeoTracker(initiator);
        var recipientTracker = comp.PawnTracker.EnsurePawnHasIdeoTracker(recipient);
        var initiatorIdeo = initiator.Ideo;
        var recipientIdeo = recipient.Ideo;

        topic = initiatorIdeo.memes.Union(recipientIdeo.memes).RandomElement();

        var initiatorRoll = GetDebateRoll(initiator);
        var recipientRoll = GetDebateRoll(recipient);

        if (Math.Abs(initiatorRoll - recipientRoll) <= 0.1f)
        {
            if (HandleDraw(initiator, recipient, initiatorTracker, recipientTracker, topic, initiatorIdeo, recipientIdeo, extraSentencePacks, ref letterText, ref letterLabel, ref letterDef, ref lookTargets))
            {
                return;
            }
        }
        else
        {
            AdjustOpinions(initiator, recipient, comp, topic, initiatorRoll, recipientRoll);
        }
    }

    private static float GetDebateRoll(Pawn pawn)
    {
        return Rand.Value * pawn.GetStatValue(StatDefOf.ConversionPower) /
               pawn.GetStatValue(StatDefOf.CertaintyLossFactor) *
               pawn.GetStatValue(StatDefOf.SocialImpact) *
               (1f + ((pawn.ideo.Certainty - 0.6f) * 0.5f));
    }


    private bool HandleDraw(
        Pawn initiator,
        Pawn recipient,
        IdeoTrackerData initiatorTracker,
        IdeoTrackerData recipientTracker,
        MemeDef topic,
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

        // Socially adept pawns are much less likely to start a brawl over an ideological debate
        var socialFightChance = 0.05f * fightChanceModifier /
            (0.5f + (initiator.skills.GetSkill(SkillDefOf.Social).Level * 0.1f)) /
            (0.5f + (recipient.skills.GetSkill(SkillDefOf.Social).Level * 0.1f));

        if (Rand.Value < socialFightChance)
        {
            recipient.interactions.StartSocialFight(initiator, "EnhancedBeliefs.IdeologicalDebateOutcomeSocialFight");
            return true;
        }

        // Smarter pawns have a higher chance of arriving to a mutual conclusion that both of their ideoligions suck
        var randomOpinion = 0.2f * (0.75f + (initiator.skills.GetSkill(SkillDefOf.Intellectual).Level * 0.05f)) *
            (0.75f + (recipient.skills.GetSkill(SkillDefOf.Intellectual).Level * 0.05f)) /
            (0.2f + ((initiator.ideo.Certainty + recipient.ideo.Certainty) / 2f * 0.8f));

        if (Rand.Value < randomOpinion)
        {
            var initiatorLossFactor = initiator.GetStatValue(StatDefOf.CertaintyLossFactor);
            var recipientLossFactor = recipient.GetStatValue(StatDefOf.CertaintyLossFactor);

            if (!topic.agreeableTraits.NullOrEmpty())
            {
                initiatorLossFactor *= ApplyTraitLossFactor(topic.agreeableTraits, initiator, initiator.Ideo, topic, agreeable: true);
                recipientLossFactor *= ApplyTraitLossFactor(topic.agreeableTraits, recipient, recipient.Ideo, topic, agreeable: true);
            }

            if (!topic.disagreeableTraits.NullOrEmpty())
            {
                initiatorLossFactor *= ApplyTraitLossFactor(topic.disagreeableTraits, initiator, initiator.Ideo, topic, agreeable: false);
                recipientLossFactor *= ApplyTraitLossFactor(topic.disagreeableTraits, recipient, recipient.Ideo, topic, agreeable: false);
            }

            initiatorTracker.AdjustMemeOpinion(topic, -0.03f * initiatorLossFactor * (0.8f + (Rand.Value * 0.4f)));
            recipientTracker.AdjustMemeOpinion(topic, -0.03f * recipientLossFactor * (0.8f + (Rand.Value * 0.4f)));

            initiator.ideo.Certainty = Mathf.Clamp01(0.01f * initiatorLossFactor * (0.8f + (Rand.Value * 0.4f)));
            recipient.ideo.Certainty = Mathf.Clamp01(0.01f * recipientLossFactor * (0.8f + (Rand.Value * 0.4f)));

            // Would be pretty funny if they both decide to change their beliefs at the same time
            HandleConversion(
                initiator,
                recipient,
                initiatorTracker,
                recipientTracker,
                initiatorIdeo,
                recipientIdeo,
                extraSentencePacks,
                ref letterText,
                ref letterLabel,
                ref letterDef,
                ref lookTargets);
            return true;
        }
        return false;
    }

    private static float ApplyTraitLossFactor(
        IEnumerable<TraitRequirement> traits,
        Pawn pawn,
        Ideo memeOwnerIdeoForCheck,
        MemeDef topic,
        bool agreeable)
    {
        var factor = 1f;
        foreach (var trait in traits)
        {
            if (trait.HasTrait(pawn))
            {
                var hasMeme = memeOwnerIdeoForCheck.memes.Contains(topic);
                factor *= agreeable
                    ? (hasMeme ? 0.8f : 1.2f)
                    : (hasMeme ? 1.2f : 0.8f);
            }
        }
        return factor;
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

    private static void AdjustOpinions(Pawn initiator, Pawn recipient, GameComponent_EnhancedBeliefs comp, MemeDef topic, float initiatorRoll, float recipientRoll)
    {
        Pawn winner;
        Pawn loser;

        if (initiatorRoll > recipientRoll)
        {
            winner = initiator;
            loser = recipient;
        }
        else
        {
            winner = recipient;
            loser = initiator;
        }

        var wasPositiveOutcome = winner.Ideo.memes.Contains(topic);

        var loserTracker = comp.PawnTracker.EnsurePawnHasIdeoTracker(loser);
        loserTracker.AdjustMemeOpinion(topic, (wasPositiveOutcome ? 0.03f : -0.03f) * winner.GetStatValue(StatDefOf.ConversionPower) * loser.GetStatValue(StatDefOf.CertaintyLossFactor));
    }
}
