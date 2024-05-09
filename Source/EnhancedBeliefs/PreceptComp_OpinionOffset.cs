using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace EnhancedBeliefs
{
    public class PreceptComp_OpinionOffset : PreceptComp
    {
        public int externalOffset = 0;
        public int internalOffset = 0;
        public List<TraitRequirement> agreeableTraits;
        public List<TraitRequirement> disagreeableTraits;
        public float opinionPerTrait = 1f;

        public virtual int ExternalOffset
        {
            get
            {
                return externalOffset;
            }
        }
        public virtual int InternalOffset
        {
            get
            {
                return internalOffset;
            }
        }

        public virtual float GetTraitOpinion(Pawn pawn)
        {
            float opinion = 0f;

            if (!agreeableTraits.NullOrEmpty())
            {
                for (int i = 0; i < agreeableTraits.Count; i++)
                {
                    TraitRequirement trait = agreeableTraits[i];

                    if (trait.HasTrait(pawn))
                    {
                        opinion += 1f;
                    }
                }
            }

            if (!disagreeableTraits.NullOrEmpty())
            {
                for (int i = 0; i < disagreeableTraits.Count; i++)
                {
                    TraitRequirement trait = disagreeableTraits[i];

                    if (trait.HasTrait(pawn))
                    {
                        opinion -= 1f;
                    }
                }
            }

            return opinion * opinionPerTrait;
        }
    }
}
