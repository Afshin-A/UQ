using System.Collections.Concurrent;

namespace FileDedupeApp.core
{
    public class ScanManager
    {
        public void Run(IEnumerable<string> directories)
        {
            var fileQueue = new BlockingCollection<string>(boundedCapacity: 1000);
            var hashQueue = new BlockingCollection<FileEntry>(boundedCapacity: 1000);

            // place all files in the fileQueue
            var scanner = new DirectoryScanner(fileQueue);
            // place all hashed FileEntry objects in the hashQueue
            var hasher = new FileHasher(fileQueue, hashQueue);
            // find duplicates from the hashQueue
            var finder = new DuplicateFinder(hashQueue);

            var scanTask = Task.Run(() => scanner.Scan(directories));
            Console.WriteLine($"{Environment.ProcessorCount} logical processors detected.");
            hasher.Start(Environment.ProcessorCount);
            var findTask = Task.Run(() => finder.Process());

            Task.WaitAll(scanTask, findTask);

            var duplicates = finder.GetDuplicates();
            Console.WriteLine($"Found {duplicates.Count()} duplicate groups");


        }
    }
}