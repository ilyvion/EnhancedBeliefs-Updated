namespace EnhancedBeliefs;

internal sealed class InteractionWorker_AdvancedConversionAttempt : InteractionWorker_ConvertIdeoAttempt
{
    public override void Interacted(
        Pawn initiator,
        Pawn recipient,
        List<RulePackDef> extraSentencePacks,
        out string? letterText,
        out string? letterLabel,
        out LetterDef? letterDef,
        out LookTargets? lookTargets)
    {
        letterLabel = null;
        letterText = null;
        letterDef = null;
        lookTargets = null;

        var comp = Current.Game.GetComponent<GameComponent_EnhancedBeliefs>();
        var recipientIdeo = recipient.Ideo;
        var initiatorIdeo = initiator.Ideo;
        var recipientTracker = comp.PawnTracker.EnsurePawnHasIdeoTracker(recipient);

        var certaintyBefore = recipient.ideo.Certainty;

        // 1) Compute conversion power
        var conversionPower = CalculateConversionPower(initiator, recipient, comp);

        // 2) Apply certainty and opinion changes
        UpdateCertaintyAndOpinions(recipient, recipientIdeo, recipientTracker, initiatorIdeo, conversionPower);

        // 3) Feedback mote
        EnhancedBeliefsUtilities.ShowCertaintyChangeMote(recipient, certaintyBefore, recipient.ideo.Certainty);

        // 4) Try success path (may set letter fields and return early)
        if (TryHandleSuccessfulConversion(initiator, recipient, recipientTracker, initiatorIdeo, recipientIdeo, conversionPower, extraSentencePacks,
            ref letterText, ref letterLabel, ref letterDef, ref lookTargets))
        {
            return;
        }

        // 5) Handle failure/neutral outcomes
        HandleOutcome(initiator, recipient, extraSentencePacks, certaintyBefore);
    }

    private static float CalculateConversionPower(Pawn initiator, Pawn recipient, GameComponent_EnhancedBeliefs comp)
    {
        // Conversion attempts don't actually adjust pawn's personal opinion of their own ideology, but rather certainty directly (aka base value).
        // Initiator's ideology does get personal opinion adjusted though. Recipient's opinion of initiator matters a lot as well.
        var power = initiator.GetStatValue(StatDefOf.ConversionPower) *
                    recipient.GetStatValue(StatDefOf.CertaintyLossFactor) *
                    comp.ConversionFactor(initiator, recipient) *
                    ConversionUtility.ConversionPowerFactor_MemesVsTraits(initiator, recipient) *
                    ReliquaryUtility.GetRelicConvertPowerFactorForPawn(initiator) *
                    Find.Storyteller.difficulty.CertaintyReductionFactor(initiator, recipient) *
                    (1 + (recipient.relations.OpinionOf(initiator) * 0.5f * 0.01f));

        var recipientIdeoRole = recipient.Ideo?.GetRole(recipient);
        if (recipientIdeoRole != null)
        {
            power *= recipientIdeoRole.def.certaintyLossFactor;
        }

        // Give it +- 0.2f random value. If we're REALLY bad at it we can go into negatives and only scare the pawn off
        power += 0.4f * (Rand.Value - 0.5f);

        return power;
    }

    private static void UpdateCertaintyAndOpinions(Pawn recipient, Ideo recipientIdeo, IdeoTrackerData recipientTracker, Ideo initiatorIdeo, float conversionPower)
    {
        // Certainty drop scales with conversion power; also nudge personal opinions
        recipient.ideo.Certainty = Mathf.Clamp01(recipient.ideo.Certainty - (0.04f * conversionPower));
        recipientTracker.AdjustPersonalOpinion(initiatorIdeo, 0.08f * conversionPower);

        var ideoOpinion = recipientTracker.PersonalIdeoOpinion(recipientIdeo);
        if (ideoOpinion > 0)
        {
            recipientTracker.AdjustPersonalOpinion(recipientIdeo, Math.Max(ideoOpinion * -0.01f, -0.02f * conversionPower));
        }
    }

    private static bool TryHandleSuccessfulConversion(
        Pawn initiator,
        Pawn recipient,
        IdeoTrackerData recipientTracker,
        Ideo initiatorIdeo,
        Ideo recipientIdeo,
        float conversionPower,
        List<RulePackDef> extraSentencePacks,
        ref string? letterText,
        ref string? letterLabel,
        ref LetterDef? letterDef,
        ref LookTargets? lookTargets)
    {
        // Don't check for conversion if we screwed up so bad that the pawn got reaffirmed in its beliefs, go straight to negative outcomes
        // ...or if the pawn got a mental breakdown because they didn't get convinced in the new ideo, only that their current one is really bad
        if (conversionPower > 0 && recipientTracker.CheckConversion(initiatorIdeo) == ConversionOutcome.Success)
        {
            if (PawnUtility.ShouldSendNotificationAbout(initiator) || PawnUtility.ShouldSendNotificationAbout(recipient))
            {
                letterLabel = "LetterLabelConvertIdeoAttempt_Success".Translate();
                letterText = "LetterConvertIdeoAttempt_Success".Translate(initiator.Named("INITIATOR"), recipient.Named("RECIPIENT"), initiator.Ideo.Named("IDEO"), recipientIdeo.Named("OLDIDEO")).Resolve();
                letterDef = LetterDefOf.PositiveEvent;
                lookTargets = new LookTargets(initiator, recipient);
                var role = recipientIdeo.GetRole(recipient);

                if (role != null)
                {
                    letterText = letterText + "\n\n" + "LetterRoleLostLetterIdeoChangedPostfix".Translate(recipient.Named("PAWN"), role.Named("ROLE"), recipientIdeo.Named("OLDIDEO")).Resolve();
                }
            }

            extraSentencePacks.Add(RulePackDefOf.Sentence_ConvertIdeoAttemptSuccess);
            return true;
        }

        return false;
    }

    private static void HandleOutcome(Pawn initiator, Pawn recipient, List<RulePackDef> extraSentencePacks, float certainty)
    {
        // For some reason vanilla calculations are completely random and don't take any social stats into consideration
        var outcome = Rand.Value *
                    (1 + (recipient.relations.OpinionOf(initiator) * 0.2f * 0.01f)) *
                    initiator.GetStatValue(StatDefOf.SocialImpact);

        // Same code as vanilla, but less janky and makes more sense. 2% to have a fight, 10% to have a negative thought, 78% for nothing to happen at base opinion and impact.
        if (outcome < 0.02f && !recipient.IsPrisoner && recipient.interactions.SocialFightPossible(initiator))
        {
            recipient.interactions.StartSocialFight(initiator, "MessageFailedConvertIdeoAttemptSocialFight");
            extraSentencePacks.Add(RulePackDefOf.Sentence_ConvertIdeoAttemptFailSocialFight);
        }
        else if (outcome < 0.12f)
        {
            if (recipient.needs.mood != null)
            {
                if (PawnUtility.ShouldSendNotificationAbout(recipient))
                {
                    Messages.Message("MessageFailedConvertIdeoAttempt".Translate(initiator.Named("INITIATOR"), recipient.Named("RECIPIENT"), certainty.ToStringPercent().Named("CERTAINTYBEFORE"), recipient.ideo.Certainty.ToStringPercent().Named("CERTAINTYAFTER")), recipient, MessageTypeDefOf.NeutralEvent);
                }

                recipient.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.FailedConvertIdeoAttemptResentment, initiator);
            }

            extraSentencePacks.Add(RulePackDefOf.Sentence_ConvertIdeoAttemptFailResentment);
        }
        else
        {
            extraSentencePacks.Add(RulePackDefOf.Sentence_ConvertIdeoAttemptFail);
        }
    }
}
