using System.Collections.Generic;

namespace LargeTextFilesSorting
{
    public sealed class StringNumberPartComparator : IComparer<StringNumberPart>
    {
        public int Compare(StringNumberPart x, StringNumberPart y)
        {
            var pairComparision = string.CompareOrdinal(x.StringPart, y.StringPart);
            if (pairComparision == 0)
            {
                pairComparision = string.CompareOrdinal(x.NumberPart, y.NumberPart);
            }
            return pairComparision;
        }
    }
}