using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace EnhancedBeliefs
{
    public class InteractionWorker_IdeologicalDebateMeme : InteractionWorker
    {
        public MemeDef topic;

        public static readonly SimpleCurve CompatibilityFactorCurve = new SimpleCurve
        {
            new CurvePoint(-1.5f, 0.1f),
            new CurvePoint(-0.5f, 0.5f),
            new CurvePoint(0f, 1f),
            new CurvePoint(0.5f, 1.3f),
            new CurvePoint(1f, 1.8f),
            new CurvePoint(2f, 3f)
        };

        public override float RandomSelectionWeight(Pawn initiator, Pawn recipient)
        {
            if (initiator.Inhumanized())
            {
                return 0f;
            }

            if (!ModsConfig.IdeologyActive)
            {
                return 0f;
            }

            if (Find.IdeoManager.classicMode)
            {
                return 0f;
            }

            if (initiator.Ideo == null || !recipient.RaceProps.Humanlike || initiator.Ideo == recipient.Ideo)
            {
                return 0f;
            }

            if (recipient.DevelopmentalStage.Baby())
            {
                return 0f;
            }

            if (initiator.skills.GetSkill(SkillDefOf.Social).TotallyDisabled)
            {
                return 0f;
            }

            return 0.03f * initiator.GetStatValue(StatDefOf.SocialIdeoSpreadFrequencyFactor) * CompatibilityFactorCurve.Evaluate(initiator.relations.CompatibilityWith(recipient));
        }

        public override void Interacted(Pawn initiator, Pawn recipient, List<RulePackDef> extraSentencePacks, out string letterText, out string letterLabel, out LetterDef letterDef, out LookTargets lookTargets)
        {
            letterText = null;
            letterLabel = null;
            letterDef = null;
            lookTargets = null;

            GameComponent_EnhancedBeliefs comp = Current.Game.GetComponent<GameComponent_EnhancedBeliefs>();

            IdeoTrackerData initiatorTracker = comp.pawnTracker.EnsurePawnHasIdeoTracker(initiator);
            IdeoTrackerData recipientTracker = comp.pawnTracker.EnsurePawnHasIdeoTracker(recipient);

            Ideo initiatorIdeo = initiator.Ideo;
            Ideo recipientIdeo = recipient.Ideo;

            topic = initiatorIdeo.memes.Union(recipientIdeo.memes).RandomElement();

            float initiatorRoll = Rand.Value * initiator.GetStatValue(StatDefOf.ConversionPower) / initiator.GetStatValue(StatDefOf.CertaintyLossFactor) * initiator.GetStatValue(StatDefOf.SocialImpact) * (1f + (initiator.ideo.Certainty - 0.6f) * 0.5f);
            float recipientRoll = Rand.Value * recipient.GetStatValue(StatDefOf.ConversionPower) / recipient.GetStatValue(StatDefOf.CertaintyLossFactor) * recipient.GetStatValue(StatDefOf.SocialImpact) * (1f + (recipient.ideo.Certainty - 0.6f) * 0.5f);

            Pawn winner;
            Pawn loser;

            if (initiatorRoll - recipientRoll > 0.1f)
            {
                winner = initiator;
                loser = recipient;
            }
            else if (recipientRoll - initiatorRoll > 0.1f)
            {
                winner = recipient;
                loser = initiator;
            }
            else
            {
                // A draw happened, time for special cases!

                // Fetch social fight multiplier
                interaction.socialFightBaseChance = 1f;
                float fightChanceModifier = (initiator.interactions.SocialFightChance(interaction, recipient) + recipient.interactions.SocialFightChance(interaction, initiator));
                interaction.socialFightBaseChance = 0f;

                // Socially adept pawns are much less likely to start a brawl over an ideological debate
                float socialFightChance = 0.05f * fightChanceModifier / (0.5f + initiator.skills.GetSkill(SkillDefOf.Social).Level * 0.1f) / (0.5f + recipient.skills.GetSkill(SkillDefOf.Social).Level * 0.1f);

                if (Rand.Value < socialFightChance)
                {
                    recipient.interactions.StartSocialFight(initiator, "{PAWN1_nameDef} tried to debate ideological views with {PAWN2_nameDef}. This led to a social fight!");
                    return;
                }

                // Smarter pawns have a higher chance of arriving to a mutual conclusion that both of their ideoligions suck
                float randomOpinion = 0.2f * (0.75f + initiator.skills.GetSkill(SkillDefOf.Intellectual).Level * 0.05f) * (0.75f + recipient.skills.GetSkill(SkillDefOf.Intellectual).Level * 0.05f) / (0.2f + ((initiator.ideo.Certainty + recipient.ideo.Certainty) / 2f) * 0.8f);

                if (Rand.Value < randomOpinion)
                {
                    float initiatorLossFactor = initiator.GetStatValue(StatDefOf.CertaintyLossFactor);
                    float recipientLossFactor = recipient.GetStatValue(StatDefOf.CertaintyLossFactor);

                    if (!topic.agreeableTraits.NullOrEmpty())
                    {
                        for (int i = 0; i < topic.agreeableTraits.Count; i++)
                        {
                            TraitRequirement trait = topic.agreeableTraits[i];

                            if (trait.HasTrait(initiator))
                            {
                                initiatorLossFactor *= (initiator.Ideo.memes.Contains(topic) ? 0.8f : 1.2f);
                            }

                            if (trait.HasTrait(recipient))
                            {
                                recipientLossFactor *= (recipient.Ideo.memes.Contains(topic) ? 0.8f : 1.2f);
                            }
                        }
                    }

                    if (!topic.disagreeableTraits.NullOrEmpty())
                    {
                        for (int i = 0; i < topic.disagreeableTraits.Count; i++)
                        {
                            TraitRequirement trait = topic.disagreeableTraits[i];

                            if (trait.HasTrait(initiator))
                            {
                                initiatorLossFactor *= (initiator.Ideo.memes.Contains(topic) ? 1.2f : 0.8f);
                            }

                            if (trait.HasTrait(recipient))
                            {
                                recipientLossFactor *= (initiator.Ideo.memes.Contains(topic) ? 1.2f : 0.8f);
                            }
                        }
                    }

                    initiatorTracker.AdjustMemeOpinion(topic, -0.03f * initiatorLossFactor * (0.8f + Rand.Value * 0.4f));
                    recipientTracker.AdjustMemeOpinion(topic, -0.03f * recipientLossFactor * (0.8f + Rand.Value * 0.4f));

                    initiator.ideo.Certainty = Mathf.Clamp01(0.01f * initiatorLossFactor * (0.8f + Rand.Value * 0.4f));
                    recipient.ideo.Certainty = Mathf.Clamp01(0.01f * recipientLossFactor * (0.8f + Rand.Value * 0.4f));

                    // Would be pretty funny if they both decide to change their beliefs at the same time

                    if (initiatorTracker.CheckConversion() == ConversionOutcome.Success)
                    {
                        if (PawnUtility.ShouldSendNotificationAbout(initiator) || PawnUtility.ShouldSendNotificationAbout(recipient))
                        {
                            letterLabel = "LetterLabelConvertIdeoAttempt_Success".Translate();
                            letterText = "Debates between {0] and {1} resulted in {1} turning away from {2} and towards {3}.".Formatted(initiator, recipient, initiatorIdeo, initiator.Ideo);
                            letterDef = LetterDefOf.NeutralEvent;
                            lookTargets = new LookTargets(recipient, initiator);
                            Precept_Role role = initiatorIdeo.GetRole(initiator);

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
                            letterText = "Debates between {0] and {1} resulted in {1} turning away from {2} and towards {3}.".Formatted(recipient, initiator, recipientIdeo, recipient.Ideo);
                            letterDef = LetterDefOf.NeutralEvent;
                            lookTargets = new LookTargets(initiator, recipient);
                            Precept_Role role = recipientIdeo.GetRole(recipient);

                            if (role != null)
                            {
                                letterText = letterText + "\n\n" + "LetterRoleLostLetterIdeoChangedPostfix".Translate(recipient.Named("PAWN"), role.Named("ROLE"), recipientIdeo.Named("OLDIDEO")).Resolve();
                            }
                        }

                        extraSentencePacks.Add(RulePackDefOf.Sentence_ConvertIdeoAttemptSuccess);
                    }

                    return;
                }

                return;
            }

            bool positiveOutcome = winner.Ideo.memes.Contains(topic);
            IdeoTrackerData loserTracker = comp.pawnTracker.EnsurePawnHasIdeoTracker(loser);
            loserTracker.AdjustMemeOpinion(topic, (positiveOutcome ? 0.03f : -0.03f) * winner.GetStatValue(StatDefOf.ConversionPower) * loser.GetStatValue(StatDefOf.CertaintyLossFactor));
        }
    }
}
