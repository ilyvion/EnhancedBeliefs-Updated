namespace EnhancedBeliefs;

internal static class EnhancedBeliefsUtilities
{
    internal static List<T> TryGetComps<T>(this Precept precept) where T : PreceptComp
    {
        return precept.def.TryGetComps<T>();
    }

    internal static List<T> TryGetComps<T>(this PreceptDef precept) where T : PreceptComp
    {
        List<T> comps = [];

        foreach (var preceptComp in precept.comps)
        {
            if (preceptComp is T comp)
            {
                comps.Add(comp);
            }
        }

        return comps;
    }
}
