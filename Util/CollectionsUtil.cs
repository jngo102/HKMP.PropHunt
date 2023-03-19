using System;
using System.Collections.Generic;

namespace PropHunt.Util
{
    /// <summary>
    /// Contains extension methods for collections.
    /// </summary>
    internal static class CollectionsUtil
    {
        private static readonly Random Random = new();

        /// <summary>
        /// Randomize the elements of a list.
        /// </summary>
        /// <typeparam name="T">The type of the list's contents</typeparam>
        /// <param name="list">The list whose elements are to be randomized.</param>
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }
    }
}
