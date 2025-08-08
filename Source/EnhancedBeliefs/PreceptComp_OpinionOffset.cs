namespace EnhancedBeliefs;

public class PreceptComp_OpinionOffset : PreceptComp
{
#pragma warning disable CS0649
#pragma warning disable IDE0044 // Add readonly modifier
    private int externalOffset;
    private int internalOffset;
#pragma warning restore IDE0044
#pragma warning restore CS0649
#pragma warning disable CA1051 // Do not declare visible instance fields
    public List<TraitRequirement> agreeableTraits = [];
    public List<TraitRequirement> disagreeableTraits = [];
    public float opinionPerTrait = 2f;
#pragma warning restore CA1051

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
