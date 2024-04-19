using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace EnhancedBeliefs
{
    // Bootleg solution because prepatcher will scare off workshop dummies
    [HarmonyPatch(typeof(Pawn_IdeoTracker), MethodType.Constructor, new Type[] { typeof(Pawn) })]
    public static class IdeoTracker_Constructor
    {
        public static void Postfix(Pawn_IdeoTracker __instance)
        {
            if (Find.World == null)
            {
                Log.Error("Pawn setup attempted with null World!");
                return;
            }

            Find.World.GetComponent<EnhancedBeliefs_WorldComp>().AddTracker(__instance);
        }
    }

    [HarmonyPatch(typeof(Ideo), MethodType.Constructor)]
    public static class Ideo_Constructor
    {
        public static void Postfix(Ideo __instance)
        {
            if (Find.World == null)
            {
                Log.Error("Ideo setup attempted with null World!");
                return;
            }

            Find.World.GetComponent<EnhancedBeliefs_WorldComp>().AddIdeoTracker(__instance);
        }
    }

    [HarmonyPatch(typeof(Pawn_IdeoTracker), nameof(Pawn_IdeoTracker.CertaintyChangePerDay), MethodType.Getter)]
    public static class IdeoTracker_CertaintyChange
    {
        public static bool Prefix(Pawn_IdeoTracker __instance, ref float __result)
        {
            // WTF
            if (Find.World == null)
            {
                return true;
            }

            __result = 0;

            Pawn pawn = __instance.pawn;
            EnhancedBeliefs_WorldComp comp = Find.World.GetComponent<EnhancedBeliefs_WorldComp>();
            IdeoTrackerData data = comp.pawnTrackerData[pawn];

            // Certainty only starts decreasing at moods below stellar and after 3 days of lacking positive precept moodlets
            if (pawn.needs.mood.CurLevelPercentage < 0.8 && Find.TickManager.TicksGame - data.lastPositiveThoughtTick > 180000f)
            {
                __result -= EnhancedBeliefs_WorldComp.CertaintyLossFromInactivity.Evaluate((Find.TickManager.TicksGame - data.lastPositiveThoughtTick) / 60000f);
            }

            // 4 recaches per second should be enough
            if (data.cachedCertaintyChange == -9999f || pawn.IsHashIntervalTick(15))
            {
                data.CertaintyChangeRecache(comp);
            }

            __result += data.cachedCertaintyChange;

            if (__result > 0)
            {
                data.lastPositiveThoughtTick = Find.TickManager.TicksGame;
            }

            return false;
        }
    }

    // Another bootleg tracker, no idea why Tynan didn't implement it in vanilla considering the amount of work and performance it would've saved him
    // Smh, backseat coding
    [HarmonyPatch(typeof(Pawn_IdeoTracker), nameof(Pawn_IdeoTracker.SetIdeo))]
    public static class IdeoTracker_SetIdeo
    {
        public static void Postfix(Pawn_IdeoTracker __instance, Ideo ideo)
        {
            Find.World?.GetComponent<EnhancedBeliefs_WorldComp>().SetIdeo(__instance.pawn, ideo);
        }
    }

    [HarmonyPatch(typeof(IdeoDevelopmentTracker), nameof(IdeoDevelopmentTracker.Notify_Reformed))]
    public static class FluidIdeoTracker_Reformed
    {
        public static void Postfix(IdeoDevelopmentTracker __instance)
        {
            Find.World?.GetComponent<EnhancedBeliefs_WorldComp>().FluidIdeoRecache(__instance.ideo);
        }
    }
}
