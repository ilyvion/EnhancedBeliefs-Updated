using System.Reflection;

namespace EnhancedBeliefs;

internal sealed class EnhancedBeliefsMod : Mod
{
    public EnhancedBeliefsMod(ModContentPack content) : base(content)
    {
        var harmony = new Harmony(content.PackageId);
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }
}
