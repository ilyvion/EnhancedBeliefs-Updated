namespace EnhancedBeliefs;

#pragma warning disable CS9113 // Parameter is unread.
internal partial class GameComponent_EnhancedBeliefs(Game game) : GameComponent
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

        foreach (var (ideo2, _) in IdeoTracker)
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
        foreach (var (_, ideoTracker) in PawnTracker)
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

internal class IdeoTrackerData(Pawn pawn) : IExposable
{
    private Pawn pawn = pawn;
    public Pawn Pawn => pawn;
    public void ForceNewPawn(Pawn newPawn)
    {
        pawn = newPawn;
    }

    private int lastPositiveThoughtTick = Find.TickManager.TicksGame;
    public int LastPositiveThoughtTick => lastPositiveThoughtTick;

    public float CachedCertaintyChange { get; private set; } = -9999f;

    // Separate because recalculating base from memes in case player's ideo is fluid cuts down on overall performance cost
    // Breaks if you multiply opinion but you really shouldn't do that
    private Dictionary<Ideo, float> baseIdeoOpinions = [];
    private Dictionary<Ideo, float> personalIdeoOpinions = [];

    private readonly Dictionary<Ideo, float> cachedRelationshipIdeoOpinions = [];
    private readonly Dictionary<Pawn, float> cachedRelationships = [];

    private Dictionary<MemeDef, float> memeOpinions = [];
    private Dictionary<PreceptDef, float> preceptOpinions = [];

    private List<Ideo>? cache1;
    private List<Ideo>? cache2;
    private List<MemeDef>? cache3;
    private List<PreceptDef>? cache4;
    private List<float>? cache5;
    private List<float>? cache6;
    private List<float>? cache7;
    private List<float>? cache8;

    private float cachedOpinionMultiplier = -1f;
    private int lastMultiplierCacheTick = -1;

    public void UpdateLastPositiveThoughtTick()
    {
        lastPositiveThoughtTick = Find.TickManager.TicksGame;
    }

    public void SetIdeoBaseOpinion(Ideo ideo, float opinion)
    {
        if (!baseIdeoOpinions.TryAdd(ideo, opinion))
        {
            baseIdeoOpinions[ideo] = opinion;
        }
    }

    public float OpinionMultiplier
    {
        get
        {
            if (cachedOpinionMultiplier >= 0f && Find.TickManager.TicksGame - 2000 > lastMultiplierCacheTick)
            {
                return cachedOpinionMultiplier;
            }

            cachedOpinionMultiplier = 1f;
            lastMultiplierCacheTick = Find.TickManager.TicksGame;

            if (Pawn.story == null || Pawn.story.traits == null)
            {
                return cachedOpinionMultiplier;
            }

            foreach (var trait in Pawn.story.traits.allTraits)
            {
                var ext = trait.def.GetModExtension<IdeoTraitExtension>();

                if (ext != null)
                {
                    cachedOpinionMultiplier *= ext.opinionMultiplier;
                }
            }

            return cachedOpinionMultiplier;
        }
    }

#pragma warning disable IDE0060 // Remove unused parameter
    // TODO: Figure out why worldComp was even passed here
    public void CertaintyChangeRecache(GameComponent_EnhancedBeliefs worldComp)
#pragma warning restore IDE0060 // Remove unused parameter
    {
        CachedCertaintyChange = 0;
        List<Thought> thoughts = [];
        Pawn.needs.mood.thoughts.GetAllMoodThoughts(thoughts);
        float moodSum = 0;

        if (Pawn.needs?.mood?.thoughts != null)
        {
            foreach (var thought in thoughts)
            {
                if (thought.sourcePrecept != null || thought.def.Worker is ThoughtWorker_Precept)
                {
                    moodSum += thought.MoodOffset();
                }
            }
        }

        var moodCertaintyOffset = GameComponent_EnhancedBeliefs.CertaintyOffsetFromThoughts.Evaluate(moodSum);
        var relationshipMultiplier = 1 + (GameComponent_EnhancedBeliefs.CertaintyMultiplierFromRelationships.Evaluate(IdeoOpinionFromRelationships(Pawn.Ideo) / 0.02f) * Math.Sign(moodCertaintyOffset));

        CachedCertaintyChange += moodCertaintyOffset * relationshipMultiplier;

        // Certainty only starts decreasing at moods below stellar and after 3 days of lacking positive precept moodlets
        if (Pawn.needs?.mood.CurLevelPercentage < 0.8 && Find.TickManager.TicksGame - LastPositiveThoughtTick > GenDate.TicksPerDay * 3f)
        {
            CachedCertaintyChange -= GameComponent_EnhancedBeliefs.CertaintyLossFromInactivity.Evaluate((Find.TickManager.TicksGame - LastPositiveThoughtTick) / GenDate.TicksPerDay);
        }
    }

    // Form opinion based on memes, personal thoughts and experience with other pawns from that ideo
    public float IdeoOpinion(Ideo ideo)
    {
        if (!baseIdeoOpinions.ContainsKey(ideo) || !personalIdeoOpinions.ContainsKey(ideo))
        {
            baseIdeoOpinions[ideo] = DefaultIdeoOpinion(ideo);
            personalIdeoOpinions[ideo] = 0;
        }

        if (ideo == Pawn.Ideo)
        {
            baseIdeoOpinions[ideo] = Pawn.ideo.Certainty * 100f;
        }

        return Mathf.Clamp(baseIdeoOpinions[ideo] + PersonalIdeoOpinion(ideo) + IdeoOpinionFromRelationships(ideo), 0, 100) / 100f;
    }

    // Rundown on the function above, for UI reasons
    public float[] DetailedIdeoOpinion(Ideo ideo, bool noRelationship = false)
    {
        if (!baseIdeoOpinions.ContainsKey(ideo))
        {
            _ = IdeoOpinion(ideo);
        }

        if (ideo == Pawn.Ideo)
        {
            baseIdeoOpinions[ideo] = Pawn.ideo.Certainty * 100f;
        }

        // In cases where you want to avoid recursion
        return noRelationship
            ? [baseIdeoOpinions[ideo] / 100f, PersonalIdeoOpinion(ideo) / 100f]
            : [baseIdeoOpinions[ideo] / 100f, PersonalIdeoOpinion(ideo) / 100f, IdeoOpinionFromRelationships(ideo) / 100f];
    }

    // Get pawn's basic opinion from hearing about ideos beliefs, based on their traits, relationships and current ideo
    public float DefaultIdeoOpinion(Ideo ideo)
    {
        var pawnIdeo = Pawn.Ideo;

        if (ideo == pawnIdeo)
        {
            return Pawn.ideo.Certainty * 100f;
        }

        float opinion = 0;

        if (Pawn.Ideo.HasMeme(EnhancedBeliefsDefOf.Supremacist))
        {
            opinion -= 20;
        }
        else if (Pawn.Ideo.HasMeme(EnhancedBeliefsDefOf.Loyalist))
        {
            opinion -= 10;
        }
        else if (Pawn.Ideo.HasMeme(EnhancedBeliefsDefOf.Guilty))
        {
            opinion += 10;
        }

        foreach (var meme in ideo.memes)
        {
            if (!meme.agreeableTraits.NullOrEmpty())
            {
                foreach (var trait in meme.agreeableTraits)
                {
                    if (trait.HasTrait(Pawn))
                    {
                        opinion += 10;
                    }
                }
            }

            if (!meme.disagreeableTraits.NullOrEmpty())
            {
                foreach (var trait in meme.disagreeableTraits)
                {
                    if (trait.HasTrait(Pawn))
                    {
                        opinion -= 10;
                    }
                }
            }
        }

        foreach (var precept in Pawn.Ideo.precepts)
        {
            var comps = precept.TryGetComps<PreceptComp_OpinionOffset>();

            foreach (var comp in comps)
            {
                opinion += comp.InternalOffset;
            }
        }

        foreach (var precept in ideo.precepts)
        {
            var comps = precept.TryGetComps<PreceptComp_OpinionOffset>();

            foreach (var comp in comps)
            {
                opinion += comp.ExternalOffset + comp.GetTraitOpinion(Pawn);
            }
        }

        // -5 opinion per incompatible meme, +5 per shared meme
        opinion -= GameComponent_EnhancedBeliefs.BeliefDifferences(pawnIdeo, ideo) * 5f;
        // Only decrease opinion if we don't like getting converted, shouldn't go the other way
        opinion *= Mathf.Clamp01(Pawn.GetStatValue(StatDefOf.CertaintyLossFactor));
        opinion *= OpinionMultiplier;

        // 30 base opinion
        return Mathf.Clamp(opinion + 30f, 0, 100);
    }

    public float PersonalIdeoOpinion(Ideo ideo)
    {
        if (!baseIdeoOpinions.TryGetValue(ideo, out var baseIdeoOpinion))
        {
            baseIdeoOpinion = DefaultIdeoOpinion(ideo);
            baseIdeoOpinions[ideo] = baseIdeoOpinion;
        }
        if (!personalIdeoOpinions.TryGetValue(ideo, out var personalIdeoOpinion))
        {
            personalIdeoOpinion = 0;
            personalIdeoOpinions[ideo] = personalIdeoOpinion;
        }

        float opinion = 0;

        foreach (var meme in ideo.memes)
        {
            if (memeOpinions.TryGetValue(meme, out var memeOpinion))
            {
                opinion += memeOpinion;
            }
        }

        foreach (var preceptDef in ideo.precepts.Select(p => p.def))
        {
            if (preceptOpinions.TryGetValue(preceptDef, out var preceptOpinion))
            {
                opinion += preceptOpinion;
            }
        }

        // Makes sure that pawn's personal opinion cannot go below/above 100% purely from circlejerking
        var curOpinion = Mathf.Clamp(baseIdeoOpinion + opinion, 0, 100);
        if (personalIdeoOpinion > 100f - curOpinion)
        {
            personalIdeoOpinions[ideo] = 100f - curOpinion;
        }
        else if (personalIdeoOpinions[ideo] < -curOpinion)
        {
            personalIdeoOpinions[ideo] = -curOpinion;
        }

        return (opinion + personalIdeoOpinions[ideo]) * OpinionMultiplier;
    }

    public float IdeoOpinionFromRelationships(Ideo ideo)
    {
        if (!cachedRelationshipIdeoOpinions.ContainsKey(ideo))
        {
            CacheRelationshipIdeoOpinion(ideo);
        }

        return cachedRelationshipIdeoOpinions[ideo] * OpinionMultiplier;
    }

    // Calculates ideo opinion offset based on how much pawn likes other pawns of other ideos, should have little weight overall
    // Relationships are a dynamic mess of cosmic scale so there really isn't a better way to do this
    public void RecalculateRelationshipIdeoOpinions()
    {
        foreach (var ideo in baseIdeoOpinions.Keys)
        {
            CacheRelationshipIdeoOpinion(ideo);
        }
    }

    // Caches specific ideo opinion from relationships
    public void CacheRelationshipIdeoOpinion(Ideo ideo)
    {
        float opinion = 0;
        var comp = Current.Game.GetComponent<GameComponent_EnhancedBeliefs>();
        var pawns = comp.GetIdeoPawns(ideo);

        foreach (var otherPawn in pawns)
        {
            // Up to +-2 opinion per pawn
            float pawnOpinion = Pawn.relations.OpinionOf(otherPawn);
            opinion += pawnOpinion * 0.02f;
            cachedRelationships[Pawn] = pawnOpinion;
        }

        cachedRelationshipIdeoOpinions[ideo] = opinion;
    }

    public void ExposeData()
    {
        Scribe_References.Look(ref pawn, "pawn");
        Scribe_Values.Look(ref lastPositiveThoughtTick, "lastPositiveThoughtTick");
        Scribe_Collections.Look(ref baseIdeoOpinions, "baseIdeoOpinions", LookMode.Reference, LookMode.Value, ref cache1, ref cache5);
        Scribe_Collections.Look(ref personalIdeoOpinions, "personalIdeoOpinions", LookMode.Reference, LookMode.Value, ref cache2, ref cache6);
        Scribe_Collections.Look(ref memeOpinions, "memeOpinions", LookMode.Def, LookMode.Value, ref cache3, ref cache7);
        Scribe_Collections.Look(ref preceptOpinions, "preceptOpinions", LookMode.Def, LookMode.Value, ref cache4, ref cache8);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            var comp = Current.Game.GetComponent<GameComponent_EnhancedBeliefs>();

            if (Pawn == null)
            {
                return;
            }

            comp.SetIdeo(Pawn, Pawn.Ideo);
        }
    }

    // Change pawn's personal opinion of another ideo, usually positively
    public void AdjustPersonalOpinion(Ideo ideo, float power)
    {
        if (!baseIdeoOpinions.ContainsKey(ideo) || !personalIdeoOpinions.ContainsKey(ideo))
        {
            baseIdeoOpinions[ideo] = DefaultIdeoOpinion(ideo);
            personalIdeoOpinions[ideo] = 0;
        }

        personalIdeoOpinions[ideo] += power * 100f;
    }

    public void AdjustMemeOpinion(MemeDef meme, float power)
    {
        memeOpinions ??= [];

        if (!memeOpinions.ContainsKey(meme))
        {
            memeOpinions[meme] = 0;
        }

        memeOpinions[meme] += power * 100f;
    }

    public void AdjustPreceptOpinion(PreceptDef precept, float power)
    {
        preceptOpinions ??= [];

        if (!preceptOpinions.ContainsKey(precept))
        {
            preceptOpinions[precept] = 0;
        }

        preceptOpinions[precept] += power * 100f;
    }

    public float TrueMemeOpinion(MemeDef meme)
    {
        if (!memeOpinions.TryGetValue(meme, out var opinion))
        {
            opinion = 0;
            memeOpinions[meme] = opinion;
        }

        if (!meme.agreeableTraits.NullOrEmpty())
        {
            foreach (var trait in meme.agreeableTraits)
            {
                if (trait.HasTrait(Pawn))
                {
                    opinion += 10;
                }
            }
        }

        if (!meme.disagreeableTraits.NullOrEmpty())
        {
            foreach (var trait in meme.disagreeableTraits)
            {
                if (trait.HasTrait(Pawn))
                {
                    opinion -= 10;
                }
            }
        }

        return opinion;
    }

    public float TruePreceptOpinion(PreceptDef precept)
    {
        if (!preceptOpinions.TryGetValue(precept, out var opinion))
        {
            opinion = 0;
            preceptOpinions[precept] = opinion;
        }

        var comps = precept.TryGetComps<PreceptComp_OpinionOffset>();

        foreach (var comp in comps)
        {
            opinion += comp.ExternalOffset + comp.GetTraitOpinion(Pawn);
        }

        return opinion;
    }

    public void RecacheAllBaseOpinions()
    {
        foreach (var ideo in baseIdeoOpinions.Keys)
        {
            baseIdeoOpinions[ideo] = DefaultIdeoOpinion(ideo);
        }
    }

    // Check if pawn should get converted to a new ideo after losing certainty in some way.
    // TODO: This sounds like a method without side-effects, but it actually makes changes,
    //       so go through the code and figure out what it actually does and then rename it to something more appropriate.
    public ConversionOutcome CheckConversion(
        Ideo? priorityIdeo = null,
        bool noBreakdown = false,
        List<Ideo>? excludeIdeos = null,
        List<Ideo>? whitelistIdeos = null,
        float? opinionThreshold = null)
    {
        if (!ModLister.CheckIdeology("Ideoligion conversion") || Pawn.DevelopmentalStage.Baby())
        {
            return ConversionOutcome.Failure;
        }
        if (Find.IdeoManager.classicMode)
        {
            return ConversionOutcome.Failure;
        }

        var certainty = Pawn.ideo.Certainty;

        if (certainty > 0.2f)
        {
            return ConversionOutcome.Failure;
        }

        var threshold = certainty <= 0f ? 0.6f : 0.85f; //Drastically lower conversion threshold if we're about to have a breakdown

        if (opinionThreshold.HasValue) // Or if we're already having one
        {
            threshold = opinionThreshold.Value;
        }

        var currentOpinion = IdeoOpinion(Pawn.Ideo);
        var ideos = whitelistIdeos ?? Find.IdeoManager.IdeosListForReading;
        ideos.SortBy(IdeoOpinion);

        if (excludeIdeos != null)
        {
            ideos = [.. ideos.Except(excludeIdeos)];
        }

        // Moves priority ideo up to the top of the list so if the pawn is being converted and not having a random breakdown, they're gonna probably get converted to the target ideology
        if (priorityIdeo != null)
        {
            _ = ideos.Remove(priorityIdeo);
            ideos.Add(priorityIdeo);
        }

        foreach (var ideo in ideos.Reverse<Ideo>())
        {
            if (ideo == Pawn.Ideo)
            {
                continue;
            }

            var opinion = IdeoOpinion(ideo);

            // Also don't convert in case we somehow like our current ideo significantly more than the new one
            // Either we have VERY high relationships with a lot of people or very strong personal opinions on current ideology for this to even be possible
            if (opinion < threshold || currentOpinion > opinion)
            {
                continue;
            }

            // 17% minimal chance of conversion at 20% certrainty and 85% opinion, half that if we're being converted and this is a wrong ideology. Randomly converting to a wrong ideology should be just a rare lol moment
            if (Rand.Value > (1 - (certainty * 4f)) * (opinion + (certainty <= 0f ? 0.3f : 0)) * ((priorityIdeo != null && priorityIdeo != ideo) ? 0.5f : 1f))
            {
                continue;
            }

            var oldIdeoContains = Pawn.ideo.PreviousIdeos.Contains(ideo);
            var oldIdeo = Pawn.Ideo;
            Pawn.ideo.SetIdeo(ideo);
            ideo.Notify_MemberGainedByConversion();

            // Move personal opinion into certainty i.e. base opinion, then zero it, since base opinions are fixed and personal beliefs are what is usually meant by certainty anyways
            var rundown = DetailedIdeoOpinion(ideo);
            Pawn.ideo.Certainty = Mathf.Min(rundown[0], 0.2f) + rundown[1];
            personalIdeoOpinions[ideo] = 0;

            // Keep current opinion of our old ideo by moving difference between new base and old base (certainty) into personal thoughts
            AdjustPersonalOpinion(oldIdeo, certainty - DetailedIdeoOpinion(oldIdeo)[0]);

            if (!oldIdeoContains)
            {
                Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.ConvertedNewMember, Pawn.Named(HistoryEventArgsNames.Doer), ideo.Named(HistoryEventArgsNames.Ideo)));
            }

            RecacheAllBaseOpinions();

            return ConversionOutcome.Success;
        }

        if (certainty > 0f || noBreakdown)
        {
            return ConversionOutcome.Failure;
        }

        // Oops
        _ = Pawn.mindState.mentalStateHandler.TryStartMentalState(EnhancedBeliefsDefOf.IdeoChange);
        return ConversionOutcome.Breakdown;
    }

    public bool OverrideConversionAttempt(float certaintyReduction, Ideo newIdeo, bool applyCertaintyFactor = true)
    {
        if (Find.IdeoManager.classicMode || Pawn.ideo == null || Pawn.DevelopmentalStage.Baby())
        {
            return false;
        }

        var newCertainty = Mathf.Clamp01(Pawn.ideo.Certainty + (applyCertaintyFactor ? Pawn.ideo.ApplyCertaintyChangeFactor(0f - certaintyReduction) : (0f - certaintyReduction)));

        EnhancedBeliefsUtilities.ShowCertaintyChangeMote(Pawn, Pawn.ideo.Certainty, newCertainty);

        var ideoOpinion = PersonalIdeoOpinion(Pawn.Ideo);
        if (ideoOpinion > 0)
        {
            AdjustPersonalOpinion(Pawn.Ideo, Math.Max(ideoOpinion * -0.01f, -0.25f * certaintyReduction));
        }

        Pawn.ideo.Certainty = newCertainty;

        return CheckConversion(newIdeo) == ConversionOutcome.Success;
    }
}

public enum ConversionOutcome : byte
{
    Failure = 0,
    Breakdown = 1,
    Success = 2
}
