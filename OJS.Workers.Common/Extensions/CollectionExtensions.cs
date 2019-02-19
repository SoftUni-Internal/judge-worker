namespace OJS.Workers.Common.Extensions
{
    using System.Collections.Generic;

    public static class CollectionExtensions
    {
        public static void AddRange<T>(this ICollection<T> destination, IEnumerable<T> source)
            => source.ForEach(destination.Add);
    }
}
