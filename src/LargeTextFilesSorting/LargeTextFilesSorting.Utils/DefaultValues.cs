namespace LargeTextFilesSorting.Utils;

public static class DefaultValues
{
    public const string InputFileName = "test.txt";
    public const string OutputFileName = "test.output.txt";

    public const int MinCharsInStringPart = 10;
    public const int MaxCharsInStringPart = 30;
    public const int MaxNumberValue = 50000;

    private const long Gb10 = 1024L * 1024 * 1024 * 10;
    private const long Gb100 = 1024L * 1024 * 1024 * 100;

    public static char[] LowerChars { get; } = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ -".ToCharArray();

    public static char[] UpperChars { get; } = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ -".ToCharArray();

    public static readonly int AvgLineLengthForGenerator = MaxNumberValue.ToString().Length + 2 + (MinCharsInStringPart + MaxCharsInStringPart) / 2 + 2; //number part, separators, string part, end of line
        
    public static readonly int AvgLineLengthForTest = MaxNumberValue.ToString().Length + (MinCharsInStringPart + MaxCharsInStringPart) / 2;
    public static readonly long Gb10PossibleLinesCount = Gb10 / AvgLineLengthForTest;
    public static readonly long Gb100PossibleLinesCount = Gb100 / AvgLineLengthForTest;
}