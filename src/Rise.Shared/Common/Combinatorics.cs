namespace Rise.Shared.Common;

/// <summary>
/// Combinatorial helpers for room search and recommendations.
/// </summary>
public static class Combinatorics
{
    /// <summary>
    /// Generates all distinct combinations of length <paramref name="length"/> from the input list.
    /// </summary>
    public static List<List<T>> GetCombinations<T>(IReadOnlyList<T> list, int length)
    {
        if (length == 0)
            return [[]];

        var result = new List<List<T>>();
        for (var i = 0; i <= list.Count - length; i++)
        {
            var head = list[i];
            var tail = list.Skip(i + 1).ToList();
            foreach (var sub in GetCombinations(tail, length - 1))
            {
                result.Add([head, .. sub]);
            }
        }
        return result;
    }
}
