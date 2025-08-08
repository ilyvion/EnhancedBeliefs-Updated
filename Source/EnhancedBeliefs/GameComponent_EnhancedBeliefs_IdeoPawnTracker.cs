using System.Diagnostics.CodeAnalysis;
#if !v1_5
using System.Runtime.CompilerServices;
#endif

namespace EnhancedBeliefs;

internal partial class GameComponent_EnhancedBeliefs
{
    internal sealed class IdeoPawnTracker : IEnumerable<KeyValuePair<Ideo, List<Pawn>>>
    {
#pragma warning disable IDE0028 // Simplify collection initialization
        public ConditionalWeakTable<Ideo, List<Pawn>> ideoPawnsTracker = new();
#pragma warning restore IDE0028 // Simplify collection initialization

        public List<Pawn> AddPawnTrackerToIdeo(Ideo ideo)
        {
            List<Pawn> list = [];
            ideoPawnsTracker.Add(ideo, list);
            return list;
        }

        public List<Pawn> EnsureIdeoHasPawnTracker(Ideo ideo)
        {
            return ideoPawnsTracker.TryGetValue(ideo, out var tracker) ? tracker : AddPawnTrackerToIdeo(ideo);
        }

        public bool TryGetPawnTracker(Ideo ideo, [NotNullWhen(true)] out List<Pawn>? pawnList)
        {
            return ideoPawnsTracker.TryGetValue(ideo, out pawnList);
        }

        public void EnsureIdeoPawnTrackerHasPawn(Ideo ideo, Pawn pawn)
        {
            var pawnList = EnsureIdeoHasPawnTracker(ideo);
            pawnList.Add(pawn);
        }

        public bool RemovePawnFromIdeoPawnTracker(Ideo ideo, Pawn pawn)
        {
            return pawn != null
                && pawn.ideo != null
                && ideoPawnsTracker.TryGetValue(ideo, out var pawnList)
                && pawnList.Remove(pawn);
        }

        public bool ContainsIdeo(Ideo ideo)
        {
            return ideoPawnsTracker.Any(kvp => kvp.Key == ideo);
        }

        private readonly List<KeyValuePair<Ideo, List<Pawn>>> _tmpForEachList = [];
        public void ForEach(Action<KeyValuePair<Ideo, List<Pawn>>> action)
        {
            _tmpForEachList.AddRange(ideoPawnsTracker.Select(kvp => kvp));
            try
            {
                foreach (var ideoPawnList in _tmpForEachList)
                {
                    action(ideoPawnList);
                }
            }
            finally
            {
                _tmpForEachList.Clear();
            }
        }

#pragma warning disable CA1859
        public IEnumerator<KeyValuePair<Ideo, List<Pawn>>> GetEnumerator()
        {
            var enumerable = (IEnumerable<KeyValuePair<Ideo, List<Pawn>>>)ideoPawnsTracker;
            return enumerable.GetEnumerator();
        }
#pragma warning restore CA1859

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
