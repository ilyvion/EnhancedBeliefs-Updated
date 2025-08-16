using System.Diagnostics.CodeAnalysis;
#if !v1_5
using System.Runtime.CompilerServices;
#endif

namespace EnhancedBeliefs;

internal partial class GameComponent_EnhancedBeliefs
{
    internal sealed class PawnIdeoTracker : IEnumerable<KeyValuePair<Pawn, IdeoTrackerData>>
    {
#pragma warning disable IDE0028 // Simplify collection initialization
        private readonly ConditionalWeakTable<Pawn, IdeoTrackerData> pawnIdeoTrackerData = new();
#pragma warning restore IDE0028 // Simplify collection initialization

        public IdeoTrackerData AddIdeoTrackerToPawn(Pawn pawn)
        {
            IdeoTrackerData data = new(pawn);
            pawnIdeoTrackerData.Add(pawn, data);
            return data;
        }

        public void SetIdeoTracker(Pawn pawn, IdeoTrackerData? data)
        {
            if (data == null)
            {
                _ = pawnIdeoTrackerData.Remove(pawn);
            }
            else
            {
                if (data.Pawn != pawn)
                {
                    EnhancedBeliefsMod.Error($"Tried to set IdeoTrackerData for pawn {pawn} but the data is for pawn {data.Pawn}. This should not happen.");
                    return;
                }
                pawnIdeoTrackerData.AddOrUpdate(pawn, data);
            }
        }

        public IdeoTrackerData EnsurePawnHasIdeoTracker(Pawn pawn)
        {
            return pawnIdeoTrackerData.TryGetValue(pawn, out var tracker) ? tracker : AddIdeoTrackerToPawn(pawn);
        }

        public bool TryGetIdeoTracker(Pawn pawn, [NotNullWhen(true)] out IdeoTrackerData? ideoTracker)
        {
            return pawnIdeoTrackerData.TryGetValue(pawn, out ideoTracker);
        }

        public IdeoTrackerData? TryGetIdeoTracker(Pawn pawn)
        {
            return pawnIdeoTrackerData.TryGetValue(pawn, out var ideoTracker) ? ideoTracker : null;
        }

        private readonly List<KeyValuePair<Pawn, IdeoTrackerData>> _tmpForEachList = [];
        public void ForEach(Action<KeyValuePair<Pawn, IdeoTrackerData>> action)
        {
            _tmpForEachList.AddRange(pawnIdeoTrackerData.Select(kvp => kvp));
            try
            {
                foreach (var pawnIdeoTracker in _tmpForEachList)
                {
                    action(pawnIdeoTracker);
                }
            }
            finally
            {
                _tmpForEachList.Clear();
            }
        }

        public bool RemoveTracker(Pawn pawn)
        {
            return pawnIdeoTrackerData.Remove(pawn);
        }

#pragma warning disable CA1859
        public IEnumerator<KeyValuePair<Pawn, IdeoTrackerData>> GetEnumerator()
        {
            var enumerable = (IEnumerable<KeyValuePair<Pawn, IdeoTrackerData>>)pawnIdeoTrackerData;
            return enumerable.GetEnumerator();
        }
#pragma warning restore CA1859

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
