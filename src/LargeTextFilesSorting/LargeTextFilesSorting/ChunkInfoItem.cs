namespace LargeTextFilesSorting;

public sealed class ChunkInfoItem
{
    public string StringFilePath = null;
    public string NumberFilePath = null;

    public long StringFileLength = 0;
    public long NumberFileLength = 0;

    public long CountOfLinesInFile = 0;

    public StringNumberPart FirstPart;
    public StringNumberPart LastPart;

    public List<StringNumberPart> Buffer = new();

    public long AllLinesCount => CountOfLinesInFile + Buffer.Count;

    public long AllChunksLength => StringFileLength + NumberFileLength;
}