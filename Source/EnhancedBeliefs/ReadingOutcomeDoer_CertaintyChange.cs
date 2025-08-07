namespace EnhancedBeliefs
{
    public class ReadingOutcomeDoer_CertaintyChange : BookOutcomeDoer
    {
        public new BookOutcomeProperties_CertaintyChange Props => props as BookOutcomeProperties_CertaintyChange;

        public Ideo ideo;

        // In percents, so divided by 100 when actually applied
        public static readonly SimpleCurve certaintyGainFromQuality = new SimpleCurve
        {
            new CurvePoint(0f, 0.0003f),
            new CurvePoint(1f, 0.0006f),
            new CurvePoint(2f, 0.0009f),
            new CurvePoint(3f, 0.0013f),
            new CurvePoint(4f, 0.0017f),
            new CurvePoint(5f, 0.0022f),
            new CurvePoint(6f, 0.0027f)
        };

        public override bool DoesProvidesOutcome(Pawn reader)
        {
            if (!ModsConfig.IdeologyActive)
            {
                return false;
            }

            if (Find.IdeoManager.classicMode)
            {
                return false;
            }

            if (reader.Ideo == null)
            {
                return false;
            }

            if (reader.DevelopmentalStage.Baby())
            {
                return false;
            }

            return true;
        }

        public override void OnBookGenerated(Pawn author = null)
        {
            base.OnBookGenerated(author);

            if (author != null && author.Ideo != null)
            {
                ideo = author.Ideo;
                return;
            }

            ideo = Find.IdeoManager.IdeosListForReading.RandomElement();
        }

        public override void Reset()
        {
            base.Reset();
            ideo = null;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                if (!Find.IdeoManager.IdeosListForReading.Contains(ideo))
                {
                    ideo = null;
                }
            }

            Scribe_References.Look(ref ideo, "ideo");
        }

        public override IEnumerable<Dialog_InfoCard.Hyperlink> GetHyperlinks()
        {
            if (!Find.IdeoManager.IdeosListForReading.Contains(ideo) || ideo == null)
            {
                yield break;
            }

            yield return new Dialog_InfoCard.Hyperlink(ideo);
        }

        public override string GetBenefitsString(Pawn reader = null)
        {
            return "{0} - {1} certainty gain per second.".Formatted(ideo, (CertaintyGain(reader) * GenTicks.TicksPerRealSecond).ToStringPercent());
        }

        public float CertaintyGain(Pawn reader = null)
        {
            float certaintyGain = certaintyGainFromQuality.Evaluate((int)Quality) / 100f;

            if (reader != null)
            {
                if (reader.Ideo == ideo)
                {
                    certaintyGain = certaintyGain / reader.GetStatValue(StatDefOf.CertaintyLossFactor);
                }
                else
                {
                    certaintyGain = certaintyGain * reader.GetStatValue(StatDefOf.CertaintyLossFactor) * 0.5f;
                }
            }

            return certaintyGain;
        }

        public override void OnReadingTick(Pawn reader, float factor)
        {
            base.OnReadingTick(reader, factor);

            if (reader.Ideo == null)
            {
                return;
            }

            if (!Find.IdeoManager.IdeosListForReading.Contains(ideo) || ideo == null)
            {
                return;
            }

            float certaintyGain = CertaintyGain(reader) * factor;

            if (reader.Ideo == ideo)
            {
                reader.ideo.Certainty = Mathf.Clamp01(reader.ideo.Certainty + certaintyGain);
                return;
            }

            GameComponent_EnhancedBeliefs comp = Current.Game.GetComponent<GameComponent_EnhancedBeliefs>();
            IdeoTrackerData tracker = comp.pawnTracker.EnsurePawnHasIdeoTracker(reader);

            reader.ideo.Certainty = Mathf.Clamp01(reader.ideo.Certainty - certaintyGain * 0.25f);
            tracker.AdjustPersonalOpinion(ideo, certaintyGain);
        }
    }

    public class BookOutcomeProperties_CertaintyChange : BookOutcomeProperties
    {
        public override Type DoerClass => typeof(ReadingOutcomeDoer_CertaintyChange);
    }
}
