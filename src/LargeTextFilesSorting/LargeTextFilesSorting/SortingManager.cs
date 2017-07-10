using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace LargeTextFilesSorting
{
    public sealed class SortingManager
    {
        private readonly StringNumberPartComparator _comparer = new StringNumberPartComparator();

        // Do not change it to bigger size without checking code thinking - possible outofmemory exception on creation of very big buffer for sorting for output file
        // tested that 32mb in memory chunks will not help much (maybe only in case of usage of DDR4 or other faster memory types) - the most optimal cases - from 4 to 8 mb - but such cases will touch IO (a lot of chunks on file system)
        // anyway - we don't have a silver bullet here - so for files with less file size we will use less size of memory chunks (faster sorting) - for 10 gb - the most optimal case from 4 to 8 Mb
        private const int AllowedCountOfFileLinesToSortInMemory = 1024 * 1024 * 4;
        private const int AllowedMaxCountOfLinesInAllBuffers = AllowedCountOfFileLinesToSortInMemory * 4;

        // needed for spliting file to chunks and temporary results
        private const int FreeSpaceMultiplier = 3;

        private readonly long _initialFileLength;
        private readonly string _inputFilePath;
        private readonly string _outputFilePath;
        private readonly string _tempFolderForChunksPath;

        private readonly LinkedList<ChunkInfoItem> _linkedlist = new LinkedList<ChunkInfoItem>();
        private readonly List<ChunkInfoItem> _listOfCurrentItemsToProcess = new List<ChunkInfoItem>();

        private long _initialCountOfLines;
        private long _outputCountOfLines;

        public SortingManager(string inputFilePath, string outputFilePath)
        {
            if (string.IsNullOrWhiteSpace(inputFilePath))
            {
                throw new ArgumentNullException(nameof(inputFilePath));
            }

            if (!File.Exists(inputFilePath))
            {
                throw new FileNotFoundException("File was not found", inputFilePath);
            }

            _inputFilePath = inputFilePath;

            if (string.IsNullOrWhiteSpace(outputFilePath))
            {
                throw new ArgumentNullException(nameof(outputFilePath));
            }

            _outputFilePath = outputFilePath;

            var fi = new FileInfo(_inputFilePath);
            _initialFileLength = fi.Length;

            var di = new DriveInfo(Path.GetPathRoot(fi.FullName));
            if (di.AvailableFreeSpace < _initialFileLength * FreeSpaceMultiplier)
            {
                throw new Exception($"There no enough free space ({_initialFileLength * FreeSpaceMultiplier} bytes) at drive {di.Name}");
            }

            _tempFolderForChunksPath = Path.Combine(fi.Directory.FullName, "Chunks");
        }

        public void ProcessFile()
        {
            try
            {
                Console.WriteLine($"{DateTime.Now}. Creating temp directory for chunks");

                if (Directory.Exists(_tempFolderForChunksPath))
                {
                    Directory.Delete(_tempFolderForChunksPath, true);
                }

                Directory.CreateDirectory(_tempFolderForChunksPath);
            }
            catch (Exception)
            {
                Console.WriteLine($"{DateTime.Now}. Cannot create temp directory for chunks. Exiting");
                throw;
            }

            var sw = new Stopwatch();

            try
            {
                sw.Start();

                Console.WriteLine($"{DateTime.Now}. Started processing file: {_inputFilePath} with initial size: {_initialFileLength} bytes");

                IterateInputFileAndBuildChunks();

                Console.WriteLine($"{DateTime.Now}. Initial count of lines: {_initialCountOfLines}, initial length: {_initialFileLength} bytes");
                Console.WriteLine($"{DateTime.Now}. Initial count of lines in chunks: {_linkedlist.Sum(x => x.AllLinesCount)}, initial length of chunks: {_linkedlist.Sum(x => x.AllChunksLength)} bytes");
                Console.WriteLine($"{DateTime.Now}. Started merging into output file");

                JoinChunksToOutputFile();

                var outputBytes = new FileInfo(_outputFilePath).Length;
                Console.WriteLine($"{DateTime.Now}. After merge. Count of lines: {_outputCountOfLines}, length of output file: {outputBytes} bytes");

                // _inputFilePath.Length + 2 == outputBytes - this check for cases when we don't have end line in input file but we will use WriteLine for output file - so we will have end of line for sure in output file
                if (_outputCountOfLines == _initialCountOfLines && (_initialFileLength == outputBytes || _initialFileLength + 2 == outputBytes))
                {
                    Console.WriteLine($"{DateTime.Now}. Checksums of count of lines and file length are equal");
                }
                else
                {
                    Console.WriteLine($"{DateTime.Now}. !!! Checksums of count of lines and file length are not equal");
                }

                sw.Stop();

                Console.WriteLine($"{DateTime.Now}. Completed processing file to output file: {_outputFilePath}. Time spent: {sw.Elapsed.TotalSeconds} sec");
            }
            catch (Exception e)
            {
                Console.WriteLine($"{DateTime.Now}. Cannot complete processing file.");
                Console.WriteLine($"Error: {e.ToString()}");
            }
            finally
            {
                try
                {
                    if (Directory.Exists(_tempFolderForChunksPath))
                    {
                        Console.WriteLine($"{DateTime.Now}. Removing temporary chunks");
                        Directory.Delete(_tempFolderForChunksPath, true);
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine($"{DateTime.Now}. Cannot remove temporary chunks");
                    throw;
                }
            }
        }

        private void IterateInputFileAndBuildChunks()
        {
            const int linesCountProgressToInform = 1000 * 1000 * 5;

            Console.WriteLine($"{DateTime.Now}. Started splitting of initial file to initial string chunks.");
          
            StreamReader streamReader = null;

            try
            {
                streamReader = new StreamReader(_inputFilePath);

                // read and sort first chunk 
                var item = ReadAndSortInitialChunk(streamReader);
                _linkedlist.AddFirst(item);

                if (_initialCountOfLines < AllowedCountOfFileLinesToSortInMemory)
                {
                    // all lines already sorted (string and number parts with ordinal order) - close reader in finally and return to main cycle
                    return;
                }
                
                // if we have more lines - continue processing
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    _initialCountOfLines++;

                    var linePart = LinePreProcessingFunc(line);
                    
                    ProcessInitialLinePart(linePart);

                    // check counter for chunks and process buffers if we need it 
                    if (ShouldStartProcessingOfMemoryData())
                    {
                        ProcessCurrentItems();
                    }

                    if (_initialCountOfLines % linesCountProgressToInform == 0)
                    {
                        Console.WriteLine($"{DateTime.Now}. Processed count of initial lines: {_initialCountOfLines}");
                    }
                }

                // for some items which left in buffer
                ProcessCurrentItems();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                streamReader?.Dispose();
            }
            
            Console.WriteLine($"{DateTime.Now}. Completed splitting of initial file to initial string chunks.");
        }

        private void ProcessCurrentItems()
        {
            // there possible improvement for parallel processing 
            foreach (var treeItemToProcess in _listOfCurrentItemsToProcess)
            {
                if (treeItemToProcess.CountOfLinesInFile == 0)
                {
                    // new chunk - sort - write
                    ProcessNewChunk(treeItemToProcess);
                }
                else
                {
                    // additional chunk - find node - read old - join lines - sort - split to 2 chunks, write and update linked list
                    ProcessExistingChunkWithBuffer(treeItemToProcess);
                }
            }

            ProcessWrongOrderOfChunks();

            _listOfCurrentItemsToProcess.Clear();
        }

        private ChunkInfoItem ReadAndSortInitialChunk(StreamReader streamReader)
        {
            // we don't pad number lines because length can be changed later 
            if (streamReader == null)
            {
                throw new ArgumentNullException(nameof(streamReader));
            }

            var item = new ChunkInfoItem();

            string line;
            while ((line = streamReader.ReadLine()) != null)
            {
                _initialCountOfLines++;

                var linePart = LinePreProcessingFunc(line);
                item.Buffer.Add(linePart);

                if (item.Buffer.Count >= AllowedCountOfFileLinesToSortInMemory)
                {
                    break;
                }
            }

            item.Buffer.Sort(_comparer);

            item.FirstPart = item.Buffer.FirstOrDefault();
            item.LastPart = item.Buffer.LastOrDefault();
            item.CountOfLinesInFile = item.Buffer.Count;

            WriteBufferToFile(item);

            return item;
        }

        private void WriteBufferToFile(ChunkInfoItem item)
        {
            item.StringFilePath = Path.Combine(_tempFolderForChunksPath, Guid.NewGuid().ToString());
            item.NumberFilePath = Path.Combine(_tempFolderForChunksPath, Guid.NewGuid().ToString());

            using (var stringWriter = new StreamWriter(item.StringFilePath))
            {
                using (var numberWriter = new StreamWriter(item.NumberFilePath))
                {
                    foreach (var pair in item.Buffer)
                    {
                        stringWriter.WriteLine(pair.StringPart);
                        numberWriter.WriteLine(pair.NumberPart);
                    }
                }
            }

            item.StringFileLength = new FileInfo(item.StringFilePath).Length;
            item.NumberFileLength = new FileInfo(item.NumberFilePath).Length;
            item.Buffer.Clear();
        }

        private void ProcessInitialLinePart(StringNumberPart linePart)
        {
            var node = _linkedlist.First;

            while (node != null)
            {
                var chunkInfoItem = node.Value;

                var compareToFirst = _comparer.Compare(linePart, chunkInfoItem.FirstPart);
                var compareToLast = _comparer.Compare(linePart, chunkInfoItem.LastPart);

                //if (compareToFirst < 0 && compareToLast < 0)
                //{
                //    // add node before
                //    var newBeforeChunkInfo = new ChunkInfoItem();
                //    newBeforeChunkInfo.Buffer.Add(linePart);
                //    newBeforeChunkInfo.FirstPart = linePart;
                //    newBeforeChunkInfo.LastPart = linePart;

                //    _linkedlist.AddBefore(node, newBeforeChunkInfo);
                //    AddCurrentItemIfNotExist(newBeforeChunkInfo);
                //    return;
                //}

                //if (compareToFirst == 0 && compareToLast < 0)
                //{
                //    chunkInfoItem.Buffer.Add(linePart);
                //    chunkInfoItem.FirstPart = linePart;
                //    AddCurrentItemIfNotExist(chunkInfoItem);
                //    return;
                //}

                // will not create a lot of small chunks with unsorted data(more time to sort and merge) - hard to say - what will be better - depends on input data
                if ((compareToFirst == 0 && compareToLast < 0) || (compareToFirst < 0 && compareToLast < 0))
                {
                    chunkInfoItem.Buffer.Add(linePart);
                    chunkInfoItem.FirstPart = linePart;
                    AddCurrentItemIfNotExist(chunkInfoItem);
                    return;
                }

                // item in 
                if (compareToFirst > 0 && compareToLast < 0)
                {
                    chunkInfoItem.Buffer.Add(linePart);
                    AddCurrentItemIfNotExist(chunkInfoItem);
                    return;
                }

                if (compareToLast == 0 && compareToFirst > 0)
                {
                    chunkInfoItem.Buffer.Add(linePart);
                    chunkInfoItem.LastPart = linePart;
                    AddCurrentItemIfNotExist(chunkInfoItem);
                    return;
                }

                // this will help to rechecking and resorting already sorted chunks 
                if (compareToFirst > 0 && compareToLast > 0)
                {
                    if (node.Next == null)
                    {
                        // end of list - last node
                        var newAfterChunkInfo = new ChunkInfoItem();
                        newAfterChunkInfo.Buffer.Add(linePart);
                        newAfterChunkInfo.FirstPart = linePart;
                        newAfterChunkInfo.LastPart = linePart;

                        _linkedlist.AddAfter(node, newAfterChunkInfo);
                        AddCurrentItemIfNotExist(newAfterChunkInfo);
                        return;
                    }
                    else
                    {
                        // check next node
                        node = node.Next;
                        continue;
                    }
                }
                
                node = node.Next;
            }
        }

        private bool ShouldStartProcessingOfMemoryData()
        {
            return _listOfCurrentItemsToProcess.Sum(x => x.Buffer.Count) >= AllowedMaxCountOfLinesInAllBuffers || _listOfCurrentItemsToProcess.Max(x => x.Buffer.Count) >= AllowedCountOfFileLinesToSortInMemory;
        }

        private void AddCurrentItemIfNotExist(ChunkInfoItem chunkInfoItem)
        {
            if (!_listOfCurrentItemsToProcess.Contains(chunkInfoItem))
            {
                _listOfCurrentItemsToProcess.Add(chunkInfoItem);
            }
        }

        private void ProcessWrongOrderOfChunks()
        {
            var first = _linkedlist.First;
            var second = first.Next;

            while (second != null)
            {
                if (_comparer.Compare(first.Value.LastPart, second.Value.FirstPart) > 0)
                {
                    first = SortChunks(first, second);
                }
                else
                {
                    first = second;
                }

                second = first.Next;
            }
        }

        private void ProcessNewChunk(ChunkInfoItem treeItemToProcess)
        {
            // new chunk - sort - write
            treeItemToProcess.Buffer.Sort(_comparer);

            treeItemToProcess.FirstPart = treeItemToProcess.Buffer.FirstOrDefault();
            treeItemToProcess.LastPart = treeItemToProcess.Buffer.LastOrDefault();
            treeItemToProcess.CountOfLinesInFile = treeItemToProcess.Buffer.Count;

            WriteBufferToFile(treeItemToProcess);
        }

        private void ProcessExistingChunkWithBuffer(ChunkInfoItem treeItemToProcess)
        {
            // additional chunk - find node - read old - join lines - sort - split to 2 chunks, write and update linked list
            var node = _linkedlist.Find(treeItemToProcess);
            if (node == null)
            {
                throw new InvalidOperationException("Cannot find mandatory node");
            }

            var parts = new List<StringNumberPart>((int)treeItemToProcess.CountOfLinesInFile + treeItemToProcess.Buffer.Count);

            using (var stringReader = new StreamReader(treeItemToProcess.StringFilePath))
            {
                using (var numberReader = new StreamReader(treeItemToProcess.NumberFilePath))
                {
                    string stringLine;
                    while ((stringLine = stringReader.ReadLine()) != null)
                    {
                        var numberLine = numberReader.ReadLine();
                        parts.Add(new StringNumberPart(stringLine, numberLine));
                    }
                }
            }

            parts.AddRange(treeItemToProcess.Buffer);
            treeItemToProcess.Buffer.Clear();

            parts.Sort(_comparer);

            var firstPartOfList = parts.Count / 2;
            var lastPartOfList = parts.Count - firstPartOfList;

            var firstList = parts.GetRange(0, firstPartOfList);
            var lastList = parts.GetRange(firstPartOfList, lastPartOfList);

            var firstItem = new ChunkInfoItem
            {
                FirstPart = firstList.FirstOrDefault(),
                LastPart = firstList.LastOrDefault(),
                CountOfLinesInFile = firstList.Count,
                Buffer = firstList,
            };

            WriteBufferToFile(firstItem);

            var lastItem = new ChunkInfoItem
            {
                FirstPart = lastList.FirstOrDefault(),
                LastPart = lastList.LastOrDefault(),
                CountOfLinesInFile = lastList.Count,
                Buffer = lastList,
            };

            WriteBufferToFile(lastItem);

            _linkedlist.AddBefore(node, firstItem);
            _linkedlist.AddAfter(node, lastItem);

            _linkedlist.Remove(node);

            File.Delete(node.Value.StringFilePath);
            File.Delete(node.Value.NumberFilePath);

            Console.WriteLine($"{DateTime.Now}. Processed buffer and split chunk to two new. Chunks count: {_linkedlist.Count}");
        }

        private LinkedListNode<ChunkInfoItem> SortChunks(LinkedListNode<ChunkInfoItem> first, LinkedListNode<ChunkInfoItem> second)
        {
            var parts = new List<StringNumberPart>();

            // read first chunk 
            using (var stringReader = new StreamReader(first.Value.StringFilePath))
            {
                using (var numberReader = new StreamReader(first.Value.NumberFilePath))
                {
                    string stringLine;
                    while ((stringLine = stringReader.ReadLine()) != null)
                    {
                        var numberLine = numberReader.ReadLine();
                        parts.Add(new StringNumberPart(stringLine, numberLine));
                    }
                }
            }

            // read second chunk 
            using (var stringReader = new StreamReader(second.Value.StringFilePath))
            {
                using (var numberReader = new StreamReader(second.Value.NumberFilePath))
                {
                    string stringLine;
                    while ((stringLine = stringReader.ReadLine()) != null)
                    {
                        var numberLine = numberReader.ReadLine();
                        parts.Add(new StringNumberPart(stringLine, numberLine));
                    }
                }
            }

            parts.Sort(_comparer);

            var firstPartOfList = parts.Count / 2;
            var lastPartOfList = parts.Count - firstPartOfList;

            var firstList = parts.GetRange(0, firstPartOfList);
            var lastList = parts.GetRange(firstPartOfList, lastPartOfList);

            var newFirstItem = new ChunkInfoItem
            {
                FirstPart = firstList.FirstOrDefault(),
                LastPart = firstList.LastOrDefault(),
                CountOfLinesInFile = firstList.Count,
                Buffer = firstList,
            };

            WriteBufferToFile(newFirstItem);

            var newLastItem = new ChunkInfoItem
            {
                FirstPart = lastList.FirstOrDefault(),
                LastPart = lastList.LastOrDefault(),
                CountOfLinesInFile = lastList.Count,
                Buffer = lastList,
            };

            WriteBufferToFile(newLastItem);

            var result = _linkedlist.AddBefore(first, newFirstItem);

            _linkedlist.AddAfter(second, newLastItem);

            _linkedlist.Remove(first);
            _linkedlist.Remove(second);

            File.Delete(first.Value.StringFilePath);
            File.Delete(second.Value.NumberFilePath);

            Console.WriteLine($"{DateTime.Now}. Sorting existing chunks to ensure correct order.");
            
            return result;
        }

        private static IEnumerable<LinkedListNode<T>> EnumerateNodes<T>(LinkedList<T> list)
        {
            var node = list.First;
            while (node != null)
            {
                yield return node;
                node = node.Next;
            }
        }

        private void JoinChunksToOutputFile()
        {
            const int linesCountProgressToInform = 1000 * 1000 * 5;
            
            using (var writer = new StreamWriter(_outputFilePath))
            {
                // there possible preloading of buffers
                foreach (var treeItem in _linkedlist)
                {
                    var stringItems = File.ReadAllLines(treeItem.StringFilePath);
                    var numberItems = File.ReadAllLines(treeItem.NumberFilePath);

                    for (int i = 0; i < stringItems.Length; i++)
                    {
                        writer.WriteLine(numberItems[i] + ". " + stringItems[i]);
                        _outputCountOfLines++;

                        if (_outputCountOfLines % linesCountProgressToInform == 0)
                        {
                            Console.WriteLine($"{DateTime.Now}. Processed count of initial lines: {_outputCountOfLines} / {_initialCountOfLines}");
                        }
                    }
               }
            }
        }

        public static StringNumberPart LinePreProcessingFunc(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return new StringNumberPart(null, null);
            }

            var indexOfEndOfNumberPart = line.IndexOf(" ", StringComparison.OrdinalIgnoreCase);

            var stringPart = line.Substring(indexOfEndOfNumberPart + 1); // without space
            var numberPart = line.Substring(0, indexOfEndOfNumberPart - 1);

            return new StringNumberPart(stringPart, numberPart);
        }
    }
}
