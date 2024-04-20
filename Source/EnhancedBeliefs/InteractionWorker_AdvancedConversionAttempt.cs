using NAudio.CoreAudioApi;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace EnhancedBeliefs
{
    public class InteractionWorker_AdvancedConversionAttempt : InteractionWorker_ConvertIdeoAttempt
    {
        public override void Interacted(Pawn initiator, Pawn recipient, List<RulePackDef> extraSentencePacks, out string letterText, out string letterLabel, out LetterDef letterDef, out LookTargets lookTargets)
        {
            letterLabel = null;
            letterText = null;
            letterDef = null;
            lookTargets = null;

            EnhancedBeliefs_WorldComp comp = Find.World.GetComponent<EnhancedBeliefs_WorldComp>();
            Ideo ideo = recipient.Ideo;
            Ideo newIdeo = initiator.Ideo;
            IdeoTrackerData tracker = comp.pawnTrackerData[recipient];
            float certainty = recipient.ideo.Certainty;
            float opinion = tracker.IdeoOpinion(newIdeo);

            // Conversion attempts don't actually adjust pawn's personal opinion of their own ideology, but rather certainty directly (aka base value). Initiatior's ideology does get personal opinion adjusted tho.
            // Unlike ideological debates which adjust opinion on specific memes and precepts and may affect multiple ideos at the same time
            // Unlike in vanilla, pawn's personal opinion of initiatior also matters a lot
            float conversionPower = initiator.GetStatValue(StatDefOf.ConversionPower) * recipient.GetStatValue(StatDefOf.CertaintyLossFactor) *
                ConversionUtility.ConversionPowerFactor_MemesVsTraits(initiator, recipient) * ReliquaryUtility.GetRelicConvertPowerFactorForPawn(initiator) *
                Find.Storyteller.difficulty.CertaintyReductionFactor(initiator, recipient) * (1 + recipient.relations.OpinionOf(initiator) * 0.5f * 0.01f);

            Precept_Role precept_Role = recipient.Ideo?.GetRole(recipient);
            if (precept_Role != null)
            {
                conversionPower *= precept_Role.def.certaintyLossFactor;
            }

            //Give it +- 0.2f random value, if we're REALLY bad at it we can go into negatives and only scare the pawn off
            conversionPower += 0.4f * (Rand.Value - 0.5f);

            float priorCertainty = recipient.ideo.Certainty;
            recipient.ideo.Certainty = Mathf.Clamp01(recipient.ideo.Certainty - 0.04f * conversionPower);
            tracker.AdjustPersonalOpinion(newIdeo, conversionPower);

            if (recipient.Spawned)
            {
                string text = "Certainty".Translate() + "\n" + priorCertainty.ToStringPercent() + " -> " + recipient.ideo.Certainty.ToStringPercent();
                MoteMaker.ThrowText(recipient.DrawPos, recipient.Map, text, 8f);
            }

            // Don't check for conversion if we screwed up so bad that the pawn got reaffirmed in its beliefs, go straight to negative outcomes
            // ...or if the pawn got a mental breakdown because they didn't get convinced in the new ideo, only that their current one is really bad
            if (conversionPower > 0 && tracker.CheckConversion(newIdeo) == ConversionOutcome.Success)
            {
                if (PawnUtility.ShouldSendNotificationAbout(initiator) || PawnUtility.ShouldSendNotificationAbout(recipient))
                {
                    letterLabel = "LetterLabelConvertIdeoAttempt_Success".Translate();
                    letterText = "LetterConvertIdeoAttempt_Success".Translate(initiator.Named("INITIATOR"), recipient.Named("RECIPIENT"), initiator.Ideo.Named("IDEO"), ideo.Named("OLDIDEO")).Resolve();
                    letterDef = LetterDefOf.PositiveEvent;
                    lookTargets = new LookTargets(initiator, recipient);
                    Precept_Role role = ideo.GetRole(recipient);

                    if (role != null)
                    {
                        letterText = letterText + "\n\n" + "LetterRoleLostLetterIdeoChangedPostfix".Translate(recipient.Named("PAWN"), role.Named("ROLE"), ideo.Named("OLDIDEO")).Resolve();
                    }
                }

                extraSentencePacks.Add(RulePackDefOf.Sentence_ConvertIdeoAttemptSuccess);
                return;
            }

            // For some reason vanilla calculations are completely random and don't take any social stats into consideration
            float outcome = Rand.Value * (1 + recipient.relations.OpinionOf(initiator) * 0.2f * 0.01f) * initiator.GetStatValue(StatDefOf.SocialImpact);

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
}
