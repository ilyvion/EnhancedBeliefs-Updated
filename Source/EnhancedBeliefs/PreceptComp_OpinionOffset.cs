namespace EnhancedBeliefs;

internal class PreceptComp_OpinionOffset : PreceptComp
{
    public int externalOffset = 0;
    public int internalOffset = 0;
    public List<TraitRequirement> agreeableTraits = [];
    public List<TraitRequirement> disagreeableTraits = [];
    public float opinionPerTrait = 2f;

    public virtual int ExternalOffset => externalOffset;
    public virtual int InternalOffset => internalOffset;

    public virtual float GetTraitOpinion(Pawn pawn)
    {
        var opinion = 0f;

        foreach (var trait in agreeableTraits)
        {
            if (trait.HasTrait(pawn))
            {
                opinion += 1f;
            }
        }


        foreach (var trait in disagreeableTraits)
        {
            if (trait.HasTrait(pawn))
            {
                opinion -= 1f;
            }
        }

        return opinion * opinionPerTrait;
    }
}
