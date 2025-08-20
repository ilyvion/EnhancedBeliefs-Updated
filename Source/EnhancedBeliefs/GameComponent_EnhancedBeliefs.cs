namespace EnhancedBeliefs;

#pragma warning disable CS9113 // Parameter is unread.
internal sealed partial class GameComponent_EnhancedBeliefs(Game game) : GameComponent
#pragma warning restore CS9113 // Parameter is unread.
{
    // Days to percentage
    internal static readonly SimpleCurve CertaintyLossFromInactivity =
    [
        new CurvePoint(3f,  0.01f),
        new CurvePoint(5f,  0.02f),
        new CurvePoint(10f, 0.03f),
        new CurvePoint(30f, 0.05f),
    ];

    // Sum mood offset to percentage
    internal static readonly SimpleCurve CertaintyOffsetFromThoughts =
    [
        new CurvePoint(-50f, -0.15f),
        new CurvePoint(-30f, -0.07f),
        new CurvePoint(-10f, -0.03f),
        new CurvePoint(-5f,  -0.015f),
        new CurvePoint(-3f,  -0.005f),
        new CurvePoint(-0,    0f),
        new CurvePoint(3f,    0.005f),
        new CurvePoint(5f,    0.012f),
        new CurvePoint(10f,   0.025f),
        new CurvePoint(30f,   0.05f),
        new CurvePoint(50f,   0.12f),
    ];

    // Sum relationship value to multiplier - 1. Values are flipped if summary mood offset is negative
    // I know that this doesn't actually result in symmetric multipliers, but else we'll get x10 certainty loss if you hate everyone and everything in your ideology
    internal static readonly SimpleCurve CertaintyMultiplierFromRelationships =
    [
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
    ];

    public PawnIdeoTracker PawnTracker { get; } = new();
    public IdeoPawnTracker IdeoTracker { get; } = new();

    public override void GameComponentTick()
    {
        base.GameComponentTick();
    }

#pragma warning disable IDE0079
#pragma warning disable IDE0060 // Remove unused parameter
    // TODO: This method seems... lacking. Investigate if it should be doing something more.
#pragma warning disable CA1822 // Mark members as static
    public float ConversionFactor(Pawn initiator, Pawn recipient)
#pragma warning restore CA1822 // Mark members as static
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore IDE0079
    {
        return 1f;
    }

    public void SetIdeo(Pawn pawn, Ideo ideo)
    {
        _ = PawnTracker.EnsurePawnHasIdeoTracker(pawn);

        foreach (var ideo2 in IdeoTracker.Select(kvp => kvp.Key).ToList())
        {
            _ = IdeoTracker.RemovePawnFromIdeoPawnTracker(ideo2, pawn);
        }

        if (ideo == null)
        {
            return;
        }

        IdeoTracker.EnsureIdeoPawnTrackerHasPawn(ideo, pawn);
    }

    internal static int BeliefDifferences(Ideo ideo1, Ideo ideo2)
    {
        var value = 0;

        foreach (var meme1 in ideo1.memes)
        {
            foreach (var meme2 in ideo2.memes)
            {
                if (meme1 == meme2)
                {
                    value -= 1;
                }
                else if (meme1.exclusionTags.Intersect(meme2.exclusionTags).Any())
                {
                    value += 1;
                }
            }
        }

        return value;
    }

    public void BaseOpinionRecache(Ideo ideo)
    {
        foreach (var ideoTracker in PawnTracker.Select(kvp => kvp.Value).ToList())
        {
            ideoTracker.SetIdeoBaseOpinion(ideo, ideoTracker.DefaultIdeoOpinion(ideo));
        }
    }

    public List<Pawn> GetIdeoPawns(Ideo ideo)
    {
        if (IdeoTracker.TryGetPawnTracker(ideo, out var pawnList))
        {
            return pawnList;
        }

        foreach (var pawn in PawnsFinder.All_AliveOrDead)
        {
            if (pawn.Ideo == ideo)
            {
                IdeoTracker.EnsureIdeoPawnTrackerHasPawn(ideo, pawn);
            }
        }

        return GetIdeoPawns(ideo);
    }
}

public enum ConversionOutcome : byte
{
    Failure = 0,
    Breakdown = 1,
    Success = 2
}
