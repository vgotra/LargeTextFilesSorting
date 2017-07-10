using System;
using System.IO;
using System.Text.RegularExpressions;

namespace LargeTextFilesSorting.Utils
{
    public class SortOrderChecher
    {
        private readonly string _filePathToCheck;
        private readonly StringNumberPartComparator _standardComparator = new StringNumberPartComparator();
        private static readonly Regex CorrectLinePattern = new Regex(@"^\d+\.{1}[ ]{1}[\w\s\-]+$", RegexOptions.Compiled);
        
        public SortOrderChecher(string filePathToCheck)
        {
            if (string.IsNullOrWhiteSpace(filePathToCheck))
            {
                throw new ArgumentNullException(nameof(filePathToCheck));
            }
            
            if (!File.Exists(filePathToCheck))
            {
                throw new FileNotFoundException("File doesn't exist", filePathToCheck);
            }

            _filePathToCheck = filePathToCheck;
        }

        public void CheckSortingOrder()
        {
            const int linesCountProgressToInform = 1000 * 1000 * 5;

            Console.WriteLine($"{DateTime.Now}. Started checking of output file for correct sorting.");

            int firstLineNumber = 1, secondLineNumber = 1;

            using (var fs = new FileStream(_filePathToCheck, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var firstLineReader = new StreamReader(fs))
                {
                    bool isWrongSorting = false;

                    string firstLine = firstLineReader.ReadLine();
                    string secondLine = null;
                    while (firstLine != null && (secondLine = firstLineReader.ReadLine()) != null)
                    {
                        secondLineNumber = firstLineNumber + 1;

                        if (!CorrectLinePattern.IsMatch(firstLine))
                        {
                            Console.WriteLine($"{DateTime.Now}. Wrong format of first line.");
                        }

                        if (!CorrectLinePattern.IsMatch(secondLine))
                        {
                            Console.WriteLine($"{DateTime.Now}. Wrong format of second line.");
                        }

                        var firstLinePart = SortingManager.LinePreProcessingFunc(firstLine);
                        var secondLinePart = SortingManager.LinePreProcessingFunc(secondLine);

                        var result = _standardComparator.Compare(firstLinePart, secondLinePart);

                        if (result > 0)
                        {
                            // it seems that we have problem with sorting
                            isWrongSorting = true;

                            Console.WriteLine($"{DateTime.Now}. Lines are in incorrect order.");
                            Console.WriteLine($"{DateTime.Now}. First line: '{firstLine}'. Line number: {firstLineNumber}");
                            Console.WriteLine($"{DateTime.Now}. Second line: '{secondLine}'. Line number: {secondLineNumber}");
                        }

                        if (secondLineNumber % linesCountProgressToInform == 0)
                        {
                            Console.WriteLine($"{DateTime.Now}. Checked {secondLineNumber} lines");
                        }

                        firstLine = secondLine;
                        firstLineNumber = secondLineNumber;
                    }

                    if (isWrongSorting)
                    {
                        Console.WriteLine($"{DateTime.Now}. Wrong sorting of lines or another problem.");
                    }
                    else
                    {
                        Console.WriteLine($"{DateTime.Now}. All lines sorted correctly and have correct output format.");
                    }
                }
            }

            Console.WriteLine($"{DateTime.Now}. Completed checking of output file for correct sorting.");
        }
    }
}
