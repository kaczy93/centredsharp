namespace CentrED.Utils;

public static class Util
{
    public static T GetRandom<T>(this ICollection<T> collection)
    {
        return collection.ElementAt(Random.Shared.Next(collection.Count));
    }
}