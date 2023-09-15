using System;
using System.IO;
using System.Linq;
using LargeTextFilesSorting.Utils;
using Mono.Options;

namespace LargeTextFilesSorting.Console;

public static class Program
{
    private static bool _generateFile;
    private static TestFileSize _testFileSize = TestFileSize.Mb100;
    private static bool _runPerformanceTest;
    private static bool _runSorting;
    private static string _sortingFilePath;
    private static bool _showHelp;
    private static bool _runCheck;
    private static string _checkingFilePath;

    private static readonly OptionSet Options = new()
    {
        {
            "t|perftest", "Run basic performance tests.", _ =>
            {
                _runPerformanceTest = true;
                System.Console.WriteLine("Selected performance test.");
            }
        },
        {
            "g|generate:", $"Generate test file with specified file size (possible sizes: {Enum.GetNames(typeof(TestFileSize)).Aggregate((f,s) => $"{f}, {s}")}). By default 100Mb will be used.", g =>
            {
                _generateFile = true;
                _testFileSize = ParseTestFileSize(g);
                System.Console.WriteLine($"Selected generation of test file. Selected size of test file: {_testFileSize}");
            }
        },
        {
            "r|run:", "Run sorting of specified file. Without path of file 'test.txt' will be used", rs =>
            {
                _runSorting = true;
                _sortingFilePath = string.IsNullOrWhiteSpace(rs) ? DefaultValues.InputFileName : rs;
                    
                System.Console.WriteLine($"Selected sorting. Selected name of test file: {_sortingFilePath}");
            }
        },
        {
            "c|check:", "Run checking of output file for correct sorting", rs =>
            {
                _runCheck = true;
                _checkingFilePath = string.IsNullOrWhiteSpace(rs) ? GetOutputFileName(DefaultValues.InputFileName) : rs;

                System.Console.WriteLine($"Selected checking. Selected name of test file: {_checkingFilePath}");
            }
        },
        {
            "h|help", "Show help and exit", h => _showHelp = h != null
        }
    };

    private static void Main(string[] args)
    {
        if (args == null || !args.Any())
        {
            ShowHelp();
            return;
        }
            
        try
        {
            Options.Parse(args);
        }
        catch (OptionException e)
        {
            System.Console.WriteLine($"Error during parsing input arguments: {e}");
            ShowHelp();
            return;
        }

        try
        {
            if (_runPerformanceTest)
            {
                var pt = new PerfTest();
                PerfTest.RunMemoryTest();
                System.Console.WriteLine();
                pt.RunFileSystemTest();
            }

            if (_generateFile)
            {
                System.Console.WriteLine();
                new TestFileGenerator().GenerateInputFile(_testFileSize);
            }

            if (_runSorting)
            {
                System.Console.WriteLine();
                new SortingManager(_sortingFilePath, GetOutputFileName(_sortingFilePath)).ProcessFile();
            }

            if (_runCheck)
            {
                System.Console.WriteLine();
                new SortOrderChecher(_checkingFilePath).CheckSortingOrder();
            }
        }
        catch (Exception e)
        {
            System.Console.WriteLine($"Error during execution: {e}");
            return;
        }

        if (_showHelp)
        {
            ShowHelp();
        }
    }

    private static void ShowHelp()
    {
        System.Console.WriteLine("Usage: LargeTestFilesSorting.Console.exe [OPTIONS]");
        System.Console.WriteLine();

        System.Console.WriteLine("Options:");
        Options.WriteOptionDescriptions(System.Console.Out);
    }

    private static TestFileSize ParseTestFileSize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return TestFileSize.Mb100;
        }

        TestFileSize testFileSize;
        if (Enum.TryParse(value, true, out testFileSize) && Enum.IsDefined(typeof(TestFileSize), testFileSize))
        {
            return testFileSize;
        }

        return TestFileSize.Mb100;
    }

    private static string GetOutputFileName(string inputFilePath)
    {
        if (string.IsNullOrWhiteSpace(inputFilePath))
        {
            throw new ArgumentNullException(nameof(inputFilePath));
        }

        return Path.Combine(Path.GetDirectoryName(inputFilePath), $"{Path.GetFileNameWithoutExtension(inputFilePath)}.output.txt");
    }
}