using System.Collections.Generic;
using System.Linq;

namespace TRM.Web.Extentions
{
    public static class ArrayExtentions
    {
        public static string ToCommaSeparatedString<T>(this IEnumerable<T> input)
        {
            if (input == null) return string.Empty;

            return string.Join(",", input);
        }

        public static List<List<T>> GetChunks<T>(this IEnumerable<T> source, int chunkSize) 
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }
    }
}