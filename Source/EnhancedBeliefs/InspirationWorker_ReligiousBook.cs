namespace EnhancedBeliefs;

internal sealed class InspirationWorker_ReligiousBook : InspirationWorker
{
    public override bool InspirationCanOccur(Pawn pawn)
    {
        if (!base.InspirationCanOccur(pawn))
        {
            return false;
        }

        if (pawn == null || pawn.Map == null || !pawn.Position.IsValid || pawn.Ideo == null)
        {
            return false;
        }

        var precept_Role = pawn.Ideo.GetRole(pawn);

        return precept_Role == null || precept_Role.def == PreceptDefOf.IdeoRole_Moralist;
    }

    public override float CommonalityFor(Pawn pawn)
    {
        if (pawn.Map == null || !pawn.Position.IsValid || pawn.Ideo == null)
        {
            return 0f;
        }

        var precept_Role = pawn.Ideo.GetRole(pawn);

        return precept_Role == null || precept_Role.def != PreceptDefOf.IdeoRole_Moralist
            ? 0f
            : 10f * Mathf.Sqrt(pawn.GetStatValue(StatDefOf.SocialIdeoSpreadFrequencyFactor));
    }
}
