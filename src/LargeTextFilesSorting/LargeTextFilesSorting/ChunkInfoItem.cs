using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LargeTextFilesSorting
{
    [DebuggerDisplay("CountOfLinesInFile={CountOfLinesInFile}")]
    public sealed class ChunkInfoItem
    {
        public string StringFilePath = null;
        public string NumberFilePath = null;

        public long StringFileLength = 0;
        public long NumberFileLength = 0;

        public long CountOfLinesInFile = 0;
       
        public StringNumberPart FirstPart;
        public StringNumberPart LastPart;

        public List<StringNumberPart> Buffer = new List<StringNumberPart>();

        public RangePlace IsInRange(StringNumberPart part, int allowedInBuffer, IComparer<StringNumberPart> comparer)
        {
            var compareToFirst = comparer.Compare(part, FirstPart);
            var compareToLast = comparer.Compare(part, LastPart);

            if ((compareToFirst == 0 && compareToLast < 0) || (compareToFirst < 0 && compareToLast < 0))
            {
                return RangePlace.FirstItem;
            }

            if (compareToFirst > 0 && compareToLast < 0)
            {
                return RangePlace.MiddleItem;
            }

            if ((compareToLast == 0 && compareToFirst > 0) || (compareToFirst > 0 && compareToLast > 0))
            {
                return RangePlace.LastItem;
            }
            
            throw new ArgumentOutOfRangeException($"Wrong part: compareToFirst {compareToFirst}, compareToLast {compareToLast}");
        }
        
        public long AllLinesCount { get { return CountOfLinesInFile + Buffer.Count; } }
        
        public long AllChunksLength { get { return StringFileLength + NumberFileLength; } }
    }
}