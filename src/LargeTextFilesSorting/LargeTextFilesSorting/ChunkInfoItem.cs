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
        
        public long AllLinesCount { get { return CountOfLinesInFile + Buffer.Count; } }
        
        public long AllChunksLength { get { return StringFileLength + NumberFileLength; } }
    }
}