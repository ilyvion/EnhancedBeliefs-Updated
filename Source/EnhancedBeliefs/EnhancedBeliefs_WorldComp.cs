using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace EnhancedBeliefs
{
    public class EnhancedBeliefs_WorldComp : WorldComponent
    {
        // Days to percentage
        public static readonly SimpleCurve CertaintyLossFromInactivity = new SimpleCurve
        {
            new CurvePoint(3f,  0.01f),
            new CurvePoint(5f,  0.02f),
            new CurvePoint(10f, 0.03f),
            new CurvePoint(30f, 0.05f),
        };

        // Sum mood offset to percentage
        public static readonly SimpleCurve CertaintyOffsetFromThoughts = new SimpleCurve
        {
            new CurvePoint(-50f, -0.15f),
            new CurvePoint(-30f, -0.1f),
            new CurvePoint(-10f, -0.05f),
            new CurvePoint(-5f,  -0.01f),
            new CurvePoint(-3f,  -0.005f),
            new CurvePoint(-0,    0f),
            new CurvePoint(3f,    0.002f),
            new CurvePoint(5f,    0.007f),
            new CurvePoint(10f,   0.05f),
            new CurvePoint(30f,   0.1f),
            new CurvePoint(50f,   0.15f),
        };

        // Sum relationship value to multiplier - 1. Values are flipped if summary mood offset is negative
        // I know that this doesn't actually result in symmetric multipliers, but else we'll get x10 certainty loss if you hate everyone and everything in your ideology
        public static readonly SimpleCurve CertaintyMultiplierFromRelationships = new SimpleCurve
        {
            new CurvePoint(-1000f, -0.9f),
            new CurvePoint(-500f,  -0.5f),
            new CurvePoint(-200f,  -0.3f),
            new CurvePoint(-100f,  -0.1f),
            new CurvePoint(-50f,   -0.05f),
            new CurvePoint(-10f,   -0.02f),
            new CurvePoint(0f,     -0f),
            new CurvePoint(10f,     0.01f),
            new CurvePoint(50f,     0.03f),
            new CurvePoint(100f,    0.07f),
            new CurvePoint(200f,    0.2f),
            new CurvePoint(500f,    0.4f),
            new CurvePoint(1000f,   0.6f),
        };

        public Dictionary<Pawn, IdeoTrackerData> pawnTrackerData = new Dictionary<Pawn, IdeoTrackerData> ();
        public Dictionary<Ideo, AdvIdeoData> ideoDataList = new Dictionary<Ideo, AdvIdeoData>();

        public EnhancedBeliefs_WorldComp(World world) : base(world) { }

        public override void ExposeData()
        {
            base.ExposeData();

            // Cleaning list from destroyed pawns
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                pawnTrackerData.RemoveAll((KeyValuePair<Pawn, IdeoTrackerData> x) => !PawnsFinder.All_AliveOrDead.Contains(x.Key));
            }

            // Same but for destroyed ideos, if that happens
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                ideoDataList.RemoveAll((KeyValuePair<Ideo, AdvIdeoData> x) => !Find.IdeoManager.IdeosListForReading.Contains(x.Key));
            }

            Scribe_Collections.Look(ref pawnTrackerData, "pawnTrackerData", LookMode.Reference, LookMode.Reference);
            Scribe_Collections.Look(ref ideoDataList, "ideoDataList", LookMode.Reference, LookMode.Reference);
        }

        public void AddTracker(Pawn_IdeoTracker tracker)
        {
            IdeoTrackerData data = new IdeoTrackerData(tracker.pawn);
            pawnTrackerData[tracker.pawn] = data;
        }

        public void AddIdeoTracker(Ideo ideo)
        {
            ideoDataList[ideo] = new AdvIdeoData();
        }

        public void SetIdeo(Pawn pawn, Ideo ideo)
        {
            if (!ideoDataList.ContainsKey(ideo))
            {
                ideoDataList[ideo] = new AdvIdeoData();
            }

            ideoDataList[ideo].pawnList.Add(pawn);
        }

        public static int BeliefDifferences(Ideo ideo1, Ideo ideo2)
        {
            int value = 0;

            for (int i = 0; i < ideo1.memes.Count; i++)
            {
                MemeDef meme1 = ideo1.memes[i];

                for (int j = 0; j < ideo2.memes.Count; j++)
                {
                    MemeDef meme2 = ideo2.memes[j];

                    if (meme1 == meme2)
                    {
                        value -= 1;
                    }
                    else if (meme1.exclusionTags.Intersect(meme2.exclusionTags).Count() > 0)
                    {
                        value += 1;
                    }
                }
            }

            return value;
        }

        public void FluidIdeoRecache(Ideo ideo)
        {
            foreach (KeyValuePair<Pawn, IdeoTrackerData> pair in pawnTrackerData)
            {
                pair.Value.baseIdeoOpinions[ideo] = pair.Value.DefaultIdeoOpinion(ideo);
            }
        }
    }

    public class IdeoTrackerData : IExposable
    {
        public Pawn pawn;
        public int lastPositiveThoughtTick = -1;
        public float cachedCertaintyChange = -9999f;

        // Separate because recalculating base from memes is cheaper in case player's ideo is fluid
        // Breaks if you multiply opinion but you really shouldn't do that
        public Dictionary<Ideo, float> baseIdeoOpinions = new Dictionary<Ideo, float>();
        public Dictionary<Ideo, float> personalIdeoOpinions = new Dictionary<Ideo, float>();

        public IdeoTrackerData(Pawn pawn)
        {
            lastPositiveThoughtTick = Find.TickManager.TicksGame;
            this.pawn = pawn;
        }

        public void CertaintyChangeRecache(EnhancedBeliefs_WorldComp worldComp)
        {
            cachedCertaintyChange = 0;
            List<Thought> thoughts = new List<Thought>();
            pawn.needs.mood.thoughts.GetAllMoodThoughts(thoughts);
            float moodSum = 0;

            for (int i = 0; i < thoughts.Count; i++)
            {
                Thought thought = thoughts[i];

                if (thought.sourcePrecept != null || thought.def.Worker is ThoughtWorker_Precept)
                {
                    moodSum += thought.MoodOffset();
                }
            }

            // Possible performance bottleneck? Check later

            List<Pawn> pawns = worldComp.ideoDataList[pawn.ideo.Ideo].pawnList;
            float relationshipSum = 0;

            for (int i = 0; i < pawns.Count; i++)
            {
                relationshipSum += pawn.relations.OpinionOf(pawns[i]);
            }

            float moodCertaintyOffset = EnhancedBeliefs_WorldComp.CertaintyOffsetFromThoughts.Evaluate(moodSum);
            float relationshipMultiplier = 1 + EnhancedBeliefs_WorldComp.CertaintyMultiplierFromRelationships.Evaluate(relationshipSum) * Math.Sign(moodCertaintyOffset);

            cachedCertaintyChange += moodCertaintyOffset * relationshipMultiplier;
        }

        public float IdeoOpinion(Ideo ideo)
        {
            if (!baseIdeoOpinions.ContainsKey(ideo))
            {
                baseIdeoOpinions[ideo] = DefaultIdeoOpinion(ideo);
                personalIdeoOpinions[ideo] = 0;
            }

            return baseIdeoOpinions[ideo] + personalIdeoOpinions[ideo];
        }

        // Get pawn's basic opinion from hearing about ideos beliefs, based on their traits, relationships and current ideo
        public float DefaultIdeoOpinion(Ideo ideo)
        {
            Ideo pawnIdeo = pawn.ideo.ideo;

            if (ideo == pawnIdeo)
            {
                return pawn.ideo.Certainty;
            }

            float opinion = 30;

            if (ideo.HasMeme(EnhancedBeliefsDefOf.Supremacist))
            {
                opinion -= 20;
            }
            else if (ideo.HasMeme(EnhancedBeliefsDefOf.Loyalist))
            {
                opinion -= 10;
            }
            else if (ideo.HasMeme(EnhancedBeliefsDefOf.Guilty))
            {
                opinion += 10;
            }

            for (int i = 0; i < ideo.memes.Count; i++)
            {
                MemeDef meme = ideo.memes[i];

                if (!meme.agreeableTraits.NullOrEmpty())
                {
                    for (int j = 0; j < meme.agreeableTraits.Count; j++)
                    {
                        TraitRequirement trait = meme.agreeableTraits[j];

                        if (trait.HasTrait(pawn))
                        {
                            opinion += 10;
                        }
                    }

                    for (int j = 0; j < meme.disagreeableTraits.Count; j++)
                    {
                        TraitRequirement trait = meme.disagreeableTraits[j];

                        if (trait.HasTrait(pawn))
                        {
                            opinion -= 10;
                        }
                    }
                }
            }

            // -5 opinion per incompatible meme, +5 per shared meme
            opinion -= EnhancedBeliefs_WorldComp.BeliefDifferences(pawnIdeo, ideo) * 5f;
            // Only decrease opinion if we don't like getting converted, shouldn't go the other way
            opinion *= Mathf.Clamp01(pawn.GetStatValue(StatDefOf.CertaintyLossFactor));

            return Mathf.Clamp(opinion, 0, 100);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref pawn, "pawn");
            Scribe_Values.Look(ref lastPositiveThoughtTick, "lastPositiveThoughtTick");
            Scribe_Collections.Look(ref baseIdeoOpinions, "baseIdeoOpinions", LookMode.Reference, LookMode.Value);
            Scribe_Collections.Look(ref personalIdeoOpinions, "personalIdeoOpinions", LookMode.Reference, LookMode.Value);
        }
    }

    public class AdvIdeoData : IExposable
    {
        public List<Pawn> pawnList = new List<Pawn>();

        public void ExposeData()
        {
            Scribe_Collections.Look(ref pawnList, "pawnList", LookMode.Reference);
        }
    }
}
