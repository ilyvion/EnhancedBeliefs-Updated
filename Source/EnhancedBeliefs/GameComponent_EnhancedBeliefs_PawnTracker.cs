#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace EnhancedBeliefs;

public partial class GameComponent_EnhancedBeliefs
{
    public class PawnIdeoTracker : IEnumerable<KeyValuePair<Pawn, IdeoTrackerData>>
    {
#pragma warning disable IDE0028 // Simplify collection initialization
        public ConditionalWeakTable<Pawn, IdeoTrackerData> pawnIdeoTrackerData = new();
#pragma warning restore IDE0028 // Simplify collection initialization

        public IdeoTrackerData AddIdeoTrackerToPawn(Pawn pawn)
        {
            IdeoTrackerData data = new(pawn);
            pawnIdeoTrackerData.Add(pawn, data);
            return data;
        }

        public void SetIdeoTracker(Pawn pawn, IdeoTrackerData data)
        {
            if (data == null)
            {
                _ = pawnIdeoTrackerData.Remove(pawn);
            }
            else
            {
                if (data.pawn != pawn)
                {
                    Log.Error($"Tried to set IdeoTrackerData for pawn {pawn} but the data is for pawn {data.pawn}. This should not happen.");
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
        public void ForEach(System.Action<KeyValuePair<Pawn, IdeoTrackerData>> action)
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

        public IEnumerator<KeyValuePair<Pawn, IdeoTrackerData>> GetEnumerator()
        {
            var enumerable = (IEnumerable<KeyValuePair<Pawn, IdeoTrackerData>>)pawnIdeoTrackerData;
            //var tmpList = pawnIdeoTrackerData.Select(kvp => kvp).ToList();
            return enumerable.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
