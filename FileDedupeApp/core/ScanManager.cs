using System.Collections.Concurrent;
using System.IO.Hashing;
namespace FileDedupeApp.core
{
    public class ScanManager
    {
        public static void Run(IEnumerable<string> directories)
        {
            // TODO: Need to free resources once we're done with them
            // TODO: Add support for a new search after duplicates are found
            var sizeGroups = new Dictionary<long, List<FileEntry>>();
            var scanner = new DirectoryScanner(sizeGroups);
            scanner.Scan(directories);

            var potentialPartialDuplicates = sizeGroups
                .Where(kv => kv.Value.Count > 1)
                .Select(kv => kv.Value);
            var partialHashInputQueue = new BlockingCollection<FileEntry>(boundedCapacity: 1000);
            var partialHashOutputQueue = new BlockingCollection<FileEntry>(boundedCapacity: 1000);
            var partialHasher = new FileHasher(partialHashInputQueue, partialHashOutputQueue, ComputePartialHash);
            // TODO: Make worker count dynamic
            // starting with 4 workers, change as needed after performance testing
            partialHasher.Start(workerCount: 4);

            Task.Run(() =>
            {
                foreach (var group in potentialPartialDuplicates)
                {
                    foreach (var fileMeta in group)
                    {
                        partialHashInputQueue.Add(fileMeta);
                    }
                }
                partialHashInputQueue.CompleteAdding();
            });

            var partialHashGroups = new Dictionary<string, List<FileEntry>>();
            foreach (var entry in partialHashOutputQueue.GetConsumingEnumerable())
            {
#pragma warning disable CS8604 // Possible null reference argument.
                if (!partialHashGroups.TryGetValue(entry.Hash, out var group))
                {
                    group = new List<FileEntry>();
                    partialHashGroups[entry.Hash] = group;
                }
#pragma warning restore CS8604 // Possible null reference argument.
                group.Add(entry);
            }

            var fullHashInputQueue = new BlockingCollection<FileEntry>(boundedCapacity: 1000);
            var fullHashOutputQueue = new BlockingCollection<FileEntry>(boundedCapacity: 1000);
            var fullHasher = new FileHasher(fullHashInputQueue, fullHashOutputQueue, ComputeFullHash);
            fullHasher.Start(workerCount: 4);

            Task.Run(() =>
            {
                foreach (var group in partialHashGroups.Values.Where(g => g.Count > 1))
                {
                    foreach (var file in group)
                    {
                        fullHashInputQueue.Add(file);
                    }
                }
                fullHashInputQueue.CompleteAdding();
            });

            var fullHashGroups = new Dictionary<string, List<FileEntry>>();
            foreach (var entry in fullHashOutputQueue.GetConsumingEnumerable())
            {
#pragma warning disable CS8604 // Possible null reference argument.
                if (!fullHashGroups.TryGetValue(entry.Hash, out var group))
                {
                    group = new List<FileEntry>();
                    fullHashGroups[entry.Hash] = group;
                }
#pragma warning restore CS8604 // Possible null reference argument.
                group.Add(entry);
            }


            var duplicateFiles = fullHashGroups.Values.Where(g => g.Count > 1);
            foreach (var group in duplicateFiles)
            {
                Console.WriteLine(new string('*', 10));
                foreach (var file in group)
                {
                    Console.WriteLine($" - {file.Path} (Size: {file.Size}, Hash: {file.Hash})");
                }
            }
        }


        public static string ComputePartialHash(string filePath)
        {
            // using is syntactic sugar for try/finally to ensure disposal/closing of the stream and hasher streams
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            long bytesToRead = Math.Min(4096, fs.Length);
            byte[] buffer = new byte[bytesToRead];
            int bytesRead = fs.Read(buffer, 0, buffer.Length);
            if (bytesRead < bytesToRead)
            {
                // ref means we are passing the reference of the buffer variable, allowing the method to modify the original variable
                Array.Resize(ref buffer, bytesRead);
            }

            // using var hasher = SHA256.Create();
            // var hashBytes = hasher.ComputeHash(buffer);
            var hashBytes = XxHash3.Hash(buffer);

            // XxHash3 is faster than SHA256, but less cryptographically secure. It's appropriate for key generation
            return Convert.ToHexString(hashBytes);
        }

        public static string ComputeFullHash(string filePath)
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            int bytesToRead = (int)fs.Length;
            byte[] buffer = new byte[bytesToRead];
            fs.ReadExactly(buffer, 0, bytesToRead);
            var hashBytes = XxHash3.Hash(buffer);
            return Convert.ToHexString(hashBytes);
        }
    }
}