using System;
using System.IO;

namespace LargeTextFilesSorting.Utils
{
    public class TestFileGenerator
    {
        public void GenerateInputFile(TestFileSize testFileSize)
        {
            const int countOfDuplicateString = 1;
            const int linesCountProgressToInform = 1000 * 1000 * 5;
            
            var needTotalLinesCount = GetCountOfLines(testFileSize);

            var standardLines = needTotalLinesCount * (10 - countOfDuplicateString) / 10;
            var duplicateLines = (needTotalLinesCount - standardLines) / countOfDuplicateString;

            Console.WriteLine($"{DateTime.Now}. File generator: Selected size: {testFileSize}, standard lines count: {standardLines}, duplicate lines count: {duplicateLines * countOfDuplicateString}");

            long countOfLines = 0;

            Console.WriteLine($"{DateTime.Now}. File generator: Started creation of test file with name: '{DefaultValues.InputFileName}'");

            var r = new Random();
            using (var fs = new FileStream(DefaultValues.InputFileName, FileMode.Create))
            using (var sw = new StreamWriter(fs))
            {
                for (var i = 0; i < standardLines; i++)
                {
                    var s = GetText(r);
                    var line = $"{r.Next(1, DefaultValues.MaxNumberValue)}. {s}";
                    sw.WriteLine(line);

                    countOfLines++;

                    if (countOfLines % linesCountProgressToInform == 0)
                    {
                        Console.WriteLine($"{DateTime.Now}. File generator: Generated count of lines: {countOfLines}");
                    }
                }

                for (var c = 0; c < countOfDuplicateString; c++)
                {
                    var duplicateLine = GetText(r);

                    Console.WriteLine($"{DateTime.Now}. File generator: Will be generated duplicate line: {duplicateLine}");

                    for (var i = 0; i < duplicateLines; i++)
                    {
                        var line = $"{r.Next(1, DefaultValues.MaxNumberValue)}. {duplicateLine}";
                        sw.WriteLine(line);

                        countOfLines++;

                        if (countOfLines % linesCountProgressToInform == 0)
                        {
                            Console.WriteLine($"{DateTime.Now}. File generator: Generated count of lines: {countOfLines}");
                        }
                    }
                }
            }

            var f = new FileInfo(DefaultValues.InputFileName);
            
            Console.WriteLine($"{DateTime.Now}. File generator: Completed creation of test file with name: '{DefaultValues.InputFileName}'. Total count of lines: {countOfLines:0.}, filesize: {f.Length} bytes.");
            Console.WriteLine();
        }

        private static long GetCountOfLines(TestFileSize testFileSize)
        {
            switch (testFileSize)
            {
                case TestFileSize.Gb1:
                    return 1024L * 1024 * 1024 / DefaultValues.AvgLineLengthForGenerator;
                case TestFileSize.Gb10:
                    return 1024L * 1024 * 1024 * 10 / DefaultValues.AvgLineLengthForGenerator;
                case TestFileSize.Gb50:
                    return 1024L * 1024 * 1024 * 50 / DefaultValues.AvgLineLengthForGenerator;
                case TestFileSize.Gb100:
                    return 1024L * 1024 * 1024 * 100 / DefaultValues.AvgLineLengthForGenerator;
                case TestFileSize.Mb100:
                default:
                    return 1024L * 1024 * 100 / DefaultValues.AvgLineLengthForGenerator;
            }
        }
        
        private static string GetText(Random ran)
        {
            var run = ran.Next(DefaultValues.MinCharsInStringPart, DefaultValues.MaxCharsInStringPart);

            var st = "" + DefaultValues.UpperChars[ran.Next(0, DefaultValues.UpperChars.Length)];

            for (var i = 1; i < run; i++)
            {
                st = st + DefaultValues.LowerChars[ran.Next(0, DefaultValues.LowerChars.Length)];
            }

            return st;
        }
    }
}