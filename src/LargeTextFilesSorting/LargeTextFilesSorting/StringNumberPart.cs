namespace LargeTextFilesSorting;

public struct StringNumberPart
{
    public StringNumberPart(string stringPart, string numberPart)
    {
        StringPart = stringPart;
        NumberPart = numberPart;
    }

    public readonly string StringPart;
    public readonly string NumberPart;
}