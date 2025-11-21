using System.Collections.Concurrent;
using System.IO.Hashing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace UQApp.core
{
    public class UQService(
        ILogger<UQService> logger,
        IConfiguration configuration,
        IOptions<UQOptions> options,
        DirectoryScanner scanner,
        FileHasher hasher
    ) : BackgroundService
    {
        private readonly ILogger<UQService> _logger = logger;
        private readonly IConfiguration _configuration = configuration;
        private readonly FileHasher _hasher = hasher;
        private readonly DirectoryScanner _scanner = scanner;
        private readonly IOptions<UQOptions> _options = options;

        protected override async Task ExecuteAsync(CancellationToken ct)
        {

            Console.WriteLine(_configuration.GetValue<string>("SomeConfig"));
            var directoriesToScan = _options.Value.Roots;

            if (directoriesToScan == null || directoriesToScan.Count == 0)
            {
                _logger.LogWarning("No root directories specified for scanning. Please provide at least one directory.");
                return;
            }

            try
            {
                // TODO: Need to free resources once we're done with them
                // TODO: Add support for a new search after duplicates are found

                // SECTION STAGE 1: DIRECTORY SCANNING
                IEnumerable<List<FileEntry>>? potentialPartialDuplicates = _scanner.Scan(directoriesToScan, ct);
                if (ct.IsCancellationRequested) return;
                // If no potential duplicates found based on size, exit early
                if (potentialPartialDuplicates == null || !potentialPartialDuplicates.Any())
                {
                    _logger.LogInformation("No duplicate files found based on size.");
                    return;
                }
                // !SECTION

                // SECTION STAGE 2: PARTIAL HASHING
                var partialHashGroups = await NewMethod(potentialPartialDuplicates, ct);
                if (partialHashGroups == null || ct.IsCancellationRequested) return;
                // !SECTION 

                // SECTION STAGE 3: FULL HASHING
                var duplicateGroups = await NewMethod(partialHashGroups, ct);
                if (duplicateGroups == null || ct.IsCancellationRequested) return;
                // !SECTION END OF STAGE 3

                // SECTION Reporting
                ReportDuplicateFiles(duplicateGroups);
                // !SECTION 
            }
            catch (OperationCanceledException e)
            {
                _logger.LogInformation(e, "Duplicate finding operation was cancelled.");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred during duplicate finding.");
            }
        }

        private async Task<IEnumerable<List<FileEntry>>?> NewMethod(IEnumerable<List<FileEntry>> duplicates, CancellationToken ct)
        {
            var inputQueue = new BlockingCollection<FileEntry>(boundedCapacity: 1000);
            var outputQueue = new BlockingCollection<FileEntry>(boundedCapacity: 1000);
            // TODO: Make worker count dynamic or configurable by user
            // Starting with 4 workers, change as needed after performance testing
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            _hasher.Start(inputQueue, outputQueue, ComputePartialHash, ct);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            var fillInputQueueTask = Task.Run(() =>
            {
                foreach (var group in duplicates)
                {
                    if (ct.IsCancellationRequested) return;
                    foreach (var fileMeta in group)
                    {
                        if (ct.IsCancellationRequested) return;
                        inputQueue.TryAdd(fileMeta);
                    }
                }
                inputQueue.CompleteAdding();
            }, ct);

            // NOTE: change to ConcurrentDictionary if needed
            var hashGroups = new Dictionary<string, List<FileEntry>>();
            foreach (var entry in outputQueue.GetConsumingEnumerable(ct))
            {
                if (ct.IsCancellationRequested) return null;
#pragma warning disable CS8604 // Possible null reference argument.
                if (!hashGroups.TryGetValue(entry.Hash, out var group))
                {
                    group = new List<FileEntry>();
                    hashGroups[entry.Hash] = group;
                }
#pragma warning restore CS8604 // Possible null reference argument.
                group.Add(entry);
            }

            // thread pauses here until the above consuming enumerable is complete
            // This is to ensure the production of partial hashes is complete before proceeding to the next stage
            // Without await here, the RunAsync method could complete and return while background tasks are still running, leading to: 
            // resource leaks (queues not properly disposed), incomplete processing, and race conditions
            await fillInputQueueTask;
            return hashGroups.Values.Where(g => g.Count > 1);
        }

        private void ReportDuplicateFiles(IEnumerable<List<FileEntry>> duplicateFiles)
        {
            foreach (var group in duplicateFiles)
            {
                Console.WriteLine(new string('*', 10));
                foreach (var file in group)
                {
                    Console.WriteLine($" - {file.Path} (Size: {file.Size}, Hash: {file.Hash})");
                }
            }
        }

        public string ComputePartialHash(string filePath)
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            long bytesToRead = Math.Min(4096, fs.Length);
            byte[] buffer = new byte[bytesToRead];
            int bytesRead = fs.Read(buffer, 0, buffer.Length);
            if (bytesRead < bytesToRead)
            {
                Array.Resize(ref buffer, bytesRead);
            }

            // using var hasher = SHA256.Create();
            // var hashBytes = hasher.ComputeHash(buffer);
            var hashBytes = XxHash3.Hash(buffer);

            // XxHash3 is faster than SHA256, but less cryptographically secure. It's appropriate for dict key generation
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