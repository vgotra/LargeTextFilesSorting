using System.Diagnostics;

namespace LargeTextFilesSorting
{
    [DebuggerDisplay("String={StringPart}, Number={NumberPart}")]
    public struct StringNumberPart
    {
        public StringNumberPart(string stringPart, string numberPart)
        {
            StringPart = stringPart;
            NumberPart = numberPart;
        }

        public string StringPart;
        public string NumberPart;
    }
}