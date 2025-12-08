namespace CentrED.Utils;

public static class Util
{
    public static T? GetRandom<T>(this ICollection<T> collection) where T : struct
    {
        if (collection.Count == 0)
            return null;
        return collection.ElementAt(Random.Shared.Next(collection.Count));
    }
}